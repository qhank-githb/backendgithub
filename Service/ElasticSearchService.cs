using MinioWebBackend.Dtos.LogDtos;
using Nest;

namespace MinioWebBackend.Service
{
    public class ElasticSearchService
    {
        private readonly IElasticClient _elastic;

        public ElasticSearchService(IElasticClient elastic)
        {
            _elastic = elastic;
        }

        public async Task<(List<FileRecordESDto> Items, int TotalCount)> SearchFilesAsync(FileSearchDto dto)
        {
            // 构建 BoolQuery
            Func<QueryContainerDescriptor<FileRecordESDto>, QueryContainer> query = q =>
            {
                var mustQueries = new List<Func<QueryContainerDescriptor<FileRecordESDto>, QueryContainer>>();

                if (dto.Id.HasValue)
                    mustQueries.Add(m => m.Term(t => t.Field(f => f.Id).Value(dto.Id.Value)));

                if (!string.IsNullOrWhiteSpace(dto.FileName))
                    mustQueries.Add(m => m.Match(ma => ma.Field(f => f.OriginalFileName).Query(dto.FileName!)));

                if (!string.IsNullOrWhiteSpace(dto.Uploader))
                    mustQueries.Add(m => m.Match(ma => ma.Field(f => f.Uploader).Query(dto.Uploader)));

                if (!string.IsNullOrWhiteSpace(dto.Bucket))
                    mustQueries.Add(m => m.Match(ma => ma.Field(f => f.BucketName).Query(dto.Bucket)));

                // 标签逻辑
                if (dto.Tags != null && dto.Tags.Any())
                {
                    if (dto.MatchAllTags)
                    {
                        // 必须包含所有标签
                        foreach (var tag in dto.Tags)
                        {
                            mustQueries.Add(m => m.Term(t => t.Field(f => f.Tags).Value(tag)));
                        }
                    }
                    else
                    {
                        // 任意标签即可
                        mustQueries.Add(m => m.Terms(t => t.Field(f => f.Tags).Terms(dto.Tags)));
                    }
                }

                if (!mustQueries.Any())
                {
                    return q.MatchAll(); // 如果没有条件，返回全部
                }

                return q.Bool(b => b.Must(mustQueries));
            };

            var searchResponse = await _elastic.SearchAsync<FileRecordESDto>(s => s
                .Index("files")
                .From((dto.PageNumber - 1) * dto.PageSize)
                .Size(dto.PageSize)
                .Query(query)
            );

            if (!searchResponse.IsValid)
                throw new Exception(searchResponse.ServerError?.ToString() ?? "Elasticsearch 查询失败");

            return (searchResponse.Documents.ToList(), (int)searchResponse.Total);
        }
    }
}
