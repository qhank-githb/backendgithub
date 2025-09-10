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
