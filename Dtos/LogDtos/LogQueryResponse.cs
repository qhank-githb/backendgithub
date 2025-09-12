using System.Collections.Generic;

namespace MinioWebBackend.Dtos.LogDtos
{
    /// <summary>
    /// 日志查询响应（分页）
    /// </summary>
    public class LogQueryResponse
    {
        /// <summary>
        /// 总日志条数
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages { get; set; }

        /// <summary>
        /// 当前页码
        /// </summary>
        public int CurrentPage { get; set; }

        /// <summary>
        /// 当前页日志列表
        /// </summary>
        public List<LogItemDto> Logs { get; set; } = new List<LogItemDto>();
    }
}
