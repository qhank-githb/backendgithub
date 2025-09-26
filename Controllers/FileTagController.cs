using Microsoft.AspNetCore.Mvc;
using MinioWebBackend.Interfaces;
using MinioWebBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;


[ApiController]
[Route("api/files")]
//[Authorize] 
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
    /// 根据单个标签名查询文件
    /// </summary>
    /// <remarks>
    /// 请求示例：
    /// ```
    /// GET /api/files/tag/合同
    /// ```
    /// 返回示例：
    /// ```
    /// {"items":[{"id":3,"storedFileName":"admin_20250915185746791_0b3be68b1dda4662a06eb4d52004b521","originalFileName":"1.pptx","bucketname":"my-bucket","relativePath":"admin_20250915185746791_0b3be68b1dda4662a06eb4d52004b521","absolutePath":"/my-bucket/admin_20250915185746791_0b3be68b1dda4662a06eb4d52004b521","fileSize":1842987,"mimeType":"application/vnd.openxmlformats-officedocument.presentationml.presentation","uploadTime":"2025-09-15T18:57:47.1643764","uploader":"admin","eTag":"","tags":["1"]},{"id":1,"storedFileName":"admin_20250915175010541_520dff1b76134536ae80e67b7f5e3843","originalFileName":"TODO.txt","bucketname":"my-bucket","relativePath":"admin_20250915175010541_520dff1b76134536ae80e67b7f5e3843","absolutePath":"/my-bucket/admin_20250915175010541_520dff1b76134536ae80e67b7f5e3843","fileSize":105,"mimeType":"text/plain","uploadTime":"2025-09-15T17:50:10.6489212","uploader":"admin","eTag":"","tags":["1"]}],"totalCount":2}
    /// ```
    /// </remarks>
    /// <param name="tagName">标签名称</param>
    /// <response code="200">返回匹配的文件列表和数量</response>
    /// <response code="404">没有找到文件</response>
    [HttpGet("tag/{tagName}")]
    public async Task<IActionResult> GetFilesByTag(string tagName)
    {
        var files = await _fileTagService.GetFilesByTagAsync(tagName);
        if (files == null || files.Count == 0)
            return NotFound($"No files found with tag '{tagName}'.");
        return Ok(files);
    }

    /// <summary>
    /// 根据多个标签名查询文件，可选择匹配所有标签或任意标签
    /// </summary>
    /// <remarks>
    /// 请求示例：
    /// ```
    ///GET /api/files/tags?tagNames=合同&amp;tagNames=PDF&amp;matchAll=true
    /// ```
    /// 返回示例：
    /// ```
    /// {"items":[{"id":3,"storedFileName":"admin_20250915185746791_0b3be68b1dda4662a06eb4d52004b521","originalFileName":"1.pptx","bucketname":"my-bucket","relativePath":"admin_20250915185746791_0b3be68b1dda4662a06eb4d52004b521","absolutePath":"/my-bucket/admin_20250915185746791_0b3be68b1dda4662a06eb4d52004b521","fileSize":1842987,"mimeType":"application/vnd.openxmlformats-officedocument.presentationml.presentation","uploadTime":"2025-09-15T18:57:47.1643764","uploader":"admin","eTag":"","tags":["1"]},{"id":1,"storedFileName":"admin_20250915175010541_520dff1b76134536ae80e67b7f5e3843","originalFileName":"TODO.txt","bucketname":"my-bucket","relativePath":"admin_20250915175010541_520dff1b76134536ae80e67b7f5e3843","absolutePath":"/my-bucket/admin_20250915175010541_520dff1b76134536ae80e67b7f5e3843","fileSize":105,"mimeType":"text/plain","uploadTime":"2025-09-15T17:50:10.6489212","uploader":"admin","eTag":"","tags":["1"]}],"totalCount":2}
    /// ```
    /// - `matchAll = true` 表示需要同时包含所有标签  
    /// - `matchAll = false` 表示只要包含任意一个标签即可
    /// </remarks>
    /// <param name="tagNames">标签名称数组</param>
    /// <param name="matchAll">是否匹配所有标签，true=全部匹配，false=任意匹配</param>
    /// <response code="200">返回匹配的文件列表和数量</response>
    /// <response code="404">没有找到文件</response>
    [HttpGet("tags")]
    public async Task<IActionResult> GetFilesByTags([FromQuery] string[] tagNames, [FromQuery] bool matchAll = false)
    {
        var files = await _fileTagService.GetFilesByTagsAsync(tagNames.ToList(), matchAll);
        if (files == null || files.Count == 0)
            return NotFound("No files found with the specified tags.");
        return Ok(files);
    }

    // / <summary>
    // / 获取指定文件的所有标签
    // / </summary>
    // / <remarks>
    // / 请求示例：
    // / ```
    // / GET /api/files/file/123
    // / ```
    // / 响应示例：
    // / ```json
    // / [
    // /   { "id": 1, "name": "合同" },
    // /   { "id": 2, "name": "PDF" }
    // / ]
    // / ```
    // / </remarks>
    // / <param name="fileId">文件 ID</param>
    // / <response code="200">返回该文件的标签列表</response>
    // / <response code="404">没有找到标签</response>
    // [HttpGet("file/{fileId}")]
    // public async Task<ActionResult<List<Tag>>> GetTagsByFile(int fileId)
    // {
    //     var tags = await _fileTagService.GetTagsByFileAsync(fileId);
    //     if (tags == null || tags.Count == 0)
    //     {
    //         return NotFound($"No tags found for fileId {fileId}.");
    //     }

    //     return Ok(tags);
    // }
}
