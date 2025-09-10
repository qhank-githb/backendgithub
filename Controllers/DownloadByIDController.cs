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
            var (stream, error, fileInfo) = await _downloadByIdService.DownloadByIdAsync(id);

            if (stream == null)
            {
                return NotFound(new { Message = error });
            }

            var fileName = fileInfo!.OriginalFileName ?? "file.dat";
            var escapedFileName = Uri.EscapeDataString(fileName);

            // 如果已存在，先移除
            Response.Headers.Remove("Content-Disposition");

            //设置响应头，通知浏览器是附件下载。用索引器赋值，替代 Add
            Response.Headers["Content-Disposition"] =
            $"attachment; filename=\"{fileName}\"; filename*=UTF-8''{escapedFileName}";


            return File(stream, fileInfo.MimeType ?? "application/octet-stream");
        }


        /// <summary>
        /// 根据文件 ID 预览文件
        /// </summary>
        /// <param name="id">文件的唯一 ID</param>
        /// <returns>返回文件流，Content-Disposition 为 inline，可直接在浏览器预览；找不到文件返回 404</returns>
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
            // 只保留编码后的文件名格式
            Response.Headers["Content-Disposition"] = $"inline; filename*=UTF-8''{escapedFileName}";

            // 返回正确的 MIME 类型
            var mimeType = fileInfo.MimeType ?? "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            return File(stream, mimeType);
        }






    }
}
