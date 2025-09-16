using Microsoft.AspNetCore.Mvc;
using MinioWebBackend.Dtos.LogDtos;
using MinioWebBackend.Service;

namespace MinioWebBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ElasticSearchController : ControllerBase
    {
        private readonly ElasticSearchService _elasticService;

        public ElasticSearchController(ElasticSearchService elasticService)
        {
            _elasticService = elasticService;
        }

        /// <summary>
        /// 查询文件（Elasticsearch）
        /// </summary>
        /// <remarks>
        /// 支持按 Id、文件名、上传者、存储桶、标签查询，支持分页。
        /// 标签支持 MatchAll（必须包含所有）和 MatchAny（包含任意一个）。
        /// </remarks>
        /// <param name="dto">查询条件 DTO</param>
        /// <returns>
        /// - Items: 文件记录列表  
        /// - TotalCount: 总记录数
        /// </returns>
        [HttpPost("search")]
        public async Task<IActionResult> SearchFiles([FromBody] FileSearchDto dto)
        {
            if (dto.PageNumber <= 0) dto.PageNumber = 1;
            if (dto.PageSize <= 0) dto.PageSize = 10;

            var (items, totalCount) = await _elasticService.SearchFilesAsync(dto);

            return Ok(new
            {
                Items = items,
                TotalCount = totalCount
            });
        }
    }
}
