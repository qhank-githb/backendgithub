//标签的添加，编辑。获取标签

using MinioWebBackend.Interfaces;
using MinioWebBackend.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using MinioWebBackend.Dtos.LogDtos;
using MinioWebBackend.Dtos.EditFileDTOs;



namespace MinioWebBackend.Service
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


       public async Task<List<TagDto>> GetAllTagsAsync()
        {
            return await _context.Tags
                .Select(t => new TagDto
                {
                    Id = t.Id,
                    Name = t.Name
                })
                .ToListAsync();
        }




            public async Task EditFileAsync(EditFileDto dto)
{
    if (dto == null)
        throw new ArgumentNullException(nameof(dto));

    try
    {
        var username = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "未知用户";

        var file = await _context.FileRecords
            .Include(f => f.FileTags!)
                .ThenInclude(ft => ft.Tag)
            .FirstOrDefaultAsync(f => f.Id == dto.Id);

        if (file == null)
            throw new FileNotFoundException($"文件不存在，Id={dto.Id}");

        bool anyChange = false;

        // 1. 文件名变化
        var oldName = file.OriginalFileName;
        if (!string.Equals(oldName, dto.FileName, StringComparison.Ordinal))
        {
            file.OriginalFileName = dto.FileName;
            anyChange = true;
            Log.Information("【编辑文件】用户 {User} 修改文件名：{OldName} → {NewName}", username, oldName, dto.FileName);
        }

        // 2. 标签变化
        var newTagNames = (dto.Tags ?? new List<string>())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var oldTagNames = file.FileTags?
            .Select(ft => ft.Tag?.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => n!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();

        var oldSet = new HashSet<string>(oldTagNames, StringComparer.OrdinalIgnoreCase);
        var newSet = new HashSet<string>(newTagNames, StringComparer.OrdinalIgnoreCase);

        var toAddNames = newSet.Except(oldSet).ToList();
        var toRemoveNames = oldSet.Except(newSet).ToList();

        if (toAddNames.Count > 0 || toRemoveNames.Count > 0)
        {
            // 新增标签
            if (toAddNames.Count > 0)
            {
                var existingAddTags = await _context.Tags
                    .Where(t => toAddNames.Contains(t.Name))
                    .ToListAsync();

                var missingNames = toAddNames
                    .Except(existingAddTags.Select(t => t.Name), StringComparer.OrdinalIgnoreCase)
                    .ToList();

                foreach (var name in missingNames)
                {
                    var newTag = new Tag { Name = name };
                    _context.Tags.Add(newTag);
                    existingAddTags.Add(newTag);
                }

                if (missingNames.Count > 0)
                    await _context.SaveChangesAsync(); // 确保新标签有 Id

                foreach (var tag in existingAddTags)
                    _context.FileTags.Add(new FileTag { FileId = file.Id, TagId = tag.Id });
            }

            // 移除标签
            if (toRemoveNames.Count > 0 && file.FileTags?.Count > 0)
            {
                var toRemove = file.FileTags
                    .Where(ft => ft.Tag != null && toRemoveNames.Contains(ft.Tag.Name, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                if (toRemove.Count > 0)
                    _context.FileTags.RemoveRange(toRemove);
            }

            anyChange = true;
            Log.Information("【编辑文件】用户 {User} 标签变更：新增 [{Add}]，移除 [{Remove}]",
                username,
                string.Join(",", toAddNames),
                string.Join(",", toRemoveNames));
        }

        // 3. 有变更才保存
        if (anyChange)
            await _context.SaveChangesAsync();
    }
    catch (Exception ex)
    {
        Log.Error(ex, "【编辑文件出错】文件 Id={FileId}", dto?.Id);
        throw;
    }
}





}

}