using MinioWebBackend.Interfaces;
using MinioWebBackend.Models;
using Microsoft.EntityFrameworkCore;


public class FileTagService : IFileTagService
{
    private readonly AppDbContext _context;
    public FileTagService(AppDbContext context) => _context = context;

    public async Task AddTagsToFileAsync(int fileId, List<int> tagIds)
    {
        foreach (var tagId in tagIds)
        {
            if (!await _context.FileTags.AnyAsync(ft => ft.FileId == fileId && ft.TagId == tagId))
            {
                _context.FileTags.Add(new FileTag { FileId = fileId, TagId = tagId });
            }
        }
        await _context.SaveChangesAsync();
    }

    public async Task<List<FileRecord>> GetFilesByTagAsync(string tagName)
    {
        return await _context.FileTags
            .Join(_context.Tags, ft => ft.TagId, t => t.Id, (ft, t) => new { ft, t })
            .Where(x => x.t.Name == tagName)
            .Join(_context.FileRecords, x => x.ft.FileId, f => f.Id, (x, f) => f)
            .ToListAsync();
    }

    public async Task<List<Tag>> GetTagsByFileAsync(int fileId)
    {
        return await _context.FileTags
            .Where(ft => ft.FileId == fileId)
            .Join(_context.Tags, ft => ft.TagId, t => t.Id, (ft, t) => t)
            .ToListAsync();
    }

public async Task<List<FileWithTagsDto>> GetFilesByTagsAsync(List<string> tagNames, bool matchAll)
{
    if (tagNames == null || tagNames.Count == 0)
        return new List<FileWithTagsDto>();

    // 找出 tag ids
    var tagIds = await _context.Tags
        .Where(t => tagNames.Contains(t.Name))
        .Select(t => t.Id)
        .ToListAsync();

    if (!tagIds.Any()) return new List<FileWithTagsDto>();

    List<int> fileIds;

    if (matchAll)
    {
        // 第一步：数据库查询，取出分组和对应的标签集合
        var groups = await _context.FileTags
            .Where(ft => tagIds.Contains(ft.TagId))
            .GroupBy(ft => ft.FileId)
            .Select(g => new
            {
                FileId = g.Key,
                TagIds = g.Select(ft => ft.TagId).Distinct().ToList()
            })
            .ToListAsync();

        // 第二步：在内存里过滤
        fileIds = groups
            .Where(g => tagIds.All(tid => g.TagIds.Contains(tid)))
            .Select(g => g.FileId)
            .ToList();
    }
    else
    {
        // 任意匹配
        fileIds = await _context.FileTags
            .Where(ft => tagIds.Contains(ft.TagId))
            .Select(ft => ft.FileId)
            .Distinct()
            .ToListAsync();
    }

    // ✅ 查询文件并直接投影成 DTO，只包含 Tag.Name
    return await _context.FileRecords
        .Where(f => fileIds.Contains(f.Id))
        .Select(f => new FileWithTagsDto
        {
            Id = f.Id,
            OriginalFileName = f.OriginalFileName,
            StoredFileName = f.StoredFileName,
            BucketName = f.BucketName,
            RelativePath = f.RelativePath,
            AbsolutePath = f.AbsolutePath,
            FileSize = f.FileSize,
            MimeType = f.MimeType,
            UploadTime = f.UploadTime,
            Uploader = f.Uploader,
            Tags = f.FileTags.Select(ft => ft.Tag.Name).ToList()
        })
        .ToListAsync();
}




}
