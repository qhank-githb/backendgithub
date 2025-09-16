using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;


namespace MinioWebBackend.Models
{

/// <summary>
/// 文件上传结果（接口返回模型）
/// </summary>
/// <remarks>
/// 上传成功后，返回文件的元数据信息。
/// </remarks>
public class UploadResult
{
    /// <summary>
    /// 上传时的原始文件名（用户上传的文件名，带扩展名）
    /// </summary>
    /// <example>合同2025.pdf</example>
    public string Originalfilename { get; set; } = string.Empty;

    /// <summary>
    /// 文件的 ETag（存储系统返回的唯一标识，用于校验文件完整性）
    /// </summary>
    /// <example>5d41402abc4b2a76b9719d911017c592</example>
    public string ETag { get; set; } = string.Empty;

    /// <summary>
    /// 文件大小（单位：字节）
    /// </summary>
    /// <example>204800</example>
    public long Size { get; set; }

    /// <summary>
    /// 所属存储桶名称（MinIO/S3 的 Bucket）
    /// </summary>
    /// <example>documents</example>
    public string Bucketname { get; set; } = string.Empty;

    /// <summary>
    /// 上传者用户名
    /// </summary>
    /// <example>admin</example>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 文件标签（用于分类或搜索）
    /// </summary>
    /// <example>["合同","PDF","2025"]</example>
    public List<string> Tags { get; set; } = new List<string>();

    /// <summary>
    /// 文件上传时间（UTC 时间）
    /// </summary>
    /// <example>2025-09-15T12:30:00Z</example>
    public DateTime Uploadtime { get; set; }
}




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




/// <summary>
/// 分片上传请求模型（后端内部使用）
/// </summary>
/// <remarks>
/// 用于封装上传文件所需的元数据和参数，传递给 MinIO/S3 服务执行上传。
/// </remarks>
public class MultipartUploadRequest
{
    /// <summary>
    /// 存储桶名称（MinIO/S3 的 Bucket）
    /// </summary>
    /// <example>documents</example>
    public string bucket { get; set; } = string.Empty;

    /// <summary>
    /// 上传时的原始文件名（用户上传的文件名，带扩展名）
    /// </summary>
    /// <example>合同2025.pdf</example>
    public string originalFileName { get; set; } = string.Empty;

    /// <summary>
    /// 存储时的文件名（系统生成的唯一名称，用于 MinIO/S3 存储 Key）
    /// </summary>
    /// <example>admin_20250915123045001_a7f9e2c13b4d4f8e9e11demo</example>
    public string storedFileName { get; set; } = string.Empty;

    /// <summary>
    /// 本地临时文件路径（后端生成，用于传输到 MinIO/S3）
    /// </summary>
    /// <example>C:\temp\minio\upload_tmp_12345.pdf</example>
    public string filePath { get; set; } = string.Empty;

    /// <summary>
    /// 文件 MIME 类型（如 "image/png", "application/pdf"）
    /// </summary>
    /// <example>application/pdf</example>
    public string contentType { get; set; } = string.Empty;

    /// <summary>
    /// 上传者用户名
    /// </summary>
    /// <example>admin</example>
    public string username { get; set; } = string.Empty;

    /// <summary>
    /// 文件标签列表（用于分类或搜索）
    /// </summary>
    /// <example>["合同","PDF","2025"]</example>
    public List<string> Tags { get; set; } = new List<string>();
}


/// <summary>
/// 文件查询结果模型
/// </summary>
/// <remarks>
/// 用于分页查询时返回数据，包含：
/// - Items：当前页的文件信息列表
/// - TotalCount：符合条件的总记录数（用于分页计算）
/// // ```
/// 
/// 返回示例：
/// ```json
/// {
///   "items": [
///     {
///       "id": 101,
///       "storedFileName": "a7f9e2c1-3b4d-4f8e-9e11-demo.pdf",
///       "originalFileName": "合同2025.pdf",
///       "bucketname": "documents",
///       "relativePath": "2025/09/",
///       "absolutePath": "documents/2025/09/a7f9e2c1-3b4d-4f8e-9e11-demo.pdf",
///       "fileSize": 204800,
///       "mimeType": "application/pdf",
///       "uploadTime": "2025-09-15T12:30:00Z",
///       "uploader": "admin",
///       "etag": "5d41402abc4b2a76b9719d911017c592",
///       "tags": [ "合同", "PDF", "2025" ]
///     },
///     {
///       "id": 102,
///       "storedFileName": "f2d3e5c9-7a6b-4f0c-8a11-report.docx",
///       "originalFileName": "报告.docx",
///       "bucketname": "documents",
///       "relativePath": "2025/09/",
///       "absolutePath": "documents/2025/09/f2d3e5c9-7a6b-4f0c-8a11-report.docx",
///       "fileSize": 102400,
///       "mimeType": "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
///       "uploadTime": "2025-09-14T09:15:00Z",
///       "uploader": "user01",
///       "etag": "098f6bcd4621d373cade4e832627b4f6",
///       "tags": [ "报告", "Word" ]
///     }
///   ],
///   "totalCount": 25
/// }
/// ```
/// </remarks>
public class FileQueryResult
{
    /// <summary>
    /// 当前页的文件列表
    /// </summary>
    public List<FileInfoModel> Items { get; set; } = new();

