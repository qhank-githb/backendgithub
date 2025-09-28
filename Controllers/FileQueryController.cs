using MinioWebBackend.Interfaces;
using MinioWebBackend.Models;
using Microsoft.AspNetCore.Mvc;
using MinioWebBackend.Dtos.QuertDTOs;
using MinioWebBackend.Dtos.FileInfoDTOs;

namespace MinioWebBackend.Controllers
{
    /// <summary>
    /// 文件查询控制器（需登录访问）
    /// </summary>
    /// <remarks>
    /// 提供以下功能：
    /// - 获取所有文件信息  
    /// - 根据文件 ID 获取单个文件信息  
    /// - 按条件查询文件（分页、时间范围、标签等）  
    /// - 查询符合条件的文件 ID 列表（便于批量下载）  
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize] // ✅ 默认所有接口需登录（JWT）
    public class FileQueryController : ControllerBase
    {
        private readonly IQueryService _iQueryService;

        /// <summary>
        /// 构造函数，注入文件查询服务
        /// </summary>
        /// <param name="iQueryService">文件查询服务接口</param>
        public FileQueryController(IQueryService iQueryService)
        {
            _iQueryService = iQueryService;
        }

        /// <summary>
        /// 获取所有文件信息
        /// </summary>
        /// <returns>
        /// - 成功：返回文件信息列表  
        /// - 失败：返回 500（内部错误）  
        /// </returns>
        /// <response code="200">成功返回文件信息列表</response>
        [HttpGet]
        public async Task<ActionResult<List<FileInfoModel>>> GetAllFiles()
        {
            var files = await _iQueryService.GetAllFilesAsync();
            return Ok(files);
        }

        /// <summary>
        /// 根据文件 ID 获取单个文件信息
        /// </summary>
        /// <param name="id">文件 ID（数据库主键）</param>
        /// <returns>
        /// - 成功：返回文件信息  
        /// - 失败：返回 404（未找到）  
        /// </returns>
        /// <response code="200">成功返回文件信息</response>
        /// <response code="404">未找到文件</response>
        [HttpGet("{id}")]
        public async Task<ActionResult<FileInfoModel>> GetFileById(int id)
        {
            var file = await _iQueryService.GetFileByIdAsync(id);
            if (file == null)
                return NotFound(new { Message = $"未找到 ID={id} 的文件" });

            return Ok(file);
        }

        /// <summary>
        /// 条件查询文件（支持分页、时间范围、标签筛选）
        /// </summary>
        /// <param name="id">文件 ID</param>
        /// <param name="uploader">上传者</param>
        /// <param name="fileName">文件名（支持模糊查询）</param>
        /// <param name="bucket">存储桶名称</param>
        /// <param name="start">上传时间起始</param>
        /// <param name="end">上传时间结束</param>
        /// <param name="pageNumber">页码（默认 1）</param>
        /// <param name="pageSize">每页大小（默认 10）</param>
        /// <param name="tags">标签列表（多选）</param>
        /// <param name="matchAllTags">标签匹配模式：true=全部匹配，false=任意匹配</param>
        /// <returns>分页结果，包含文件列表和总数</returns>
        /// <response code="200">查询成功</response>
        [HttpGet("query")]
        //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult<FileQueryResult>> QueryFiles(
            [FromQuery] int? id,
            [FromQuery] string? uploader,
            [FromQuery] string? fileName,
            [FromQuery] string? bucket,
            [FromQuery] DateTime? start,
            [FromQuery] DateTime? end,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] List<string>? tags = null,
            [FromQuery] bool matchAllTags = false
        )
        {
            var (items, totalCount) = await _iQueryService.QueryFilesAsync(
                id, uploader, fileName, bucket, start, end,
                pageNumber, pageSize,
                tags,
                matchAllTags
            );

            return Ok(new FileQueryResult
            {
                Items = items,
                TotalCount = totalCount
            });
        }

        /// <summary>
        /// 查询符合条件的文件 ID 列表（用于批量下载）
        /// </summary>
        /// <param name="id">文件 ID</param>
        /// <param name="uploader">上传者</param>
        /// <param name="fileName">文件名（支持模糊查询）</param>
        /// <param name="bucket">存储桶名称</param>
        /// <param name="start">上传时间起始</param>
        /// <param name="end">上传时间结束</param>
        /// <returns>
        /// - 成功：返回符合条件的文件 ID 列表 和 符合条件的文件总数
        /// - 失败：返回 500（内部错误）  
        /// </returns>
        /// <response code="200">成功返回文件 ID 列表和符合条件的文件总数 如: {"items":[2,1],"total":2} </response>
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
