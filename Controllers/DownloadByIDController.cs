using Microsoft.AspNetCore.Mvc;
using MinioWebBackend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.Annotations;
using MinioWebBackend.Models;
using Amazon.S3;
using Amazon.S3.Model;

namespace MinioWebBackend.Controllers
{
    /// <summary>
    /// 文件下载与预览接口控制器（需登录后使用）
    /// </summary>
    /// <remarks>
    /// 提供以下功能：
    /// - 根据文件 ID 下载文件（Content-Disposition = attachment）
    /// - 根据文件 ID 在线预览文件（Content-Disposition = inline）
    /// </remarks>
    [ApiController]
    [Route("api/file")] //  固定访问路径，无需手动传入 bucket
    [Authorize] //  所有接口需要身份验证（JWT）
    public class DownloadByIDController : ControllerBase
    {
        private readonly IDownloadByIDService _downloadByIdService;
        private readonly IAmazonS3 _s3Client;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IQueryService _iQueryService;

        /// <summary>
        /// 构造函数，注入依赖服务
        /// </summary>
        /// <param name="downloadByIdService">文件下载服务</param>
        /// <param name="s3Client">S3/MinIO 客户端</param>
        /// <param name="httpContextAccessor">HttpContext 访问器</param>
        /// <param name="iQueryService">文件信息查询服务</param>
        public DownloadByIDController(
            IDownloadByIDService downloadByIdService,
            IAmazonS3 s3Client,
            IHttpContextAccessor httpContextAccessor,
            IQueryService iQueryService
        )
        {
            _downloadByIdService = downloadByIdService;
            _s3Client = s3Client;
            _httpContextAccessor = httpContextAccessor;
            _iQueryService = iQueryService;
        }

/// <summary>
/// 根据文件 ID 下载文件（作为附件）
/// </summary>
/// <param name="id">文件的唯一 ID（数据库主键）</param>
/// <remarks>
/// **接口说明：**
/// - 根据文件 ID 从 S3/MinIO 下载文件  
/// - 文件以附件形式返回，浏览器会触发下载
///
/// **请求示例：**
/// GET /api/files/download-by-id?id=123
///
/// **成功响应（200 OK）：**
/// - 返回类型：<see cref="FileStreamResult"/>
/// - 响应体：文件二进制流，来源于 S3/MinIO 的对象流复制到 <see cref="MemoryStream"/>
/// - 响应头：
///Content-Type: 文件 MIME 类型（如 application/pdf、image/png 等）  
///Content-Disposition: attachment; filename="原始文件名"  
///
/// **失败响应：**
/// - 404 Not Found：未找到对应文件，返回 <see cref="NotFoundObjectResult"/>，响应体包含错误信息  
/// - 500 Internal Server Error：下载失败或服务异常
///
/// **返回示例（404）：**
/// ```json
/// {
///     "message": "文件不存在或下载失败"
/// }
/// ```
/// </remarks>
[HttpGet("download-by-id")]
[SwaggerResponse(StatusCodes.Status200OK, "下载成功，返回文件流", typeof(FileStreamResult))]
[SwaggerResponse(StatusCodes.Status404NotFound, "未找到对应文件", typeof(object))]
[SwaggerResponse(StatusCodes.Status500InternalServerError, "下载失败或服务异常", typeof(object))]
public async Task<IActionResult> DownloadById([FromQuery] int id)
{
    // 调用业务服务，根据 ID 获取文件流、错误信息、文件元数据
    var (stream, error, fileInfo) = await _downloadByIdService.DownloadFileByIdAsync(id);
    if (stream == null) return NotFound(new { Message = error });

    // 如果数据库没保存文件名或 MIME，提供默认值
    var fileName = fileInfo!.OriginalFileName ?? "file.dat";
    var mime = fileInfo.MimeType ?? "application/octet-stream";

    // 返回文件流，浏览器会自动下载
    return File(stream, mime, fileName);
}


/// <summary>
/// 根据文件 ID 预览文件（内联显示）
/// </summary>
/// <param name="id">文件的唯一 ID（数据库主键）</param>
/// <returns>
/// 成功返回文件流 (<see cref="Stream"/>)，前端可直接在浏览器中内联显示文件（如 PDF、图片、TXT）。
/// 失败返回 404，响应体包含错误信息，例如 `{ "message": "文件不存在" }`。
/// </returns>
/// <remarks>
/// **接口行为：**
/// - 设置 `Content-Disposition: inline`，浏览器直接显示文件内容而非下载
/// - 设置文件 MIME 类型 (`Content-Type`)，浏览器根据类型渲染
/// - 支持 PDF、图片、TXT 等常见文件类型
///
/// **前端调用示例（Axios）：**
/// ```js
/// const res = await axios.get('/api/file/preview-by-id?id=123', { responseType: 'blob' });
/// const blob = new Blob([res.data], { type: res.data.type || 'application/octet-stream' });
/// const url = URL.createObjectURL(blob);
/// window.open(url); // 直接在浏览器打开预览
/// ```
///
/// **注意事项：**
/// - 与 DownloadById 接口不同，DownloadById 会强制下载文件（`Content-Disposition: attachment`）
/// - PreviewById 仅用于内联预览
/// </remarks>
[HttpGet("preview-by-id")]
[SwaggerResponse(StatusCodes.Status200OK, "预览成功，返回文件流")]
[SwaggerResponse(StatusCodes.Status404NotFound, "未找到对应文件")]
public async Task<IActionResult> PreviewById([FromQuery] int id)
{
    // 获取 S3 响应流 + 文件信息
    var (response, error, fileInfo) = await PreviewFileByIdAsync(id);
    if (response == null) return NotFound(new { Message = error });

    var mime = fileInfo?.MimeType ?? "application/octet-stream";
    var fileName = fileInfo?.OriginalFileName ?? "file";

    // 设置 Content-Length，提升浏览器兼容性
    if (response.ContentLength > 0)
    {
        Response.ContentLength = response.ContentLength;
    }

    // 设置为 inline，浏览器支持直接预览（如 PDF、图片、TXT 等）
    Response.Headers["Content-Disposition"] =
        $"inline; filename*=UTF-8''{Uri.EscapeDataString(fileName)}";

    // 直接返回流，不要在这里 Dispose response
    return File(response.ResponseStream, mime);
}