    /// <summary>
    /// 符合条件的总文件数（不受分页限制）
    /// </summary>
    public int TotalCount { get; set; }
}

   /// <summary>
    /// 文件记录表实体（映射数据库表 file_info）
    /// </summary>
    /// <remarks>
    /// 存储在数据库中的文件元数据，用于文件查询、下载、预览和标签管理。
    /// 与 MinIO/S3 的对象存储对应，每条记录代表一个文件。
    /// </remarks>
    [Table("file_info")]
    public class FileRecord
    {
        /// <summary>
        /// 文件唯一 ID（数据库主键，自增）
        /// </summary>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// 存储时的文件名（系统生成的唯一名称，用作 MinIO/S3 的对象 Key）
        /// </summary>
        /// <example>admin_20250915123045_demo.pdf</example>
        [Column("stored_file_name")]
        public string StoredFileName { get; set; } = string.Empty;

        /// <summary>
        /// 上传时的原始文件名（用户本地文件名，带扩展名）
        /// </summary>
        /// <example>合同2025.pdf</example>
        [Column("original_file_name")]
        public string OriginalFileName { get; set; } = string.Empty;

        /// <summary>
        /// 存储桶名称（MinIO/S3 的 Bucket）
        /// </summary>
        /// <example>documents</example>
        [Column("bucketname")]
        public string BucketName { get; set; } = string.Empty;

        /// <summary>
        /// 文件的相对路径（逻辑目录，例如 "2025/09/15/"）
        /// </summary>
        /// <example>2025/09/</example>
        [Column("relative_path")]
        public string RelativePath { get; set; } = string.Empty;

        /// <summary>
        /// 文件的绝对路径（包含存储桶和完整 Key）
        /// </summary>
        /// <example>documents/2025/09/admin_20250915123045_demo.pdf</example>
        [Column("absolute_path")]
        public string AbsolutePath { get; set; } = string.Empty;

        /// <summary>
        /// 文件大小（字节数）
        /// </summary>
        /// <example>204800</example>
        [Column("file_size")]
        public long FileSize { get; set; }

        /// <summary>
        /// 文件 MIME 类型（如 "application/pdf", "image/png"）
        /// </summary>
        /// <example>application/pdf</example>
        [Column("mime_type")]
        public string MimeType { get; set; } = string.Empty;

        /// <summary>
        /// 上传时间（UTC 时间）
        /// </summary>
        /// <example>2025-09-15T12:30:00Z</example>
        [Column("upload_time")]
        public DateTime UploadTime { get; set; }

        /// <summary>
        /// 上传者用户名
        /// </summary>
        /// <example>admin</example>
        [Column("uploader")]
        public string Uploader { get; set; } = string.Empty;

        /// <summary>
        /// 文件的 ETag（MinIO/S3 生成的哈希，用于文件完整性校验）
        /// </summary>
        /// <example>5d41402abc4b2a76b9719d911017c592</example>
        [Column("etag")]
        public string ETag { get; set; } = string.Empty;

        /// <summary>
        /// 文件关联的标签（多对多关系：FileRecord - FileTag - Tag）
        /// </summary>
        public ICollection<FileTag>? FileTags { get; set; }
    }

    /// <summary>
    /// 标签表实体（映射数据库表 tags）
    /// </summary>
    /// <remarks>
    /// 用于对文件进行分类或标记。
    /// 一个标签可以关联多个文件，一个文件也可以有多个标签。
    /// </remarks>
    [Table("tags")]
    public class Tag
    {
        /// <summary>
        /// 标签唯一 ID（主键，自增）
        /// </summary>
        /// <example>1</example>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// 标签名称（唯一，区分大小写与否可由数据库约束决定）
        /// </summary>
        /// <example>财务报表</example>
        [Required]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 标签与文件的关联（多对多关系）
        /// </summary>
        public ICollection<FileTag>? FileTags { get; set; }
    }

