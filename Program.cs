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





var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:5000");


var dbConnectionString = builder.Configuration.GetSection("Minio:DbConnectionString").Value;

// 注入 DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        dbConnectionString,
        new MySqlServerVersion(new Version(8, 0, 33))
    )
);

// 添加 CORS 服务
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


// 在 Program.cs 中注册
builder.Services.AddSingleton<IAmazonS3>(sp => {
    var minioOptions = sp.GetRequiredService<IOptions<MinioOptions>>().Value;
    var creds = new BasicAWSCredentials(minioOptions.AccessKey, minioOptions.SecretKey);
    var config = new AmazonS3Config {
        ServiceURL = $"http://{minioOptions.Endpoint}",
        ForcePathStyle = true
    };
    return new AmazonS3Client(creds, config);
});
builder.Services.AddSingleton<TransferUtility>(sp => 
    new TransferUtility(sp.GetRequiredService<IAmazonS3>())
);


// 1️⃣ 允许上传大文件（500MB）
builder.WebHost.ConfigureKestrel(opts =>
{
    opts.Limits.MaxRequestBodySize = 524_288_000; // 500MB
});

builder.Services.Configure<FormOptions>(opts =>
{
    opts.MultipartBodyLengthLimit = 524_288_000;
});

builder.Services.AddScoped<IBucketService, BucketService>();


// 2️⃣ 绑定 appsettings.json 中的 Minio 节配置
builder.Services.Configure<MinioOptions>(builder.Configuration.GetSection("Minio"));

// 3️⃣ 注册你自己的服务
builder.Services.AddScoped<IQueryService, QueryService>(); 
builder.Services.AddScoped<IUploadService, UploadService>();
builder.Services.AddScoped<IDownloadService, DownloadService>();
builder.Services.AddScoped<IDownloadByIDService, DownloadByIDService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IFileTagService, FileTagService>();


builder.Services.AddAuthorization();
builder.Services.AddControllers();


//JWT
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
        ValidIssuer = "your_issuer",
        ValidAudience = "your_audience",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your_secret_key_here"))
    };
});

var app = builder.Build();

///////
// 测试数据库
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    Console.WriteLine("数据库连接成功并确保表存在");
}


//////

app.UseCors("AllowAll");
app.UseRouting();
app.UseAuthorization();
app.MapControllers();
app.Run();

