using Microsoft.AspNetCore.Mvc;
using ConsoleApp1.Interfaces;
using ConsoleApp1.Models;

[ApiController]
[Route("api/tags")]
public class TagController : ControllerBase
{
    private readonly ITagService _tagService;
    public TagController(ITagService tagService) => _tagService = tagService;

    [HttpPost]
    public async Task<IActionResult> CreateTag([FromBody] CreateTagDto dto)
    {
        try
        {
            var tag = await _tagService.CreateTagAsync(dto.Name);
    return CreatedAtAction(nameof(GetAll), new { id = tag.Id }, tag);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tags = await _tagService.GetAllTagsAsync();
        return Ok(tags);
    }
}


