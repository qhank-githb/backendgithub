using MinioWebBackend.Models;
using Nest;
using Microsoft.EntityFrameworkCore;
using MinioWebBackend.Dtos.LogDtos;

namespace MinioWebBackend.Service
{
    public class ElasticSyncService
    {
        private readonly AppDbContext _db;
        private readonly IElasticClient _elastic;

        public ElasticSyncService(AppDbContext db, IElasticClient elastic)
        {
            _db = db;
            _elastic = elastic;
        }

        public async Task SyncAllFilesAsync()
        {
            var files = await _db.FileRecords
                .Include(f => f.FileTags!)
                    .ThenInclude(ft => ft.Tag)
                .ToListAsync();

            var dtos = files.Select(MapToESDto).ToList();

            var response = await _elastic.IndexManyAsync(dtos, "files");

            if (response.ApiCall.HttpStatusCode >= 400 || (response.Errors && response.ItemsWithErrors.Any()))
            {
                var serverErr = response.ServerError?.Error?.Reason ?? "未知错误";
                var debugInfo = response.DebugInformation;
                Console.WriteLine("Elasticsearch 同步失败！");
                Console.WriteLine($"ServerError: {serverErr}");
                Console.WriteLine($"DebugInfo: {debugInfo}");
                throw new Exception($"同步到 Elasticsearch 失败: {serverErr}");
            }


        }

        private FileRecordESDto MapToESDto(FileRecord file)
        {
            return new FileRecordESDto
            {
                Id = file.Id,
                OriginalFileName = file.OriginalFileName,
                StoredFileName = file.StoredFileName,
                BucketName = file.BucketName,
                Uploader = file.Uploader,
                UploadTime = file.UploadTime,
                FileSize = file.FileSize,
                MimeType = file.MimeType,
                ETag = file.ETag,
                Tags = file.FileTags?.Select(ft => ft.Tag?.Name ?? "").ToList() ?? new List<string>()
            };
        }
        

        public async Task IndexFileAsync(FileRecord record)
    {
        var dto = MapToDto(record);
        await _elastic.IndexDocumentAsync(dto);
    }

    public async Task UpdateFileAsync(FileRecord record)
    {
        var dto = MapToDto(record);
        await _elastic.IndexAsync(dto, idx => idx.Index("files"));
    }

    public async Task DeleteFileAsync(int id)
    {
        await _elastic.DeleteAsync<FileRecordESDto>(id, idx => idx.Index("files"));
    }

    private FileRecordESDto MapToDto(FileRecord record)
    {
        return new FileRecordESDto
        {
            Id = record.Id,
            StoredFileName = record.StoredFileName,
            OriginalFileName = record.OriginalFileName,
            BucketName = record.BucketName,
            UploadTime = record.UploadTime,
            Uploader = record.Uploader,
            FileSize = record.FileSize,
            MimeType = record.MimeType,
            Tags = record.FileTags?.Select(ft => ft.Tag!.Name).ToList()
        };
    }
    }
}
