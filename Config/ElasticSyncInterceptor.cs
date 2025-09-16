using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MinioWebBackend.Dtos.LogDtos;
using MinioWebBackend.Models;
using Nest;

public class ElasticSyncInterceptor : SaveChangesInterceptor
{
    private readonly IElasticClient _elastic;

    public ElasticSyncInterceptor(IElasticClient elastic)
    {
        _elastic = elastic;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, 
        InterceptionResult<int> result, 
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null) return await base.SavingChangesAsync(eventData, result, cancellationToken);

        var entries = context.ChangeTracker.Entries<FileRecord>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted);

        foreach (var entry in entries)
        {
            var esDto = new FileRecordESDto
            {
                Id = entry.Entity.Id,
                StoredFileName = entry.Entity.StoredFileName,
                OriginalFileName = entry.Entity.OriginalFileName,
                BucketName = entry.Entity.BucketName,
                FileSize = entry.Entity.FileSize,
                MimeType = entry.Entity.MimeType,
                UploadTime = entry.Entity.UploadTime,
                Uploader = entry.Entity.Uploader,
                ETag = string.Empty,
                Tags = entry.Entity.FileTags?.Select(ft => ft.Tag?.Name ?? "").ToList() ?? new List<string>()
            };

            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
            {
                await _elastic.IndexAsync(esDto, i => i.Index("files"), cancellationToken);
            }
            else if (entry.State == EntityState.Deleted)
            {
                await _elastic.DeleteAsync<FileRecordESDto>(esDto.Id, d => d.Index("files"), cancellationToken);
            }
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
