using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;


namespace MinioWebBackend.Models
{

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
        [JsonIgnore]
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
    
    [Table("SerilogLogs")] // 表名风格与OperationLogs一致
    public class SerilogLog
    {
        /// <summary>
        /// 自增主键（与OperationLogs的Id类型一致）
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>
        /// 日志级别（Info/Warn/Error等，对应OperationLogs的OperationType风格）
        /// </summary>
        [Required]
        public string Level { get; set; } = string.Empty;// 对应longtext类型（允许长文本）

        /// <summary>
        /// 渲染后的日志消息（对应OperationLogs的Message风格）
        /// </summary>
        [Required]
        public string Message { get; set; } = string.Empty; // 对应longtext类型

        /// <summary>
        /// 异常信息（ nullable，对应OperationLogs的Message可空场景）
        /// </summary>
        public string? Exception { get; set; } // 对应longtext类型

        /// <summary>
        /// 结构化参数（JSON格式，存储{username}, {bucket}等键值对）
        /// </summary>
        public string? Properties { get; set; } // 对应longtext类型

        /// <summary>
        /// 日志时间戳（与OperationLogs的Timestamp类型完全一致：datetime(6)）
        /// </summary>
         [Column(TypeName = "datetime")] 
    public DateTime Timestamp { get; set; }
    }



}