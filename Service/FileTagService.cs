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

        public async Task<List<FileRecord>> GetFilesByTagsAsync(List<string> tagNames, bool matchAll)
        {
            if (tagNames == null || tagNames.Count == 0)
                return new List<FileRecord>();

            // 找出 tag ids
            var tagIds = await _context.Tags
                .Where(t => tagNames.Contains(t.Name))
                .Select(t => t.Id)
                .ToListAsync();

            if (!tagIds.Any()) return new List<FileRecord>();

            IQueryable<int> fileIdsQuery;

        if (matchAll)
        {
            var groups = await _context.FileTags
                .Where(ft => tagIds.Contains(ft.TagId))
                .GroupBy(ft => ft.FileId)
                .Select(g => new
                {
                    FileId = g.Key,
                    TagIds = g.Select(ft => ft.TagId).Distinct().ToList()
                })
                .ToListAsync();

            fileIdsQuery = groups
                .Where(g => tagIds.All(tid => g.TagIds.Contains(tid)))
                .Select(g => g.FileId)
                .AsQueryable();
        }
        else
        {
            fileIdsQuery = _context.FileTags
                .Where(ft => tagIds.Contains(ft.TagId))
                .Select(ft => ft.FileId)
                .Distinct();
}


            var fileIds = await fileIdsQuery.ToListAsync();

            return await _context.FileRecords
                .Where(f => fileIds.Contains(f.Id))
                .ToListAsync();
        }


}
