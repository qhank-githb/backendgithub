using MinioWebBackend.Interfaces;
using MinioWebBackend.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace MinioWebBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] 
    public class FileQueryController : ControllerBase
    {
        private readonly IQueryService _iQueryService;

        public FileQueryController(IQueryService iQueryService)
        {
            _iQueryService = iQueryService;
        }

        /// <summary>
        /// 获取所有文件信息
        /// </summary>
        // GET: /api/FileInfo
        [HttpGet]
        public async Task<ActionResult<List<FileInfoModel>>> GetAllFiles()
        {
            var files = await _iQueryService.GetAllFilesAsync();
            return Ok(files);
        }


        /// <summary>
        /// 根据文件 ID 获取文件信息
        /// </summary>
        /// <param name="id">文件 ID</param>
        // GET: /api/FileInfo/5
        [HttpGet("{id}")]
        public async Task<ActionResult<FileInfoModel>> GetFileById(int id)
        {
            var file = await _iQueryService.GetFileByIdAsync(id);
            if (file == null)
                return NotFound();

            return Ok(file);
        }


        /// <summary>
        /// 查询文件（支持分页、时间范围、标签筛选）
        /// </summary>
        /// <param name="id">文件 ID</param>
        /// <param name="uploader">上传者</param>
        /// <param name="fileName">文件名</param>
        /// <param name="bucket">桶名</param>
        /// <param name="start">开始时间</param>
        /// <param name="end">结束时间</param>
        /// <param name="pageNumber">分页页码</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="tags">标签列表</param>
        /// <param name="matchAllTags">是否全部匹配标签</param> 
        [HttpGet("query")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
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


        /// <summary>
        /// 查询符合条件的文件 ID 列表
        /// </summary>
        /// <param name="id">文件 ID</param>
        /// <param name="uploader">上传者</param>
        /// <param name="fileName">文件名</param>
        /// <param name="bucket">桶名</param>
        /// <param name="start">开始时间</param>
        /// <param name="end">结束时间</param>
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
