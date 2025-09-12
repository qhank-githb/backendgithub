using System;
using System.Collections.Generic;
using Serilog.Events;

namespace MinioWebBackend.Dtos.LogDtos
{
    /// <summary>
    /// 日志查询请求参数
    /// </summary>
    public class LogQueryRequest
    {
        /// <summary>
        /// 日志级别（可多选：Information/Warning/Error等）
        /// </summary>
        public List<LogEventLevel>? Levels { get; set; }

        /// <summary>
        /// 日志消息关键词（模糊匹配）
        /// </summary>
        public string? MessageKeyword { get; set; }

        /// <summary>
        /// 异常信息关键词（模糊匹配）
        /// </summary>
        public string? ExceptionKeyword { get; set; }

        /// <summary>
        /// 时间戳开始（UTC时间）
        /// </summary>
        public DateTime? TimestampStart { get; set; }

        /// <summary>
        /// 时间戳结束（UTC时间）
        /// </summary>
        public DateTime? TimestampEnd { get; set; }

        /// <summary>
        /// Properties中的键值过滤（如{"MachineName":"localhost"}）
        /// </summary>
        public Dictionary<string, string>? PropertyFilters { get; set; }

        /// <summary>
        /// 分页页码（默认1）
        /// </summary>
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// 每页条数（默认20，最大100）
        /// </summary>
        public int PageSize { get; set; } = 20;

        /// <summary>
        /// 校验分页参数合法性
        /// </summary>
        public void Validate()
        {
            PageIndex = Math.Max(1, PageIndex);
            PageSize = Math.Clamp(PageSize, 1, 100);
        }
    }
}
