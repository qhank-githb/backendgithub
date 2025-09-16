using MinioWebBackend.Dtos.LogDtos;
using MinioWebBackend.Interfaces;
using MinioWebBackend.Models;
using Nest;

namespace MinioWebBackend.Service
{
    public class QueryService : IQueryService
    {
        private readonly IElasticClient _elastic;

        public QueryService(IElasticClient elastic)
        {
            _elastic = elastic;
        }

        public async Task<string?> GetOriginalFileNameAsync(string storedFileName, string bucketName)
        {
            var response = await _elastic.SearchAsync<FileRecordESDto>(s => s
                .Index("files")
                .Size(1)
                .Query(q => q
                    .Bool(b => b
                        .Must(
                            mu => mu.Term(t => t.Field(f => f.StoredFileName).Value(storedFileName)),
                            mu => mu.Term(t => t.Field(f => f.BucketName).Value(bucketName))
                        )
                    )
                )
            );

            return response.Documents.FirstOrDefault()?.OriginalFileName;
        }

        public async Task<List<FileInfoModel>> GetAllFilesAsync()
        {
            var response = await _elastic.SearchAsync<FileRecordESDto>(s => s
                .Index("files")
                .Size(10000) // 尽量返回全部，注意ES默认限制10000
                .Sort(ss => ss.Descending(f => f.UploadTime))
            );

            return response.Documents.Select(MapEsToFileInfo).ToList();
        }

        public async Task<(List<FileInfoModel> Items, int TotalCount)> QueryFilesAsync(
            int? id = null,
            string? uploader = null,
            string? fileName = null,
            string? bucket = null,
            DateTime? start = null,
            DateTime? end = null,
            int pageNumber = 1,
            int pageSize = 10,
            List<string>? tags = null,
            bool matchAllTags = false
        )
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 10, 1000);

            Func<QueryContainerDescriptor<FileRecordESDto>, QueryContainer> query = q =>
            {
                var mustQueries = new List<Func<QueryContainerDescriptor<FileRecordESDto>, QueryContainer>>();

                if (id.HasValue)
                    mustQueries.Add(m => m.Term(t => t.Field(f => f.Id).Value(id.Value)));

                if (!string.IsNullOrWhiteSpace(uploader))
                    mustQueries.Add(m => m.Match(ma => ma.Field(f => f.Uploader).Query(uploader!)));

                if (!string.IsNullOrWhiteSpace(fileName))
                    mustQueries.Add(m => m.Match(ma => ma.Field(f => f.OriginalFileName).Query(fileName!)));

                if (!string.IsNullOrWhiteSpace(bucket))
                    mustQueries.Add(m => m.Match(ma => ma.Field(f => f.BucketName).Query(bucket!)));

                if (start.HasValue || end.HasValue)
                {
                    mustQueries.Add(m => m.DateRange(dr =>
                    {
                        dr.Field(f => f.UploadTime);
                        if (start.HasValue) dr.GreaterThanOrEquals(start.Value);
                        if (end.HasValue) dr.LessThanOrEquals(end.Value);
                        return dr;
                    }));
                }

                if (tags != null && tags.Any())
                {
                    if (matchAllTags)
                    {
                        foreach (var tag in tags)
                        {
                            mustQueries.Add(m => m.Term(t => t.Field(f => f.Tags).Value(tag)));
                        }
                    }
                    else
                    {
                        mustQueries.Add(m => m.Terms(t => t.Field(f => f.Tags).Terms(tags)));
                    }
                }

                if (!mustQueries.Any())
                    return q.MatchAll();

                return q.Bool(b => b.Must(mustQueries));
            };

            var response = await _elastic.SearchAsync<FileRecordESDto>(s => s
                .Index("files")
                .From((pageNumber - 1) * pageSize)
                .Size(pageSize)
                .Query(query)
                .Sort(ss => ss.Descending(f => f.UploadTime))
            );

            if (!response.IsValid)
                throw new Exception(response.ServerError?.ToString() ?? "Elasticsearch 查询失败");

            return (response.Documents.Select(MapEsToFileInfo).ToList(), (int)response.Total);
        }

        public async Task<List<int>> QueryFileIdsAsync(
            int? id = null,
            string? uploader = null,
            string? fileName = null,
            string? bucket = null,
            DateTime? start = null,
            DateTime? end = null
        )
        {
            var (items, _) = await QueryFilesAsync(id, uploader, fileName, bucket, start, end, 1, 10000);
            return items.Select(f => f.Id).ToList();
        }

        public async Task<FileInfoModel?> GetFileByIdAsync(int id)
        {
            var response = await _elastic.GetAsync<FileRecordESDto>(id, idx => idx.Index("files"));
            return response.Found ? MapEsToFileInfo(response.Source) : null;
        }

        private FileInfoModel MapEsToFileInfo(FileRecordESDto record)
        {
            return new FileInfoModel
            {
                Id = record.Id,
                StoredFileName = record.StoredFileName,
                OriginalFileName = record.OriginalFileName,
                Bucketname = record.BucketName,
                RelativePath   = string.Empty,
                AbsolutePath  = string.Empty,
                FileSize = record.FileSize,
                MimeType = record.MimeType,
                UploadTime = record.UploadTime,
                Uploader = record.Uploader,
                ETag = record.ETag ?? string.Empty,
                Tags = record.Tags ?? new List<string>()
            };
        }
    }
}
