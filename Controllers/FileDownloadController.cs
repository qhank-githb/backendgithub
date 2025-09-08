using Microsoft.AspNetCore.Mvc;
using MinioWebBackend.Interfaces;
using Microsoft.AspNetCore.Authorization;


namespace MinioWebBackend.Controllers
{
    [ApiController]
    [Route("api/file")]
    [Authorize] 
    public class FileDownloadController : ControllerBase
    {
        private readonly IDownloadService _idownloadService;

        public FileDownloadController(IDownloadService idownloadService)
        {
            _idownloadService = idownloadService;
        }

         /// <summary>
        /// 下载指定桶中的单个文件
        /// </summary>
        /// <param name="bucket">桶名</param>
        /// <param name="originalfilename">原始文件名</param>
        [HttpGet("/api/{bucket}/file/download")]
        public async Task<IActionResult> Download([FromRoute] string bucket, [FromQuery] string originalfilename)
        {
            var (stream, error) = await _idownloadService.DownloadFileAsync(bucket, originalfilename);
            if (stream == null)
                return StatusCode(500, new { Error = "DownloadFailed", Message = error });

            return File(stream, "application/octet-stream", originalfilename);
        }


        /// <summary>
        /// 批量下载文件，返回 ZIP 压缩包
        /// </summary>
        /// <param name="ids">文件 ID 列表</param>
        [HttpPost("batch-download")]
        public async Task<IActionResult> BatchDownloadByIds([FromBody] List<int> ids)
        {
            (Stream? zipStream, string? error) = await _idownloadService.BatchDownloadByIdsAsync(ids);

            if (zipStream == null || zipStream == Stream.Null)
            {
                return BadRequest(new { Error = "BatchDownloadFailed", Message = error });
            }

            return File(zipStream, "application/zip", $"batch_{DateTime.Now:yyyyMMddHHmmss}.zip");
        }







    }
}
