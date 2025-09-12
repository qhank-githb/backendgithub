using MinioWebBackend.Dtos.LogDtos;
using System.Threading.Tasks;

namespace MinioWebBackend.Interfaces
{
    /// <summary>
    /// 日志查询服务接口
    /// </summary>
    public interface ILogQueryService
    {
        /// <summary>
        /// 按条件查询日志
        /// </summary>
        Task<LogQueryResponse> QueryLogsAsync(LogQueryRequest request);
    }
}
