using Microsoft.AspNetCore.Mvc;
using ConsoleApp1.Interfaces;

[ApiController]
[Route("api/files")]
public class FileTagController : ControllerBase
{
    private readonly IFileTagService _fileTagService;

    public FileTagController(IFileTagService fileTagService)
    {
        _fileTagService = fileTagService;
    }

    [HttpPost("{fileId}/tags")]
    public async Task<IActionResult> AddTagsToFile(int fileId, [FromBody] List<int> tagIds)
    {
        await _fileTagService.AddTagsToFileAsync(fileId, tagIds);
        return Ok();
    }

    [HttpGet("tag/{tagName}")]
    public async Task<IActionResult> GetFilesByTag(string tagName)
    {
        var files = await _fileTagService.GetFilesByTagAsync(tagName);
        return Ok(files);
    }

    [HttpGet("tags")]
    public async Task<IActionResult> GetFilesByTags([FromQuery] string[] tagNames, [FromQuery] bool matchAll = false)
    {
        var files = await _fileTagService.GetFilesByTagsAsync(tagNames.ToList(), matchAll);
        return Ok(files);
    }

}
