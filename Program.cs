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
using Microsoft.OpenApi.Models;
using MinioWebBackend.Filters;
using Swashbuckle.AspNetCore.Annotations;


var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5000");

// ==================== 数据库配置 ====================
var activeConfig = builder.Configuration["ActiveConfig"];

// 从对应配置块里拿 provider 和 connStr
var provider = builder.Configuration[$"Configs:{activeConfig}:DatabaseProvider"];
var connStr = builder.Configuration[$"Configs:{activeConfig}:ConnectionStrings:DefaultConnection"];

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (string.Equals(provider, "MySQL", StringComparison.OrdinalIgnoreCase))
    {
        options.UseMySql(connStr, ServerVersion.AutoDetect(connStr));
    }
    else if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(connStr);
    }
    else
    {
        throw new Exception($"不支持的数据库类型: {provider}");
    }
});
// ==================== Swagger ====================
// ==================== Swagger ====================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "My API",
        Version = "v1"
    });
    c.OperationFilter<FileUploadOperationFilter>();
    // 启用注释显示
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);

    // 启用 [SwaggerOperation] 注解
    c.EnableAnnotations(); // ⚠️ 需要 using Swashbuckle.AspNetCore.Annotations
});


// ==================== CORS ====================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("Content-Disposition");
    });
});

// ==================== MinIO ====================
builder.Services.Configure<MinioOptions>(builder.Configuration.GetSection("Minio"));

builder.Services.AddSingleton<IAmazonS3>(sp => {
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
    new TransferUtility(sp.GetRequiredService<IAmazonS3>())
);

// ==================== Serilog ====================
builder.Host.UseSerilog((context, services, loggerConfig) =>
{
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
        // 异步写 Elasticsearch
        .WriteTo.Async(a => a.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://localhost:9200"))
        {
            AutoRegisterTemplate = true,
            IndexFormat = "myapp-logs-{0:yyyy.MM.dd}", // 每天一个索引
            NumberOfReplicas = 1,
            NumberOfShards = 2
        }));
});



// ==================== 上传限制 ====================
builder.WebHost.ConfigureKestrel(opts =>
{
    opts.Limits.MaxRequestBodySize = 524_288_000; // 500MB
});

builder.Services.Configure<FormOptions>(opts =>
{
    opts.MultipartBodyLengthLimit = 524_288_000; // 500MB
});

// ==================== 注册业务服务 ====================
builder.Services.AddScoped<IBucketService, BucketService>();
builder.Services.AddScoped<IQueryService, QueryService>();
builder.Services.AddScoped<IUploadService, UploadService>();
builder.Services.AddScoped<IDownloadService, DownloadService>();
builder.Services.AddScoped<IDownloadByIDService, DownloadByIDService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IFileTagService, FileTagService>();
builder.Services.AddHttpContextAccessor();

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
                Log.Warning("JWT Token 已过期: {Token}", token);
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();



// ==================== 确保数据库存在 ====================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();  // 没有表就建表
    Console.WriteLine("数据库已迁移到最新版本");
}

// ==================== Swagger 中间件 ====================

    app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    c.RoutePrefix = "swagger"; // 访问 http://localhost:5000/swagger
});





// ==================== 中间件顺序 ====================
app.UseCors("AllowAll");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
