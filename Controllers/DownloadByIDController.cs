using Microsoft.AspNetCore.Mvc;
using MinioWebBackend.Interfaces;
using Microsoft.AspNetCore.Authorization;

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

        // ✅ GET: /api/file/download-by-id?id=123
        [HttpGet("download-by-id")]
        public async Task<IActionResult> DownloadById([FromQuery] int id)
        {
            var (stream, error, fileInfo) = await _downloadByIdService.DownloadByIdAsync(id);

            if (stream == null)
            {
                return NotFound(new { Message = error });
            }

            var fileName = fileInfo!.OriginalFileName ?? "file.dat";
            var escapedFileName = Uri.EscapeDataString(fileName);

            // 如果已存在，先移除
            Response.Headers.Remove("Content-Disposition");

            // 用索引器赋值，替代 Add
            Response.Headers["Content-Disposition"] =
            $"attachment; filename=\"{fileName}\"; filename*=UTF-8''{escapedFileName}";


            return File(stream, fileInfo.MimeType ?? "application/octet-stream");
        }


        [HttpGet("preview-by-id")]
        public async Task<IActionResult> PreviewById([FromQuery] int id)
        {
            var (stream, error, fileInfo) = await _downloadByIdService.DownloadByIdAsync(id);

            if (stream == null)
            {
                return NotFound(new { Message = error });
            }

            var fileName = fileInfo!.OriginalFileName ?? "file.dat";
            var escapedFileName = Uri.EscapeDataString(fileName);

            // 设置 inline，而不是 attachment
            Response.Headers.Remove("Content-Disposition");
            Response.Headers["Content-Disposition"] =
                $"inline; filename=\"{fileName}\"; filename*=UTF-8''{escapedFileName}";

            // 返回正确的 MIME 类型
            var mimeType = fileInfo.MimeType ?? "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            return File(stream, mimeType);
        }





    }
}
