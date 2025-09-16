using MinioWebBackend.Dtos.LogDtos;
using MinioWebBackend.Interfaces;
using Nest;
using System;

public class LogQueryService : ILogQueryService
{
    private readonly IElasticClient _elastic;
    private const string IndexPattern = "myapp-logs-*"; // 可改为你真实的索引模式

    public LogQueryService(IElasticClient elastic)
    {
        _elastic = elastic;
    }

    public async Task<LogQueryResponse> QueryLogsAsync(LogQueryRequest request)
    {
        request.Validate();

        // 构建查询
        Func<QueryContainerDescriptor<SerilogLogESDto>, QueryContainer> query = q =>
        {
            var must = new List<Func<QueryContainerDescriptor<SerilogLogESDto>, QueryContainer>>();

            // Levels (枚举 -> 字符串)
            if (request.Levels?.Count > 0)
            {
                // 将 level 转为字符串（例如 "Information"）
                var levelStrings = request.Levels.Select(l => l.ToString()).ToArray();
                must.Add(m => m.Terms(t => t.Field("level.keyword").Terms(levelStrings)));
            }

            // Message 关键字
            if (!string.IsNullOrWhiteSpace(request.MessageKeyword))
            {
                must.Add(m => m.Match(ma => ma.Field(f => f.Message).Query(request.MessageKeyword)));
            }

            // Exception 关键字
            if (!string.IsNullOrWhiteSpace(request.ExceptionKeyword))
            {
                must.Add(m => m.Match(ma => ma.Field(f => f.Exception!).Query(request.ExceptionKeyword)));
            }

            // 时间范围
            if (request.TimestampStart.HasValue || request.TimestampEnd.HasValue)
            {
                must.Add(m => m.DateRange(dr =>
                {
                    dr.Field(f => f.Timestamp);
                    if (request.TimestampStart.HasValue) dr.GreaterThanOrEquals(request.TimestampStart.Value);
                    if (request.TimestampEnd.HasValue) dr.LessThanOrEquals(request.TimestampEnd.Value);
                    return dr;
                }));
            }

            // Properties JSON 属性过滤（例如 properties.userId）
            if (request.PropertyFilters?.Count > 0)
            {
                foreach (var (jsonKey, targetValue) in request.PropertyFilters)
                {
                    if (string.IsNullOrEmpty(jsonKey) || targetValue == null) continue;
                    // 使用 keyword 子字段进行精确匹配
                    var fieldName = $"properties.{jsonKey}.keyword";
                    must.Add(m => m.Term(t => t.Field(fieldName).Value(targetValue)));
                }
            }

            // 如果没有任何条件，返回 MatchAll
            return must.Any() ? q.Bool(b => b.Must(must)) : q.MatchAll();
        };

        try
        {
            var response = await _elastic.SearchAsync<SerilogLogESDto>(s => s
                .Index(IndexPattern)
                .From((request.PageIndex - 1) * request.PageSize)
                .Size(request.PageSize)
                .Query(query)
                .Sort(ss => ss.Descending(f => f.Timestamp))
            );

            if (!response.IsValid)
            {
                // 抛出详细信息，方便排查（包含底层错误、DebugInformation）
                var serverErr = response.ServerError?.ToString();
                throw new Exception($"Elasticsearch 查询失败: {serverErr}\nDebug: {response.DebugInformation}");
            }

            var totalCount = (int)response.Total;
            // 映射到你的 DTO（确保 LogItemDto.FromESDto 存在并正确）
            var logs = response.Documents.Select(LogItemDto.FromESDto).ToList();

            return new LogQueryResponse
            {
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize),
                CurrentPage = request.PageIndex,
                Logs = logs
            };
        }
        catch (Exception ex)
        {
            // 可以在这里记录日志（Serilog）或者向上抛出带更多上下文的信息
            throw new Exception($"查询 Elastic 日志失败: {ex.Message}", ex);
        }
    }
}