    /// <summary>
    /// 文件-标签关联表实体（多对多关系映射表 file_tags）
    /// </summary>
    /// <remarks>
    /// 代表一个文件与一个标签的对应关系。
    /// 主键通常是 (FileId, TagId) 复合键。
    /// </remarks>
    [Table("file_tags")]
    public class FileTag
    {
        /// <summary>
        /// 文件 ID（外键，指向 file_info 表）
        /// </summary>
        /// <example>101</example>
        [Column("file_id")]
        public int FileId { get; set; }

        /// <summary>
        /// 文件实体（导航属性）
        /// </summary>
        public FileRecord? FileRecord { get; set; }

        /// <summary>
        /// 标签 ID（外键，指向 tags 表）
        /// </summary>
        /// <example>5</example>
        [Column("tag_id")]
        public int TagId { get; set; }

        /// <summary>
        /// 标签实体（导航属性）
        /// </summary>
        public Tag? Tag { get; set; }
    }

    /// <summary>
    /// 创建标签 DTO（供前端调用时使用）
    /// </summary>
    /// <remarks>
    /// 用于新增标签的输入参数。
    /// </remarks>
    public class CreateTagDto
    {
        /// <summary>
        /// 标签名称
        /// </summary>
        /// <example>项目文档</example>
        [Required(ErrorMessage = "标签名不能为空")]
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// 文件操作日志表实体（记录用户对文件的操作）
    /// </summary>
    [Table("operation_logs")]
    public class OperationLog
    {
        /// <summary>
        /// 日志 ID（主键，自增）
        /// </summary>
        /// <example>1001</example>
        [Key]
        [Column("id")]
        public long Id { get; set; }

        /// <summary>
        /// 用户名（执行操作的用户）
        /// </summary>
        /// <example>admin</example>
        [Column("username")]
        [Required]
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 操作类型（Upload / Delete / Download / Edit）
        /// </summary>
        /// <example>Upload</example>
        [Column("operation_type")]
        [Required]
        public string OperationType { get; set; } = string.Empty;

        /// <summary>
        /// 文件名（涉及的文件名称）
        /// </summary>
        /// <example>report2024.xlsx</example>
        [Column("file_name")]
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 存储桶名称
        /// </summary>
        /// <example>documents</example>
        [Column("bucket")]
        public string Bucket { get; set; } = string.Empty;

        /// <summary>
        /// 操作时间（UTC 时间）
        /// </summary>
        /// <example>2025-09-15T08:30:00Z</example>
        [Column("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 操作状态（Success / Fail）
        /// </summary>
        /// <example>Success</example>
        [Column("status")]
        public string Status { get; set; } = "Success";

        /// <summary>
        /// 额外信息（失败原因、操作描述等）
        /// </summary>
        /// <example>文件已存在，跳过上传</example>
        [Column("message")]
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// 文件编辑请求 DTO
    /// </summary>
    public class EditFileDto
    {
        /// <summary>
        /// 文件 ID
        /// </summary>
        /// <example>2001</example>
        [Required]
        public int Id { get; set; }

        /// <summary>
        /// 新的文件名
        /// </summary>
        /// <example>new_report2024.xlsx</example>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 新的标签列表（覆盖原有标签）
        /// </summary>
        /// <example>["财务", "年度报告"]</example>
        public List<string>? Tags { get; set; }
    }

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


    /// <summary>
    /// 系统用户表
    /// </summary>
    [Table("users")]
    public class User
    {
        /// <summary>
        /// 用户唯一 ID（主键，自增）
        /// </summary>
        /// <example>1</example>
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// 登录账号（唯一）
        /// </summary>
        /// <example>admin</example>
        [Required]
        [Column("username")]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 加密后的密码（不要存明文）
        /// </summary>
        /// <example>$2a$11$A8G7...</example>
        [Required]
        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// 用户角色（Admin / User）
        /// </summary>
        /// <example>Admin</example>
        [Required]
        [Column("role")]
        [MaxLength(50)]
        public string Role { get; set; } = "User";

        /// <summary>
        /// 上次登录时间（可为空）
        /// </summary>
        /// <example>2025-09-14T12:30:00Z</example>
        [Column("last_login")]
        public DateTime? LastLogin { get; set; }
    }


}