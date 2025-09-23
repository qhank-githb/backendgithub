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
    /// 提供基于 Elasticsearch 的日志查询功能：
    /// - 支持按日志等级(Levels)过滤
    /// - 支持按消息关键字(MessageKeyword)和异常关键字(ExceptionKeyword)过滤
    /// - 支持时间范围过滤(TimestampStart / TimestampEnd)
    /// - 支持自定义 JSON 字段过滤(PropertyFilters)，如 fields.ActionName="Login"
    /// - 支持分页返回结果(PageIndex / PageSize)
    /// 
    /// 注意：
    /// - 普通用户不可访问，仅限 Admin 角色
    /// - 返回的日志列表为 <see cref="LogItemDto"/> 对象
    /// </remarks>
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(Roles = "Admin")]
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
        /// <param name="request">
        /// 日志查询参数对象：
        /// - Levels：日志等级列表（Verbose, Debug, Information, Warning, Error, Fatal）
        /// - MessageKeyword：日志消息关键字
        /// - ExceptionKeyword：异常消息关键字
        /// - TimestampStart / TimestampEnd：查询时间范围
        /// - PropertyFilters：JSON 属性过滤，如 fields.ActionName="Login"
        /// - PageIndex / PageSize：分页信息
        /// </param>
        /// <returns>
        /// 返回 <see cref="LogQueryResponse"/>：
        /// - Logs：符合条件的日志列表（每条日志为 <see cref="LogItemDto"/>）
        /// - TotalCount：总匹配条数
        /// - TotalPages：总页数
        /// - CurrentPage：当前页码
        /// </returns>
        [HttpGet("query")]
        [SwaggerOperation(
            Summary = "查询日志",
            Description = "基于 Elasticsearch 的日志查询，支持等级、关键字、异常关键字、时间范围、JSON 属性字段以及分页查询，仅 Admin 可用"
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
