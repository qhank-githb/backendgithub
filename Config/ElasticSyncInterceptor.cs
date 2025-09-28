using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using MinioWebBackend.Dtos.LogDtos; // 假设 FileRecordESDto 在这里
using MinioWebBackend.Models;
using Nest;

public class ElasticSyncInterceptor : SaveChangesInterceptor
{
    private readonly IElasticClient _elastic;
    private readonly ILogger<ElasticSyncInterceptor> _logger;
    private static readonly ConcurrentDictionary<Guid, List<ChangeDescriptor>> _pending =
        new ConcurrentDictionary<Guid, List<ChangeDescriptor>>();

    private const string IndexName = "files";
    private const int BulkBatchSize = 500; // 可按需调整

    public ElasticSyncInterceptor(IElasticClient elastic, ILogger<ElasticSyncInterceptor> logger)
    {
        _elastic = elastic ?? throw new ArgumentNullException(nameof(elastic));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    private class ChangeDescriptor
    {
        public EntityState State { get; init; }
        public FileRecord Entity { get; init; } = null!;
    }

    // 仅收集变更（不与 ES 通信）
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        var entries = context.ChangeTracker.Entries<FileRecord>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
            .Select(e => new ChangeDescriptor { State = e.State, Entity = e.Entity })
            .ToList();

        if (entries.Count > 0)
        {
            var ctxId = context.ContextId.InstanceId;
            _pending[ctxId] = entries;
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    // DB 提交成功后再批量同步到 ES（此时自增 Id 已生成）
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null) return await base.SavedChangesAsync(eventData, result, cancellationToken);

        var ctxId = context.ContextId.InstanceId;
        if (!_pending.TryRemove(ctxId, out var changes) || changes.Count == 0)
            return await base.SavedChangesAsync(eventData, result, cancellationToken);

        try
        {
            var toIndex = new List<FileRecordESDto>();
            var toDeleteIds = new List<int>();

            foreach (var desc in changes)
            {
                var entity = desc.Entity;

                if (desc.State == EntityState.Deleted)
                {
                    // 删除仅需 Id（Save 后 Id 仍然可读）
                    toDeleteIds.Add(entity.Id);
                    continue;
                }

                // 为了防止导航未加载，显式从 DB 投影 tags（不把整个导航拉入内存）
                List<string> tags = new List<string>();
                try
                {
                    var collection = context.Entry(entity).Collection(e => e.FileTags);
                    tags = await collection
                        .Query()
                        .Include(ft => ft.Tag) // FileTag.Tag 导航存在
                        .Select(ft => ft.Tag != null ? ft.Tag.Name : string.Empty)
                        .Where(tn => !string.IsNullOrEmpty(tn))
                        .ToListAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "加载 FileTags 失败，fileId={FileId}，将索引为空标签列表。", entity.Id);
                    tags = new List<string>();
                }

                var esDto = new FileRecordESDto
                {
                    Id = entity.Id,
                    StoredFileName = entity.StoredFileName,
                    OriginalFileName = entity.OriginalFileName,
                    BucketName = entity.BucketName,
                    FileSize = entity.FileSize,
                    MimeType = entity.MimeType,
                    UploadTime = entity.UploadTime,
                    Uploader = entity.Uploader,
                    ETag = entity.ETag ?? string.Empty,
                    Tags = tags ?? new List<string>()
                };

                toIndex.Add(esDto);
            }

            // 分批提交 index/update（Bulk）
            for (int i = 0; i < toIndex.Count; i += BulkBatchSize)
            {
                var batch = toIndex.Skip(i).Take(BulkBatchSize).ToList();
                var bulkResp = await _elastic.BulkAsync(b => b
                    .IndexMany(batch)
                    .Index(IndexName), cancellationToken);

                if (bulkResp == null)
                {
                    _logger.LogError("Elasticsearch BulkAsync 返回 null（index batch）。");
                    continue;
                }

                if (bulkResp.Errors)
                {
                    foreach (var item in bulkResp.ItemsWithErrors)
                    {
                        _logger.LogError("ES index error op={Op} id={Id} error={Error}", item.Operation, item.Id, item.Error?.Reason ?? item.Error?.ToString());
                    }
                }
            }

            // 分批提交 delete（Bulk）
            for (int i = 0; i < toDeleteIds.Count; i += BulkBatchSize)
            {
                var batch = toDeleteIds.Skip(i).Take(BulkBatchSize).ToList();
                var bulkReq = new BulkDescriptor();
                foreach (var id in batch)
                {
                    bulkReq = bulkReq.Delete<FileRecordESDto>(d => d.Index(IndexName).Id(id));
                }

                var bulkResp = await _elastic.BulkAsync(bulkReq, cancellationToken);
                if (bulkResp == null)
                {
                    _logger.LogError("Elasticsearch BulkAsync 返回 null（delete batch）。");
                    continue;
                }

                if (bulkResp.Errors)
                {
                    foreach (var item in bulkResp.ItemsWithErrors)
                    {
                        _logger.LogError("ES delete error op={Op} id={Id} error={Error}", item.Operation, item.Id, item.Error?.Reason ?? item.Error?.ToString());
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // **关键**：ES 异常不应阻塞 DB 提交。记录异常并（可选）写入 outbox 以便后台重试。
            _logger.LogError(ex, "将变更同步到 Elasticsearch 失败（已捕获，数据库已提交）。建议将失败写入 outbox 以便后台重试。");
            // TODO: 在这里写入 outbox 表或其他持久化重试机制
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    // Save 失败时清理 pending
    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context != null)
        {
            _pending.TryRemove(context.ContextId.InstanceId, out _);
        }

        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }
}
