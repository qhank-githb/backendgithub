using Microsoft.AspNetCore.Mvc;
using MinioWebBackend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.Annotations;
using MinioWebBackend.Models;
using Amazon.S3;
using Amazon.S3.Model;

namespace MinioWebBackend.Controllers
{
    [ApiController]
    [Route("api/file")] // ✅ 只以固定路径访问，无需 bucket 参数
    [Authorize]
    public class DownloadByIDController : ControllerBase
    {
        private readonly IDownloadByIDService _downloadByIdService;
        private readonly IAmazonS3 _s3Client;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IQueryService _iQueryService;

        public DownloadByIDController(IDownloadByIDService downloadByIdService,
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
        /// 根据文件 ID 下载文件
        /// </summary>
        /// <param name="id">文件的唯一 ID</param>
        /// <returns>返回文件流，下载成功；找不到文件返回 404</returns>
        [HttpGet("download-by-id")]
        public async Task<IActionResult> DownloadById([FromQuery] int id)
        {
            var (stream, error, fileInfo) = await _downloadByIdService.DownloadFileByIdAsync(id);
            if (stream == null) return NotFound(new { Message = error });

            var fileName = fileInfo!.OriginalFileName ?? "file.dat";
            var mime = fileInfo.MimeType ?? "application/octet-stream";

            // 让框架处理 Content-Disposition（会正确处理非 ASCII 文件名）
            return File(stream, mime, fileName);
        }


        /// <summary>
        /// 根据文件 ID 预览文件
        /// </summary>
        /// <param name="id">文件的唯一 ID</param>
        /// <returns>返回文件流，Content-Disposition 为 inline，可直接在浏览器预览；找不到文件返回 404</returns>
        [HttpGet("preview-by-id")]
        public async Task<IActionResult> PreviewById([FromQuery] int id)
        {
            var (response, error, fileInfo) = await PreviewFileByIdAsync(id);
            if (response == null) return NotFound(new { Message = error });

            var mime = fileInfo?.MimeType ?? "application/octet-stream";
            var fileName = fileInfo?.OriginalFileName ?? "file";

            if (response.ContentLength > 0)
            {
                Response.ContentLength = response.ContentLength;
            }

            Response.Headers["Content-Disposition"] = $"inline; filename*=UTF-8''{Uri.EscapeDataString(fileName)}";

            // 直接返回底层流（不要 Dispose response 直到框架写完）
            return File(response.ResponseStream, mime);
        }

        
        // Controller 中（替换原来的 PreviewFileByIdAsync）
        private async Task<(GetObjectResponse? Response, string? Error, FileInfoModel? FileInfo)> PreviewFileByIdAsync(int id)
        {
            try
            {
                var fileInfo = await _iQueryService.GetFileByIdAsync(id);
                if (fileInfo == null)
                    return (null, $"未找到 ID={id} 的文件", null);

                var request = new GetObjectRequest
                {
                    BucketName = fileInfo.Bucketname,
                    Key = fileInfo.StoredFileName
                };

                var response = await _s3Client.GetObjectAsync(request);

                // 注意：不要在这里 Dispose response
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
}
