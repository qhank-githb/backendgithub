using MinioWebBackend.Dtos.LogDtos;
using MinioWebBackend.Models;

namespace MinioWebBackend.Service
{
    public static class ElasticMapper
    {
        public static FileRecordESDto MapToESDto(FileRecord file)
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
    }
}
