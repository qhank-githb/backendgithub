using MinioWebBackend.Dtos.LogDtos;
using MinioWebBackend.Interfaces;
using Nest;

public class LogQueryService : ILogQueryService
{
    private readonly IElasticClient _elastic;

    public LogQueryService(IElasticClient elastic)
    {
        _elastic = elastic;
    }

    public async Task<LogQueryResponse> QueryLogsAsync(LogQueryRequest request)
    {
        request.Validate();

        Func<QueryContainerDescriptor<SerilogLogESDto>, QueryContainer> query = q =>
        {
            var mustQueries = new List<Func<QueryContainerDescriptor<SerilogLogESDto>, QueryContainer>>();

            if (request.Levels?.Count > 0)
            {
                mustQueries.Add(m => m.Terms(t => t.Field(f => f.Level.Suffix("keyword")).Terms(request.Levels.Select(l => l.ToString()))));
            }

            if (!string.IsNullOrEmpty(request.MessageKeyword))
            {
                mustQueries.Add(m => m.Match(ma => ma.Field(f => f.Message).Query(request.MessageKeyword)));
            }

            if (!string.IsNullOrEmpty(request.ExceptionKeyword))
            {
                mustQueries.Add(m => m.Match(ma => ma.Field(f => f.Exception!).Query(request.ExceptionKeyword)));
            }

            if (request.TimestampStart.HasValue || request.TimestampEnd.HasValue)
            {
                mustQueries.Add(m => m.DateRange(dr =>
                {
                    dr.Field(f => f.Timestamp);
                    if (request.TimestampStart.HasValue) dr.GreaterThanOrEquals(request.TimestampStart.Value);
                    if (request.TimestampEnd.HasValue) dr.LessThanOrEquals(request.TimestampEnd.Value);
                    return dr;
                }));
            }

            if (request.PropertyFilters?.Count > 0)
            {
                foreach (var (jsonKey, targetValue) in request.PropertyFilters)
                {
                    mustQueries.Add(m => m.Term(t => t.Field($"properties.{jsonKey}.keyword").Value(targetValue)));
                }
            }

            return mustQueries.Any() ? q.Bool(b => b.Must(mustQueries)) : q.MatchAll();
        };

        // 只查询符合模式的索引，并忽略不存在 timestamp 字段的索引
        var response = await _elastic.SearchAsync<SerilogLogESDto>(s => s
            .Index("myapp-logs-*")   // 只匹配 myapp-logs-* 索引
            .AllowNoIndices(true)     // 索引不存在也不报错
            .IgnoreUnavailable(true)  // 索引不可用也忽略
            .From((request.PageIndex - 1) * request.PageSize)
            .Size(request.PageSize)
            .Query(query)
            .Sort(ss => ss.Descending(f => f.Timestamp))
        );

        if (!response.IsValid)
            throw new Exception(response.ServerError?.ToString() ?? "Elasticsearch 查询失败");

        var totalCount = (int)response.Total;
        var logs = response.Documents.Select(LogItemDto.FromESDto).ToList();

        return new LogQueryResponse
        {
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize),
            CurrentPage = request.PageIndex,
            Logs = logs
        };
    }
}
