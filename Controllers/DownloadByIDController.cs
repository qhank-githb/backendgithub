using Microsoft.AspNetCore.Mvc;
using MinioWebBackend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.Annotations;

namespace MinioWebBackend.Controllers
{
    [ApiController]
    [Route("api/file")] // ✅ 只以固定路径访问，无需 bucket 参数
    [Authorize] 
    public class DownloadByIDController : ControllerBase
    {
        private readonly IDownloadByIDService _downloadByIdService;

        public DownloadByIDController(IDownloadByIDService downloadByIdService)
        {
            _downloadByIdService = downloadByIdService;
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
            var (stream, error, fileInfo) = await _downloadByIdService. DownloadFileByIdAsync(id);
            if (stream == null) return NotFound(new { Message = error });

            var mime = fileInfo!.MimeType ?? "application/octet-stream";

            // 直接返回文件流，浏览器会根据 mimeType 决定是否 inline 预览
            return File(stream, mime);
        }






    }
}
