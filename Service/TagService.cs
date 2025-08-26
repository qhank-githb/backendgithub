using ConsoleApp1.Interfaces;
using ConsoleApp1.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using ConsoleApp1.Options;


namespace ConsoleApp1.Service
{
    public class TagService : ITagService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public TagService(AppDbContext context) => _context = context;

        public async Task<Tag> CreateTagAsync(string name)
        {
            var username = _httpContextAccessor.HttpContext?.User?.Identity?.Name 
               ?? _httpContextAccessor.HttpContext?.User?.FindFirst("username")?.Value
               ?? "匿名用户";

            try
            {
                // 业务操作
                Log.Information("用户 {Username} 尝试创建标签 {TagName}", username ?? "匿名用户", name);

                var exists = await _context.Tags.AnyAsync(t => t.Name == name);
                if (exists)
                    throw new InvalidOperationException("标签已存在");

                var tag = new Tag { Name = name };
                _context.Tags.Add(tag);
                await _context.SaveChangesAsync();

                Log.Information("用户 {Username} 成功创建标签 {TagName}", username ?? "匿名用户", name);
                return tag;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "用户 {Username} 创建标签 {TagName} 出错", username ?? "匿名用户", name);
                throw;
            }
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