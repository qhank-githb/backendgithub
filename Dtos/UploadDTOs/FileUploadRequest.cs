using Microsoft.AspNetCore.Mvc;

namespace MinioWebBackend.Dtos.UploadDTOs
{
    /// <summary>
/// 文件上传请求 DTO
/// </summary>
public class FileUploadDto
{
    /// <summary>
    /// 上传的文件
    /// </summary>
    [FromForm(Name = "file")]
    public IFormFile? File { get; set; }

    /// <summary>
    /// 上传者用户名
    /// </summary>
    [FromForm(Name = "username")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 文件标签（JSON 数组字符串）
    /// ⚠ 必须是 JSON 数组格式，例如：["合同","PDF"]
    /// </summary>
    [FromForm(Name = "tags")]
    public string Tags { get; set; } = string.Empty;
}
}
