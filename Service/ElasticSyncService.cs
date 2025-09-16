using Microsoft.EntityFrameworkCore;
using Nest;
using MinioWebBackend.Models;

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
            // 1. 从数据库读取所有文件（包含标签）
            var files = await _db.FileRecords
                .Include(f => f.FileTags!)  // null-forgiving
                    .ThenInclude(ft => ft.Tag)
                .ToListAsync();

            if (files.Count == 0)
            {
                Console.WriteLine("没有需要同步的文件记录。");
                return;
            }

            // 2. 映射 DTO
            var dtos = files.Select(f => ElasticMapper.MapToESDto(f)).ToList();

            // 3. 批量写入 Elasticsearch
            var response = await _elastic.IndexManyAsync(dtos, "files");

            if (!response.IsValid)
            {
                Console.WriteLine($"同步失败: {response.ServerError?.Error.Reason}");
                throw new Exception($"同步到 Elasticsearch 失败: {response.ServerError?.Error.Reason}");
            }

            Console.WriteLine($"成功同步 {dtos.Count} 条文件记录到 Elasticsearch");
        }
    }
}
