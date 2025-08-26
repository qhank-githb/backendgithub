using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using Amazon.S3;
using Amazon.Runtime;
using Amazon.S3.Transfer;
using ConsoleApp1.Interfaces;
using ConsoleApp1.Options;
using ConsoleApp1.Service;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5000");


// ---------------------- Serilog ----------------------
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File("logs/app.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// ---------------------- 数据库 ----------------------
var dbConnectionString = builder.Configuration.GetSection("Minio:DbConnectionString").Value;
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(dbConnectionString, new MySqlServerVersion(new Version(8, 0, 33)))
);

// ---------------------- CORS ----------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader().WithExposedHeaders("Content-Disposition");
    });
});

// ---------------------- MinIO ----------------------
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
    new TransferUtility(sp.GetRequiredService<IAmazonS3>())
);

// ---------------------- 文件上传限制 ----------------------
builder.WebHost.ConfigureKestrel(opts =>
{
    opts.Limits.MaxRequestBodySize = 524_288_000; // 500MB
});
builder.Services.Configure<FormOptions>(opts =>
{
    opts.MultipartBodyLengthLimit = 524_288_000; // 500MB
});

// ---------------------- 业务服务 ----------------------
builder.Services.AddScoped<IBucketService, BucketService>();
builder.Services.AddScoped<IQueryService, QueryService>();
builder.Services.AddScoped<IUploadService, UploadService>();
builder.Services.AddScoped<IDownloadService, DownloadService>();
builder.Services.AddScoped<IDownloadByIDService, DownloadByIDService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IFileTagService, FileTagService>();
builder.Services.AddHttpContextAccessor();


// ---------------------- 心跳服务 ----------------------
builder.Services.AddSingleton<OnlineUserService>();
builder.Services.AddHostedService<OfflineChecker>();

// ---------------------- JWT 认证 ----------------------
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
    };
});

builder.Services.AddAuthorization();

// ---------------------- 控制器 ----------------------
builder.Services.AddControllers();


var app = builder.Build();

// ---------------------- 确保数据库 ----------------------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    Console.WriteLine("数据库连接成功并确保表存在");
}

// ---------------------- 中间件 ----------------------
app.UseCors("AllowAll");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
