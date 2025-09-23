using Microsoft.AspNetCore.Mvc;
using MinioWebBackend.Interfaces;
using MinioWebBackend.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.Annotations;

namespace MinioWebBackend.Controllers
{
    [ApiController]
    [Route("api/{bucketName}/[controller]")]
    //[Authorize] 
    public class FileUploadController : ControllerBase
    {
        private readonly IUploadService _iUploadService;

        public FileUploadController(IUploadService iUploadService)
        {
            _iUploadService = iUploadService;
        }

        /// <summary>
        /// 上传文件到指定 Bucket
        /// </summary>
        /// <remarks>
        /// ```
        /// /// 请求示例（multipart/form-data）：
        ///— URL 参数：
        /// - bucketName: 指定上传的存储桶名称
        ///
        /// Form 表单参数:
        /// ```text
        /// file: 要上传的文件 (IFormFile)
        /// username: 上传者用户名 (string)
        /// description: 文件描述信息，可选 (string)
        /// tags: 文件标签列表，可选，多值可用逗号分隔 (string)
        /// ```
        ///
        /// 示例请求：
        ///— POST /api/files/upload/my-bucket
        ///— Content-Type: multipart/form-data
        ///
        /// FormData:
        ///Form 表单参数:
        /// | 字段名   | 类型       | 必填 | 描述                                        | 示例                     |
        /// |----------|-----------|------|-------------------------------------------|-------------------------|
        /// | file     | IFormFile | 是   | 要上传的文件                                | example.pdf             |
        /// | username | string    | 是   | 上传者用户名                                | testUser                |
        /// | tags     | string    | 否   | 文件标签（JSON 数组字符串），例如：["合同","PDF"] | ["合同","PDF"]           |
        /// ```
        /// </remarks>
        /// <param name="bucketName">桶名称</param>
        /// <param name="dto">上传参数（文件 + 用户名 + 标签）</param>
        /// <returns>返回上传结果，包括文件元信息和存储名</returns>
        /// <response code="200">上传成功，返回文件信息
        /// 响应示例：
        /// ```json
        ///{"bucket":"my-bucket",
        /// "originalFileName":"测试docx.docx",
        /// "storedFileName":"admin_20250915191437425_e225e6662e5b44f4a4e0593756b87814",
        /// "size":13555,
        /// "eTag":"\"3327a5262d1956c3d969057a87574be5-1\"",
        /// "tags":["1"],
        /// "uploadtime":"2025-09-15T19:14:38.0656739+08:00"}
        /// ```
        /// </response>
        /// <response code="400">请求错误（如缺少文件、用户名或 tags 格式错误）</response>
        /// <response code="500">服务器错误（如 MinIO 上传失败）</response>
        [HttpPost("upload")]
        [RequestSizeLimit(524_288_000)]
        [Consumes("multipart/form-data")]
                public async Task<IActionResult> UploadFile(
            [FromRoute] string bucketName,
             [FromForm] FileUploadDto dto)
        {
            Console.WriteLine($"上传 Bucket: {bucketName}, File: {dto.File?.FileName}, Tags: {dto.Tags}");

            if (dto.File == null)
                return BadRequest("请选择上传文件");

            if (string.IsNullOrEmpty(dto.Username))
                return BadRequest("缺少用户名");

            // 解析 tags
            List<string> tagList = new List<string>();
            if (!string.IsNullOrEmpty(dto.Tags))
            {
                try
                {
                    tagList = JsonSerializer.Deserialize<List<string>>(dto.Tags) ?? new List<string>();

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
