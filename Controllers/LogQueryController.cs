using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MinioWebBackend.Dtos.LogDtos;
using MinioWebBackend.Interfaces;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace MinioWebBackend.Controllers
{
    /// <summary>
    /// 日志查询控制器（仅管理员可访问）
    /// </summary>
    /// <remarks>
    /// 提供日志的高级查询功能：支持通过日志等级、关键词、异常关键字、时间范围等条件进行过滤，
    /// 并支持分页返回结果。
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // 仅管理员可访问
    public class LogQueryController : ControllerBase
    {
        private readonly ILogQueryService _logQueryService;
        private readonly ILogger<LogQueryController> _logger;

        /// <summary>
        /// 构造函数，注入日志查询服务和日志记录器
        /// </summary>
        public LogQueryController(
            ILogQueryService logQueryService,
            ILogger<LogQueryController> logger)
        {
            _logQueryService = logQueryService;
            _logger = logger;
        }

        /// <summary>
        /// 按条件查询日志（支持分页）
        /// </summary>
        /// <remarks>
        /// 示例请求：
        /// 
        /// ```
        /// GET /api/LogQuery/query?Levels=4&amp;MessageKeyword=数据库&amp;Page=1&amp;PageSize=50
        /// ```
        /// 
        /// - **Levels**: 日志等级集合 (如 `4 = Warning`, `5 = Error`)
        /// - **MessageKeyword**: 日志消息关键词
        /// - **ExceptionKeyword**: 异常关键词
        /// - **TimestampStart**: 起始时间 (UTC)
        /// - **TimestampEnd**: 结束时间 (UTC)
        /// - **Page**: 页码，从 1 开始
        /// - **PageSize**: 每页大小
        /// </remarks>
        /// <param name="request">日志查询参数对象，包含等级、关键词、时间范围及分页信息</param>
        /// <returns>
        /// 分页后的日志查询结果。
        /// 返回 <see cref="LogQueryResponse"/>，包含日志条目集合、总记录数和分页信息。
        /// </returns>
        [HttpGet("query")]
        [SwaggerOperation(
            Summary = "查询日志",
            Description = "根据等级、关键词、时间范围、分页参数等条件查询日志记录，仅管理员可用"
        )]
        [Produces("application/json")]
        [ProducesResponseType(typeof(LogQueryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<LogQueryResponse>> QueryLogs([FromQuery] LogQueryRequest request)
        {
            try
            {
                var result = await _logQueryService.QueryLogsAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "日志查询失败，参数: {Request}", JsonSerializer.Serialize(request));
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new { Message = "查询失败，请稍后重试" }
                );
            }
        }
    }
}
