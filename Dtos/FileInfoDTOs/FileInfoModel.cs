namespace MinioWebBackend.Dtos.FileInfoDTOs
{
    /// <summary>
/// 文件信息模型（对应数据库中的文件记录）
/// </summary>
/// <remarks>
/// 保存文件的基本信息和元数据，用于文件存储、查询、下载、预览等场景。
/// </remarks>
public class FileInfoModel
{
    /// <summary>
    /// 文件唯一 ID（数据库主键，自增）
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 存储时的文件名（系统生成的唯一名称，用于 MinIO/S3 存储 Key）
    /// </summary>
    public string StoredFileName { get; set; } = string.Empty;

    /// <summary>
    /// 上传时的原始文件名（用户上传的文件名，带扩展名）
    /// </summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// 所属存储桶名称（MinIO/S3 的 Bucket）
    /// </summary>
    public string Bucketname { get; set; } = string.Empty;

    /// <summary>
    /// 文件的相对路径（相对于存储桶或应用的逻辑路径，例如 "2025/09/15/"）
    /// </summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>
    /// 文件的绝对路径（包含存储桶、目录、文件名的完整路径）
    /// </summary>
    public string AbsolutePath { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小（单位：字节）
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// 文件 MIME 类型（如 "image/png", "application/pdf", "text/plain"）
    /// </summary>
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// 文件上传时间（UTC 时间）
    /// </summary>
    public DateTime UploadTime { get; set; }

    /// <summary>
    /// 上传人（用户名或系统标识）
    /// </summary>
    public string Uploader { get; set; } = string.Empty;

    /// <summary>
    /// 文件的 ETag（存储系统返回的校验标识，用于验证文件完整性）
    /// </summary>
    public string ETag { get; set; } = string.Empty;

    /// <summary>
    /// 文件标签（用于分类或搜索，例如 ["合同", "2025", "PDF"]）
    /// </summary>
    public List<string> Tags { get; set; } = new List<string>();
}
}

