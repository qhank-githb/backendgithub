using Microsoft.AspNetCore.Mvc;
using ConsoleApp1.Interfaces;
using Microsoft.AspNetCore.Authorization;


namespace MinioWebApi.Controllers
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

        [HttpGet("/api/{bucket}/file/download")]
        public async Task<IActionResult> Download([FromRoute] string bucket, [FromQuery] string originalfilename)
        {
            var (stream, error) = await _idownloadService.DownloadFileAsync(bucket, originalfilename);
            if (stream == null)
                return StatusCode(500, new { Error = "DownloadFailed", Message = error });

            return File(stream, "application/octet-stream", originalfilename);
        }

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
