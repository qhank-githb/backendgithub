using Microsoft.AspNetCore.Mvc;
using MinioWebBackend.Interfaces;
using MinioWebBackend.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace MinioWebBackend.Controllers
{
    [ApiController]
    [Route("api/{bucketName}/[controller]")]
    [Authorize] 
    public class FileUploadController : ControllerBase
    {
        private readonly IUploadService _iUploadService;

        public FileUploadController(IUploadService iUploadService)
        {
            _iUploadService = iUploadService;
        }

        /// <summary>
        /// 上传文件
        /// POST /api/{bucketName}/fileupload/upload
        /// </summary>
        [HttpPost("upload")]
[RequestSizeLimit(524_288_000)]
[Consumes("multipart/form-data")]
public async Task<IActionResult> UploadFile(
    [FromRoute] string bucketName,
    [FromForm] FileUploadDto dto)
{
    Console.WriteLine($"上传 Bucket: {bucketName}, File: {dto.File?.FileName}, Tags: {dto.Tags}");

    if (dto.File == null || dto.File.Length == 0)
        return BadRequest("请选择上传文件");

    if (string.IsNullOrEmpty(dto.Username))
        return BadRequest("缺少用户名");

    // 解析 tags
    List<string> tagList = new List<string>();
    if (!string.IsNullOrEmpty(dto.Tags))
    {
        try
        {
            tagList = JsonSerializer.Deserialize<List<string>>(dto.Tags);
        }
        catch
        {
            return BadRequest("tags 格式不正确，应为 JSON 数组");
        }
    }

    // 生成临时文件
    var tempFilePath = Path.GetTempFileName();
    await using (var stream = System.IO.File.Create(tempFilePath))
    {
        await dto.File.CopyToAsync(stream);
    }

    try
    {
        // 构造上传请求
        var request = new MultipartUploadRequest
        {
            bucket = bucketName,
            originalFileName = dto.File.FileName,
            storedFileName = $"{dto.Username}_{DateTime.Now:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}",
            filePath = tempFilePath,
            contentType = dto.File.ContentType,
            username = dto.Username,
            Tags = tagList
        };

        var result = await _iUploadService.MultipartUploadAsync(request);

        return Ok(new
        {
            Bucket = bucketName,
            OriginalFileName = dto.File.FileName,
            StoredFileName = request.storedFileName,
            Size = result.Size,
            ETag = result.ETag,
            Tags = tagList,
            Uploadtime = DateTime.Now
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"上传失败: {ex.Message}");
        return StatusCode(500, $"上传失败: {ex.Message}");
    }
    finally
    {
        if (System.IO.File.Exists(tempFilePath))
            System.IO.File.Delete(tempFilePath);
    }
}

        
    }
}
