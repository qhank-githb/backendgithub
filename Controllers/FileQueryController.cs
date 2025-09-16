using MinioWebBackend.Dtos.LogDtos;
using MinioWebBackend.Interfaces;
using MinioWebBackend.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MinioWebBackend.Controllers
{
    /// <summary>
    /// 文件查询控制器（基于 Elasticsearch 查询，需登录访问）
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
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class FileQueryController : ControllerBase
    {
        private readonly IQueryService _queryService;

        public FileQueryController(IQueryService queryService)
        {
            _queryService = queryService;
        }

        /// <summary>
        /// 获取所有文件信息
        /// </summary>
        /// <returns>文件信息列表</returns>
        [HttpGet]
        public async Task<ActionResult<List<FileInfoModel>>> GetAllFiles()
        {
            var files = await _queryService.GetAllFilesAsync();
            return Ok(files);
        }

        /// <summary>
        /// 根据文件 ID 获取单个文件信息
        /// </summary>
        /// <param name="id">文件 ID</param>
        /// <returns>文件信息</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<FileInfoModel>> GetFileById(int id)
        {
            var file = await _queryService.GetFileByIdAsync(id);
            if (file == null)
                return NotFound(new { Message = $"未找到 ID={id} 的文件" });

            return Ok(file);
        }

        /// <summary>
        /// 条件查询文件（分页、时间范围、标签等）
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
        [HttpGet("query")]
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
            var (items, totalCount) = await _queryService.QueryFilesAsync(
                id, uploader, fileName, bucket, start, end,
                pageNumber, pageSize,
                tags, matchAllTags
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
        /// <returns>文件 ID 列表及总数</returns>
        [HttpGet("query-ids")]
        public async Task<IActionResult> QueryFileIds(
           [FromQuery] int? id,
           [FromQuery] string? uploader,
           [FromQuery] string? fileName,
           [FromQuery] string? bucket,
           [FromQuery] DateTime? start,
           [FromQuery] DateTime? end)
        {
            var ids = await _queryService.QueryFileIdsAsync(
                id, uploader, fileName, bucket, start, end
            );

            return Ok(new { items = ids, total = ids.Count });
        }
    }
}
