using Microsoft.AspNetCore.Http.Features;
using MinioWebBackend.Interfaces;
using MinioWebBackend.Options;
using MinioWebBackend.Service;
using Microsoft.Extensions.Options;
using Amazon.S3;
using Amazon.Runtime;
using Amazon.S3.Transfer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using MinioWebBackend.Filters;
using MinioWebBackend.Serilog;
using Microsoft.OpenApi.Models;
using Nest;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5000");

// ==================== 数据库配置 ====================
var activeConfig = builder.Configuration["ActiveConfig"];
var provider = builder.Configuration[$"Configs:{activeConfig}:DatabaseProvider"];
var connStr = builder.Configuration[$"Configs:{activeConfig}:ConnectionStrings:DefaultConnection"];

builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    if (string.Equals(provider, "MySQL", StringComparison.OrdinalIgnoreCase))
        options.UseMySql(connStr, ServerVersion.AutoDetect(connStr));
    else if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
        options.UseSqlServer(connStr);
    else
        throw new Exception($"不支持的数据库类型: {provider}");

    // 注册拦截器
    var interceptor = serviceProvider.GetRequiredService<ElasticSyncInterceptor>();
    options.AddInterceptors(interceptor);
});


// ==================== Swagger ====================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MinIO Web Backend API",
        Version = "v1",
        Description = "MinIO 文件管理 API（含认证、上传、标签等）"
    });

    var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlFilePath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlFilePath))
        c.IncludeXmlComments(xmlFilePath, includeControllerXmlComments: true);
    else
        Console.WriteLine($"警告：未找到 XML 注释文件，路径：{xmlFilePath}");

    c.OperationFilter<FileUploadOperationFilter>();
    c.EnableAnnotations();
});

// ==================== CORS ====================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .WithExposedHeaders("Content-Disposition"));
});

// ==================== MinIO ====================
builder.Services.Configure<MinioOptions>(builder.Configuration.GetSection("Minio"));
builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var minioOptions = sp.GetRequiredService<IOptions<MinioOptions>>().Value;
    var creds = new BasicAWSCredentials(minioOptions.AccessKey, minioOptions.SecretKey);
    var config = new AmazonS3Config
    {
        ServiceURL = $"http://{minioOptions.Endpoint}",
        ForcePathStyle = true
    };
    return new AmazonS3Client(creds, config);
});
builder.Services.AddSingleton<TransferUtility>(sp =>
    new TransferUtility(sp.GetRequiredService<IAmazonS3>()));

// ==================== Elasticsearch ====================
// ==================== Elasticsearch ====================
builder.Services.AddSingleton<IElasticClient>(sp =>
{
    var uri = new Uri("http://192.168.150.93:9200"); // Elasticsearch IP

    var settings = new ConnectionSettings(uri)
                   .DefaultIndex("files")
                   .EnableDebugMode(); // 打开调试，方便排查错误

    // 如果 Elasticsearch 需要用户名密码，请取消注释：
    // settings = settings.BasicAuthentication("elastic", "你的密码");

    return new ElasticClient(settings);
});

builder.Services.AddScoped<ElasticSyncService>();



// ==================== Serilog ====================
builder.Host.UseSerilog((context, services, loggerConfig) =>
{
    var scopeFactory = services.GetRequiredService<IServiceScopeFactory>();
    loggerConfig
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .Enrich.WithThreadId()
        .WriteTo.Console()
        .WriteTo.File(
            path: "logs/app.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14
        )
        .WriteTo.EFCore(scopeFactory)
        .WriteTo.Async(a => a.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://192.168.150.93:9200"))
        {
            AutoRegisterTemplate = true,
            IndexFormat = "myapp-logs-{0:yyyy.MM.dd}",
            NumberOfReplicas = 1,
            NumberOfShards = 2
        }));
});

// ==================== 上传限制 ====================
builder.WebHost.ConfigureKestrel(opts => opts.Limits.MaxRequestBodySize = 524_288_000);
builder.Services.Configure<FormOptions>(opts => opts.MultipartBodyLengthLimit = 524_288_000);

// ==================== 业务服务 ====================
builder.Services.AddScoped<IBucketService, BucketService>();
builder.Services.AddScoped<IQueryService, QueryService>();
builder.Services.AddScoped<IUploadService, UploadService>();
builder.Services.AddScoped<IDownloadService, DownloadService>();
builder.Services.AddScoped<IDownloadByIDService, DownloadByIDService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IFileTagService, FileTagService>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ILogQueryService, LogQueryService>();
builder.Services.AddScoped<ElasticSearchService>();
builder.Services.AddScoped<ElasticSyncInterceptor>();

// ==================== 控制器 ====================
builder.Services.AddControllers();

// ==================== JWT ====================
var jwtSecretKey = "MySuperSecretKeyForJWTToken_32BytesOrMore!";
var issuer = "my_app_issuer";
var audience = "my_app_audience";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = ctx =>
        {
            if (ctx.Exception is SecurityTokenExpiredException)
            {
                var token = ctx.Request.Headers["Authorization"].ToString();
                Serilog.Log.Warning("JWT Token 已过期: {Token}", token);
            }
            return Task.CompletedTask;
        }
    };
});

var app = builder.Build();

// ==================== 数据库初始化 ====================
using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;

    // 数据库初始化
    try
    {
        var dbContext = serviceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();

        var authService = serviceProvider.GetRequiredService<IAuthService>();
        await authService.InitializeAdminAccountAsync();
    }
    catch (Exception ex)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "应用启动时初始化管理员账号失败");
    }

    // Elasticsearch 同步
    var elasticService = scope.ServiceProvider.GetRequiredService<ElasticSyncService>();
    try
    {
        await elasticService.SyncAllFilesAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"同步失败: {ex.Message}");
    }
}


// ==================== 中间件 ====================
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    c.RoutePrefix = "swagger";
});

app.UseCors("AllowAll");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
