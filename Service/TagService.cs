using ConsoleApp1.Interfaces;
using ConsoleApp1.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;


namespace ConsoleApp1.Service
{
    public class TagService : ITagService
    {
        private readonly AppDbContext _context;
        
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TagService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

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



    public async Task EditFileAsync(EditFileDto dto)
    {
        if (dto == null) 
        {
            Log.Error("【编辑文件失败】参数为空");
            throw new ArgumentNullException(nameof(dto));
        }

        try
        {
            // 假设这里有获取用户名的方法
            var username = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "未知用户";

            // 1. 查找文件
            var file = await _context.FileRecords
                .Include(f => f.FileTags)
                .FirstOrDefaultAsync(f => f.Id == dto.Id);

            if (file == null)
            {
                Log.Warning("【编辑文件失败】用户 {User} 尝试编辑不存在的文件，Id={FileId}", username, dto.Id);
                throw new FileNotFoundException("文件不存在");
            }

            // 2. 更新文件名
            var oldName = file.OriginalFileName;
            file.OriginalFileName = dto.FileName;
            Log.Information("【编辑文件】用户 {User} 修改文件名：{OldName} → {NewName}", username, oldName, dto.FileName);

            // 3. 更新标签
            if (file.FileTags != null)
            {
                _context.FileTags.RemoveRange(file.FileTags);
                Log.Information("【编辑文件】用户 {User} 删除了文件 Id={FileId} 的所有旧标签", username, file.Id);
            }

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
                    Log.Information("【编辑文件】用户 {User} 新建标签：{TagName}", username, tagName);
                }
                newTags.Add(new FileTag { FileId = file.Id, TagId = tag.Id });
            }

            await _context.FileTags.AddRangeAsync(newTags);
            Log.Information("【编辑文件】用户 {User} 为文件 Id={FileId} 设置新标签：{Tags}", username, file.Id, string.Join(",", dto.Tags));

            // 4. 保存事务
            await _context.SaveChangesAsync();
            Log.Information("【编辑文件完成】用户 {User} 成功编辑了文件 Id={FileId}", username, file.Id);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "【编辑文件出错】文件 Id={FileId}", dto?.Id);
            throw;
        }
    }



}

}