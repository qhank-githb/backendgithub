using Microsoft.AspNetCore.Mvc;
using MinioWebBackend.Interfaces;
using MinioWebBackend.Models;
using Microsoft.AspNetCore.Authorization;

[ApiController]
[Route("api/tags")]
[Authorize] 
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
    
     [HttpPost("edit")]
    public async Task<IActionResult> EditFile([FromBody] EditFileDto dto)
    {
        await _tagService.EditFileAsync(dto);
        return Ok(new { message = "文件信息修改成功" });
    }

}


