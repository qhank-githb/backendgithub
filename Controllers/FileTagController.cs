using Microsoft.AspNetCore.Mvc;
using MinioWebBackend.Dtos.LogDtos;
using MinioWebBackend.Dtos.TagDTOs;
using MinioWebBackend.Interfaces;
using MinioWebBackend.Models;


[ApiController]
[Route("api/files")]
//[Authorize] 
public class FileTagController : ControllerBase
{
    private readonly IFileTagService _fileTagService;

    public FileTagController(IFileTagService fileTagService)
    {
        _fileTagService = fileTagService;
    }



    /// <summary>
    /// 根据单个标签名查询文件
    /// </summary>
    /// <remarks>
    /// 请求示例：
    /// ```
    /// GET /api/files/tag/测试
    /// ```
    /// 返回示例：
    /// ```
    /// [{
    ///     "id": 18,
    ///     "originalFileName": "测试docx.docx",
    ///     "storedFileName": "admin_20250925181323165_ede2ddf0e09e4f2e8343741b5a9f30f1",
    ///     "bucketName": "my-bucket",
    ///     "uploader": "admin",
    ///     "uploadTime": "2025-09-25T18:13:23.362577",
    ///     "fileSize": 13555,
    ///     "mimeType": "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    ///     "eTag": "\"3327a5262d1956c3d969057a87574be5-1\"",
    ///   "tags": [
    ///       "压缩文件",
    ///       "xlxs",
    ///       "测试",
    ///       "图片",
    ///       "合同"
    ///     ]
    ///   }]
    /// ```
    /// </remarks>
    /// <param name="tagName">标签名称</param>
    /// <response code="200">返回匹配的文件列表和数量</response>
    /// <response code="404">没有找到文件</response>
    [HttpGet("tag/{tagName}")]
    public async Task<ActionResult<List<FileRecordESDto>>> GetFilesByTag(string tagName)
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
    /// [{
    ///     "id": 18,
    ///     "originalFileName": "测试docx.docx",
    ///     "storedFileName": "admin_20250925181323165_ede2ddf0e09e4f2e8343741b5a9f30f1",
    ///     "bucketName": "my-bucket",
    ///     "uploader": "admin",
    ///     "uploadTime": "2025-09-25T18:13:23.362577",
    ///     "fileSize": 13555,
    ///     "mimeType": "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
    ///     "eTag": "\"3327a5262d1956c3d969057a87574be5-1\"",
    ///   "tags": [
    ///       "压缩文件",
    ///       "xlxs",
    ///       "测试",
    ///       "图片",
    ///       "合同"
    ///     ]
    ///   }]
    /// ```
    /// - `matchAll = true` 表示需要同时包含所有标签  
    /// - `matchAll = false` 表示只要包含任意一个标签即可
    /// </remarks>
    /// <param name="tagNames">标签名称数组</param>
    /// <param name="matchAll">是否匹配所有标签，true=全部匹配，false=任意匹配</param>
    /// <response code="200">返回匹配的文件列表和数量</response>
    /// <response code="404">没有找到文件</response>
    [HttpGet("tags")]
    public async Task<ActionResult<List<FileWithTagsDto>>>  GetFilesByTags([FromQuery] string[] tagNames, [FromQuery] bool matchAll = false)
    {
        var files = await _fileTagService.GetFilesByTagsAsync(tagNames.ToList(), matchAll);
        if (files == null || files.Count == 0)
            return NotFound("No files found with the specified tags.");
        return Ok(files);
    }


}
