using Microsoft.EntityFrameworkCore;
using MinioWebBackend.Dtos.LogDtos;
using MinioWebBackend.Interfaces;
using MinioWebBackend.Models;

namespace MinioWebBackend.Service
{
    public class LogQueryService : ILogQueryService
    {
        private readonly AppDbContext _dbContext;
        private readonly bool _isMySql;
        private readonly bool _isSqlServer;

        public LogQueryService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
            var providerName = _dbContext.Database.ProviderName;
            _isMySql = !string.IsNullOrEmpty(providerName) && providerName.Contains("MySql");
            _isSqlServer = !string.IsNullOrEmpty(providerName) && providerName.Contains("SqlServer");
        }

        public async Task<LogQueryResponse> QueryLogsAsync(LogQueryRequest request)
        {
            request.Validate();
            var query = _dbContext.SerilogLogs.AsQueryable();

            // 日志级别过滤
            if (request.Levels?.Count > 0)
            {
                var levelStrings = request.Levels.ConvertAll(l => l.ToString());
                query = query.Where(log => levelStrings.Contains(log.Level));
            }

            // 消息/异常关键词过滤
            if (!string.IsNullOrEmpty(request.MessageKeyword))
                query = query.Where(log => log.Message.Contains(request.MessageKeyword));
            if (!string.IsNullOrEmpty(request.ExceptionKeyword))
                query = query.Where(log => log.Exception != null && log.Exception.Contains(request.ExceptionKeyword));

            // 时间范围过滤
            if (request.TimestampStart.HasValue)
                query = query.Where(log => log.Timestamp >= request.TimestampStart.Value);
            if (request.TimestampEnd.HasValue)
                query = query.Where(log => log.Timestamp <= request.TimestampEnd.Value);

            // JSON 属性过滤
            if (request.PropertyFilters?.Count > 0)
            {
                foreach (var (jsonKey, targetValue) in request.PropertyFilters)
                {
                    string jsonPath = $"$.{jsonKey}.Value";

                    if (_isMySql)
                    {
                        // ⚡ 用 SQL 原生 JSON_EXTRACT 查询
                        query = _dbContext.SerilogLogs
                            .FromSqlRaw(@"
                                SELECT * FROM SerilogLogs
                                WHERE JSON_UNQUOTE(JSON_EXTRACT(Properties, {0})) = {1}", 
                                jsonPath, targetValue);
                    }
                    else if (_isSqlServer)
                    {
                        query = query.Where(log =>
                            log.Properties != null &&
                            SqlServerJsonFunctions.JsonValue(log.Properties, jsonPath) == targetValue
                        );
                    }
                }
            }

            // 排序 & 分页
            query = query.OrderByDescending(log => log.Timestamp);
            var totalCount = await query.CountAsync();
            var logs = await query
                .Skip((request.PageIndex - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return new LogQueryResponse
            {
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize),
                CurrentPage = request.PageIndex,
                Logs = logs.ConvertAll(LogItemDto.FromEntity)
            };
        }
    }

    /// <summary>
    /// SQL Server JSON_VALUE 映射
    /// </summary>
    public static class SqlServerJsonFunctions
    {
        [DbFunction(name: "JSON_VALUE", IsBuiltIn = true)]
        public static string JsonValue(string expression, string path)
            => throw new NotSupportedException("仅用于 EF Core 查询翻译，不能在本地调用。");
    }
}
