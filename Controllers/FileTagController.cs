using Microsoft.AspNetCore.Mvc;
using MinioWebBackend.Interfaces;
using MinioWebBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;


[ApiController]
[Route("api/files")]
[Authorize] 
public class FileTagController : ControllerBase
{
    private readonly IFileTagService _fileTagService;
    private readonly AppDbContext _dbContext;

    public FileTagController(IFileTagService fileTagService, AppDbContext dbcontext)
    {
        _fileTagService = fileTagService;
        _dbContext = dbcontext;
    }


    /// <summary>
    /// 给指定文件添加标签
    /// </summary>
    /// <param name="fileId">文件 ID</param>
    /// <param name="tagIds">标签 ID 列表</param>
    [HttpPost("{fileId}/tags")]
    public async Task<IActionResult> AddTagsToFile(int fileId, [FromBody] List<int> tagIds)
    {
        await _fileTagService.AddTagsToFileAsync(fileId, tagIds);
        return Ok();
    }

     /// <summary>
    /// 根据单个标签名查询文件
    /// </summary>
    /// <param name="tagName">标签名称</param>
    [HttpGet("tag/{tagName}")]
    public async Task<IActionResult> GetFilesByTag(string tagName)
    {
        var files = await _fileTagService.GetFilesByTagAsync(tagName);
        return Ok(files);
    }

    /// <summary>
    /// 根据多个标签名查询文件，可选择匹配所有标签或任意标签
    /// </summary>
    /// <param name="tagNames">标签名称数组</param>
    /// <param name="matchAll">是否匹配所有标签，true=全部匹配，false=任意匹配</param>
    [HttpGet("tags")]
    public async Task<IActionResult> GetFilesByTags([FromQuery] string[] tagNames, [FromQuery] bool matchAll = false)
    {
        var files = await _fileTagService.GetFilesByTagsAsync(tagNames.ToList(), matchAll);
        return Ok(files);
    }

    /// <summary>
    /// 获取指定文件的所有标签
    /// </summary>
    /// <param name="fileId">文件 ID</param>
    [HttpGet("file/{fileId}")]
    public async Task<ActionResult<List<Tag>>> GetTagsByFile(int fileId)
    {
        var tags = await _fileTagService.GetTagsByFileAsync(fileId);
        if (tags == null || tags.Count == 0)
        {
            return NotFound($"No tags found for fileId {fileId}.");
        }

        return Ok(tags);
    }
        


}
