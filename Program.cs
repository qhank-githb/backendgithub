using Microsoft.AspNetCore.Http.Features;
using ConsoleApp1.Interfaces;
using ConsoleApp1.Options;
using ConsoleApp1.Service;
using Microsoft.Extensions.Options;
using Amazon.S3;
using Amazon.Runtime;
using Amazon.S3.Transfer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 允许外部访问
builder.WebHost.UseUrls("http://0.0.0.0:5000");

// 读取数据库连接字符串
var dbConnectionString = builder.Configuration.GetSection("Minio:DbConnectionString").Value;

// 注入 DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        dbConnectionString,
        new MySqlServerVersion(new Version(8, 0, 33))
    )
);

// 添加 CORS
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

// 注册 MinIO 服务
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



// 配置 Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File("logs/app.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();


builder.Host.UseSerilog(); // 将 Serilog 集成到 ASP.NET Core

// 上传大文件限制 500MB
builder.WebHost.ConfigureKestrel(opts =>
{
    opts.Limits.MaxRequestBodySize = 524_288_000; // 500MB
});

builder.Services.Configure<FormOptions>(opts =>
{
    opts.MultipartBodyLengthLimit = 524_288_000; // 500MB
});

// 注册业务服务
builder.Services.AddScoped<IBucketService, BucketService>();
builder.Services.AddScoped<IQueryService, QueryService>();
builder.Services.AddScoped<IUploadService, UploadService>();
builder.Services.AddScoped<IDownloadService, DownloadService>();
builder.Services.AddScoped<IDownloadByIDService, DownloadByIDService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IFileTagService, FileTagService>();
builder.Services.AddHttpContextAccessor();



// 添加控制器
builder.Services.AddControllers();

// ✅ 设置 JWT 认证
var jwtSecretKey = "MySuperSecretKeyForJWTToken_32BytesOrMore!"; // ⚠️ 必须 32+ 字节
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
    };
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = ctx =>
        {
            Console.WriteLine("JWT Authentication Failed: " + ctx.Exception.Message);
            return Task.CompletedTask;
        },
        OnMessageReceived = ctx =>
        {
            Console.WriteLine("JWT Received: " + ctx.Request.Headers["Authorization"]);
            return Task.CompletedTask;
        },
        OnTokenValidated = ctx =>
        {
            Console.WriteLine("JWT Token Validated for: " + ctx.Principal?.Identity?.Name);
            return Task.CompletedTask;
        }
    };
});


// 添加授权
builder.Services.AddAuthorization();

var app = builder.Build();

// 确保数据库表存在
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    Console.WriteLine("数据库连接成功并确保表存在");
}

// 中间件顺序：Cors → Authentication → Authorization → Routing → Endpoints
app.UseCors("AllowAll");

app.UseRouting();

app.UseAuthentication(); // ✅ 必须在 UseAuthorization 之前
app.UseAuthorization();

app.MapControllers();

app.Run();
