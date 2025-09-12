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
using MinioWebBackend.Serilog; // 引入包含 EFCoreSinkExtensions 的命名空间



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
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ILogQueryService, LogQueryService>(); // 新增：日志查询服务


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


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    
    try
    {
        // 确保数据库已创建（如果使用Code First迁移，可替换为数据库迁移逻辑）
        var dbContext = serviceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureCreatedAsync(); // 创建数据库和表（首次运行时）
        
        // 触发管理员账号初始化
        var authService = serviceProvider.GetRequiredService<IAuthService>();
        await authService.InitializeAdminAccountAsync(); // 调用初始化方法
    }
    catch (Exception ex)
    {
        // 记录初始化失败的日志
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "应用启动时初始化管理员账号失败");
    }
}



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
    c.RoutePrefix = "swagger"; 
});

// ==================== 中间件顺序 ====================
// 1. Swagger 中间件（仅开发环境建议启用，生产环境可注释）
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
    c.RoutePrefix = "swagger"; 
});

// 2. CORS 中间件（必须在 UseRouting 之前）
app.UseCors("AllowAll");

// 3. 路由中间件（启用路由匹配）
app.UseRouting();

// 4. 认证中间件（验证用户身份，必须在 UseAuthorization 之前）
app.UseAuthentication();

// 5. 授权中间件（处理 [Authorize] 特性，必须在 UseRouting 之后、MapControllers 之前）
app.UseAuthorization(); // ⚠️ 之前缺失的关键中间件

// 6. 控制器映射（必须在 UseAuthorization 之后）
app.MapControllers();

// 启动应用
app.Run();
