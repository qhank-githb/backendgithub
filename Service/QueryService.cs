using MinioWebBackend.Models;
using MinioWebBackend.Interfaces;
using Microsoft.EntityFrameworkCore;
using MinioWebBackend.Dtos.FileInfoDTOs;


namespace MinioWebBackend.Service
{
    public class QueryService : IQueryService
    {
        private readonly AppDbContext _db;

        public QueryService(AppDbContext db)
        {
            _db = db;
        }


        public async Task<string?> GetOriginalFileNameAsync(string storedFileName, string bucketName)
        {
            return await _db.FileRecords
                .Where(f => f.StoredFileName == storedFileName && f.BucketName == bucketName)
                .Select(f => f.OriginalFileName)
                .FirstOrDefaultAsync();
        }

        public async Task<List<FileInfoModel>> GetAllFilesAsync()
        {
            var files = await _db.FileRecords
                .Include(f => f.FileTags!)
                    .ThenInclude(ft => ft.Tag)
                .OrderByDescending(f => f.UploadTime)
                .ToListAsync();

            return files.Select(MapRecordToFileInfo).ToList();
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

            var query = _db.FileRecords
                .Include(f => f.FileTags!)
                    .ThenInclude(ft => ft.Tag)
                .AsQueryable();

            // 基础条件
            if (id.HasValue)
                query = query.Where(f => f.Id == id.Value);

            if (!string.IsNullOrWhiteSpace(uploader))
                query = query.Where(f => f.Uploader.Contains(uploader));

            if (!string.IsNullOrWhiteSpace(fileName))
                query = query.Where(f => f.OriginalFileName.Contains(fileName));

            if (!string.IsNullOrWhiteSpace(bucket))
                query = query.Where(f => f.BucketName.Contains(bucket));

            if (start.HasValue)
                query = query.Where(f => f.UploadTime >= start.Value);

            if (end.HasValue)
                query = query.Where(f => f.UploadTime <= end.Value);

            // 标签筛选（按 TagId）
            if (tags != null && tags.Count > 0)
            {
                // 把 string 转换成 int
                var tagIds = tags
                    .Select(t => int.TryParse(t, out var id) ? id : (int?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .ToList();

            if (tagIds.Count > 0)
            {
                if (matchAllTags)
                {
                    // 文件必须包含所有指定标签 ID
                    foreach (var tagId in tagIds)
                    {
                        query = query.Where(f => f.FileTags!.Any(ft => ft.TagId == tagId));
                    }
                }
                else
                {
                    // 文件包含任意一个标签 ID
                    query = query.Where(f => f.FileTags!.Any(ft => tagIds.Contains(ft.TagId)));
                }
            }
        }


            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(f => f.UploadTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items.Select(MapRecordToFileInfo).ToList(), totalCount);
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
            var query = _db.FileRecords.AsQueryable();

            if (id.HasValue)
                query = query.Where(f => f.Id == id.Value);

            if (!string.IsNullOrWhiteSpace(uploader))
                query = query.Where(f => f.Uploader.Contains(uploader));

            if (!string.IsNullOrWhiteSpace(fileName))
                query = query.Where(f => f.OriginalFileName.Contains(fileName));

            if (!string.IsNullOrWhiteSpace(bucket))
                query = query.Where(f => f.BucketName.Contains(bucket));

            if (start.HasValue)
                query = query.Where(f => f.UploadTime >= start.Value);

            if (end.HasValue)
                query = query.Where(f => f.UploadTime <= end.Value);

            return await query
                .OrderByDescending(f => f.UploadTime)
                .Select(f => f.Id)
                .ToListAsync();
        }

        public async Task<FileInfoModel?> GetFileByIdAsync(int id)
        {
            var record = await _db.FileRecords
                .Include(f => f.FileTags!)
                    .ThenInclude(ft => ft.Tag)
                .FirstOrDefaultAsync(f => f.Id == id);

            return record != null ? MapRecordToFileInfo(record) : null;
        }

        private FileInfoModel MapRecordToFileInfo(FileRecord record)
        {
            return new FileInfoModel
            {
                Id = record.Id,
                StoredFileName = record.StoredFileName,
                OriginalFileName = record.OriginalFileName,
                Bucketname = record.BucketName,
                RelativePath = record.RelativePath,
                AbsolutePath = record.AbsolutePath,
                FileSize = record.FileSize,
                MimeType = record.MimeType,
                UploadTime = record.UploadTime,
                Uploader = record.Uploader,
                ETag = string.Empty, // FileRecord 里没有 etag 字段，你需要看是否要加
                Tags = record.FileTags?
                .Where(ft => ft.Tag != null)
                .Select(ft => ft.Tag!.Name)  // ! 告诉编译器这里一定不为 null
                .ToList() ?? new List<string>()
            };
        }
    }
}
