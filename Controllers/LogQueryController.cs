using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MinioWebBackend.Dtos.LogDtos;
using MinioWebBackend.Interfaces;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace MinioWebBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize] // 需登录访问
    public class LogQueryController : ControllerBase
    {
        private readonly ILogQueryService _logQueryService;
        private readonly ILogger<LogQueryController> _logger;

        public LogQueryController(ILogQueryService logQueryService, ILogger<LogQueryController> logger)
        {
            _logQueryService = logQueryService;
            _logger = logger;
        }

        /// <summary>
        /// 按条件查询日志
        /// </summary>
        [HttpGet("query")]
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
                return StatusCode(500, new { Message = "查询失败，请稍后重试" });
            }
        }
    }
}