        /// <summary>
        /// 根据文件 ID 从 S3 获取文件流（预览用）
        /// </summary>
        /// <param name="id">文件 ID</param>
        /// <returns>
        /// 返回一个元组：
        /// - Response：S3 原始响应（包含文件流）  
        /// - Error：错误信息（如果失败）  
        /// - FileInfo：数据库中的文件元数据
        /// </returns>
        private async Task<(GetObjectResponse? Response, string? Error, FileInfoModel? FileInfo)> PreviewFileByIdAsync(int id)
        {
            try
            {
                // 1. 查询数据库中的文件信息
                var fileInfo = await _iQueryService.GetFileByIdAsync(id);
                if (fileInfo == null)
                    return (null, $"未找到 ID={id} 的文件", null);

                // 2. 构造 S3 请求
                var request = new GetObjectRequest
                {
                    BucketName = fileInfo.Bucketname,
                    Key = fileInfo.StoredFileName
                };

                // 3. 向 MinIO/S3 请求文件对象
                var response = await _s3Client.GetObjectAsync(request);

                // ✅ 注意：这里不要 Dispose response，交给框架处理
                return (response, null, fileInfo);
            }
            catch (AmazonS3Exception ex)
            {
                return (null, $"MinIO 访问异常: {ex.Message}", null);
            }
            catch (Exception ex)
            {
                return (null, $"未知错误: {ex.Message}", null);
            }
        }
    }
    
    public class DownloadFileResponse
{
    /// <summary>文件名称</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>MIME 类型</summary>
    public string MimeType { get; set; } = "application/octet-stream";

    /// <summary>文件大小（字节）</summary>
    public long Size { get; set; }

    /// <summary>文件内容 Base64 编码字符串</summary>
    public string Base64Content { get; set; } = string.Empty;
}
}

