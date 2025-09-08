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

    /// <summary>
    /// 创建新标签
    /// </summary>
    /// <param name="dto">包含标签名称的 DTO 对象</param>
    /// <returns>返回创建的标签对象</returns>
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

    /// <summary>
    /// 获取所有标签
    /// </summary>
    /// <returns>返回标签列表</returns>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tags = await _tagService.GetAllTagsAsync();
        return Ok(tags);
    }
    
    /// <summary>
    /// 修改文件信息
    /// </summary>
    /// <param name="dto">包含文件修改信息的 DTO 对象</param>
    /// <returns>返回修改结果</returns>
     [HttpPost("edit")]
    public async Task<IActionResult> EditFile([FromBody] EditFileDto dto)
    {
        await _tagService.EditFileAsync(dto);
        return Ok(new { message = "文件信息修改成功" });
    }

}


