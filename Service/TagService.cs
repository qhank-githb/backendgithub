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
        // 1. 查找文件
        var file = await _dbContext.FileRecords
            .Include(f => f.FileTags)  // 包含标签关联
            .FirstOrDefaultAsync(f => f.Id == dto.Id);

        if (file == null)
            throw new FileNotFoundException("文件不存在");

        // 2. 更新文件名
        file.OriginalFileName = dto.FileName;

        // 3. 更新标签
        // 删除旧标签
        _dbContext.FileTags.RemoveRange(file.FileTags);

        // 插入新标签
        var newTags = dto.Tags.Select(tagName => new FileTag
        {
            FileId = file.Id,
            TagId = _dbContext.Tags
                .Where(t => t.Name == tagName)
                .Select(t => t.Id)
                .FirstOrDefault() // 假设 tag 已存在
        }).ToList();

        await _dbContext.FileTags.AddRangeAsync(newTags);

        // 4. 保存事务
        await _dbContext.SaveChangesAsync();
    }
}

}