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
}

}