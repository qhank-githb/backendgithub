using Microsoft.AspNetCore.Mvc;
using ConsoleApp1.Interfaces;
using ConsoleApp1.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace MinioWebApi.Controllers
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
        public async Task<IActionResult> UploadFile(
            [FromRoute] string bucketName,
            [FromForm] IFormFile file,
            [FromForm] string username,
            [FromForm] string tags) // 新增 tags
        {
            Console.WriteLine($"上传 Bucket: {bucketName}, File: {file?.FileName}, Tags: {tags}");

            if (file == null || file.Length == 0)
                return BadRequest("请选择上传文件");

            if (string.IsNullOrEmpty(username))
                return BadRequest("缺少用户名");

            // 解析 tags
            List<string> tagList = new List<string>();
            if (!string.IsNullOrEmpty(tags))
            {
                try
                {
                    tagList = JsonSerializer.Deserialize<List<string>>(tags);
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
                await file.CopyToAsync(stream);
            }

            try
            {
                // 构造上传请求
                var request = new MultipartUploadRequest
                {
                    bucket = bucketName,
                    originalFileName = file.FileName,
                    storedFileName = $"{username}_{DateTime.Now:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}",
                    filePath = tempFilePath,
                    contentType = file.ContentType,
                    username = username,
                    Tags = tagList
                };

                // 调用上传服务
                var result = await _iUploadService.MultipartUploadAsync(request);

                return Ok(new
                {
                    Bucket = bucketName,
                    OriginalFileName = file.FileName,
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
                // 删除临时文件
                if (System.IO.File.Exists(tempFilePath))
                    System.IO.File.Delete(tempFilePath);
            }
        }
    }
}
