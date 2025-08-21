using ConsoleApp1.Interfaces;
using ConsoleApp1.Models;
using Microsoft.AspNetCore.Mvc;


namespace ConsoleApp1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileQueryController : ControllerBase
    {
        private readonly IQueryService _iQueryService;

        public FileQueryController(IQueryService iQueryService)
        {
            _iQueryService = iQueryService;
        }

        // GET: /api/FileInfo
        [HttpGet]
        public async Task<ActionResult<List<FileInfoModel>>> GetAllFiles()
        {
            var files = await _iQueryService.GetAllFilesAsync();
            return Ok(files);
        }

        // GET: /api/FileInfo/5
        [HttpGet("{id}")]
        public async Task<ActionResult<FileInfoModel>> GetFileById(int id)
        {
            var file = await _iQueryService.GetFileByIdAsync(id);
            if (file == null)
                return NotFound();

            return Ok(file);
        }



        [HttpGet("query")]
        public async Task<ActionResult<FileQueryResult>> QueryFiles(
                // 原有查询参数
                [FromQuery] int? id,
                [FromQuery] string? uploader,
                [FromQuery] string? fileName,
                [FromQuery] string? bucket,
                [FromQuery] DateTime? start,
                [FromQuery] DateTime? end,
                // 新增分页参数（带默认值，兼容旧调用）
                [FromQuery] int pageNumber = 1,
                [FromQuery] int pageSize = 10,
                // 新增标签筛选参数
                [FromQuery] List<string>? tags = null,            // 前端多选标签
                [FromQuery] bool matchAllTags = false            // true=全部匹配, false=任意匹配
        )
        {
            var (items, totalCount) = await _iQueryService.QueryFilesAsync(
                id, uploader, fileName, bucket, start, end,
                pageNumber, pageSize,
                tags,                 // 传递标签参数
                matchAllTags          // 传递匹配模式
            );

            return Ok(new FileQueryResult
            {
                Items = items,
                TotalCount = totalCount
            });
        }

            

    [HttpGet("query-ids")]
    public async Task<IActionResult> QueryFileIds(
       [FromQuery] int? id,
       [FromQuery] string? uploader,
       [FromQuery] string? fileName,
       [FromQuery] string? bucket,
       [FromQuery] DateTime? start,
       [FromQuery] DateTime? end)
    {
        var ids = await _iQueryService.QueryFileIdsAsync(
            id, uploader, fileName, bucket, start, end
        );

        return Ok(new { items = ids, total = ids.Count });
}




    }
}
