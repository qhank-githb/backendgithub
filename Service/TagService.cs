using ConsoleApp1.Interfaces;
using ConsoleApp1.Models;
using Microsoft.EntityFrameworkCore;


namespace ConsoleApp1.Service
{
    public class TagService : ITagService
    {
        private readonly AppDbContext _context;
        public TagService(AppDbContext context) => _context = context;

        public async Task<Tag> CreateTagAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("标签名不能为空");

            var exists = await _context.Tags.AnyAsync(t => t.Name == name);
            if (exists) throw new InvalidOperationException("标签已存在");

            var tag = new Tag { Name = name };
            _context.Tags.Add(tag);
            await _context.SaveChangesAsync();
            return tag;
        }

        public async Task<List<Tag>> GetAllTagsAsync()
        {
            return await _context.Tags.ToListAsync();
        }

        private readonly AppDbContext _dbContext;


public async Task EditFileAsync(EditFileDto dto)
{
    if (dto == null) throw new ArgumentNullException(nameof(dto));

    // 1. 查找文件
    var file = await _context.FileRecords
        .Include(f => f.FileTags)
        .FirstOrDefaultAsync(f => f.Id == dto.Id);

    if (file == null)
        throw new FileNotFoundException("文件不存在");

    // 2. 更新文件名
    file.OriginalFileName = dto.FileName;

    // 3. 更新标签
    if (file.FileTags != null)
        _context.FileTags.RemoveRange(file.FileTags);

    if (dto.Tags == null)
        dto.Tags = new List<string>();

    var newTags = new List<FileTag>();
    foreach (var tagName in dto.Tags)
    {
        var tag = await _context.Tags.FirstOrDefaultAsync(t => t.Name == tagName);
        if (tag == null)
        {
            tag = new Tag { Name = tagName };
            await _context.Tags.AddAsync(tag);
            await _context.SaveChangesAsync(); // 保存后才有 Id
        }
        newTags.Add(new FileTag { FileId = file.Id, TagId = tag.Id });
    }

    await _context.FileTags.AddRangeAsync(newTags);

    // 4. 保存事务
    await _context.SaveChangesAsync();
}


}

}