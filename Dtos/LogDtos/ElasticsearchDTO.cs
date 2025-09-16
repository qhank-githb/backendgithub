
namespace MinioWebBackend.Dtos.LogDtos
{
    public class FileRecordESDto
{
    /// <summary>
    /// 文件唯一 ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 上传时的原始文件名（用户上传的文件名）
    /// </summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// 存储时的系统文件名（用于 MinIO/S3 Key）
    /// </summary>
    public string StoredFileName { get; set; } = string.Empty;

    /// <summary>
    /// 文件所属存储桶
    /// </summary>
    public string BucketName { get; set; } = string.Empty;

    /// <summary>
    /// 上传者用户名
    /// </summary>
    public string Uploader { get; set; } = string.Empty;

    /// <summary>
    /// 上传时间
    /// </summary>
    public DateTime UploadTime { get; set; }

    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 文件类型（MIME）
    /// </summary>
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// 文件 ETag（哈希标识）
    /// </summary>
    public string ETag { get; set; } = string.Empty;

    /// <summary>
    /// 文件标签
    /// </summary>
    public List<string> Tags { get; set; } = new List<string>();
}


public class FileSearchDto
{
    public int? Id { get; set; }
    public string? Uploader { get; set; }
    public string? FileName { get; set; }
    public string? Bucket { get; set; }
    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }
    public List<string>? Tags { get; set; }
    public bool MatchAllTags { get; set; } = false;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}


}
