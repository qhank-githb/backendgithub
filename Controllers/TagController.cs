using Microsoft.AspNetCore.Mvc;
using MinioWebBackend.Interfaces;
using MinioWebBackend.Models;
using Microsoft.AspNetCore.Authorization;
using MinioWebBackend.Dtos.LogDtos;

[ApiController]
[Route("api/tags")]
//[Authorize] 
public class TagController : ControllerBase
{
    private readonly ITagService _tagService;
    public TagController(ITagService tagService) => _tagService = tagService;

    /// <summary>
    /// 创建新标签
    /// </summary>
    /// <remarks>
    /// 请求示例：
    /// ```json
    /// {
    ///   "name": "合同"
    /// }
    /// ```
    /// 
    /// 响应示例：
    /// ```json
    /// {
    ///   "id": 1,
    ///   "name": "合同"
    /// }
    /// ```
    /// </remarks>
    /// <param name="dto">包含标签名称的 DTO 对象</param>
    /// <returns>返回创建的标签对象</returns>
    /// <response code="201">创建成功</response>
    /// <response code="400">请求错误（如标签已存在）</response>
    [HttpPost]
    public async Task<IActionResult> CreateTag([FromBody] CreateTagDto dto)
    {
        try
        {
            var tag = await _tagService.CreateTagAsync(dto.Name);
            var tagDto = new TagDto { Id = tag.Id, Name = tag.Name };
        return CreatedAtAction(nameof(GetAll), new { id = tag.Id }, tagDto);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// 获取所有标签
    /// </summary>
    /// <remarks>
    /// 响应示例：
    /// ```json
    /// [
    ///   { "id": 1, "name": "合同" },
    ///   { "id": 2, "name": "报告" },
    ///   { "id": 3, "name": "PDF" }
    /// ]
    /// ```
    /// </remarks>
    /// <returns>返回标签列表</returns>
    /// <response code="200">返回标签列表</response>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var tags = await _tagService.GetAllTagsAsync();
        return Ok(tags);
    }
    
    /// <summary>
    /// 修改文件信息（例如更新文件名或标签）
    /// </summary>
    /// <remarks>
    /// 请求示例：
    /// ```json
    ///{
    ///"id": 15,
    ///"fileName": "测试.docx",
    ///"tags": [
    ///     "PPTX",
    ///     "PDF"
    ///         ]
    ///}
    /// ```
    /// 
    /// 响应示例：
    /// ```json
    /// {
    ///   "message": "文件信息修改成功"
    /// }
    /// ```
    /// </remarks>
    /// <param name="dto">包含文件修改信息的 DTO 对象</param>
    /// <returns>返回修改结果</returns>
    /// <response code="200">修改成功</response>
    /// <response code="400">请求错误（如文件不存在）</response>
    [HttpPost("edit")]
    public async Task<IActionResult> EditFile([FromBody] EditFileDto dto)
    {
        await _tagService.EditFileAsync(dto);
        return Ok(new { message = "文件信息修改成功" });
    }
}
