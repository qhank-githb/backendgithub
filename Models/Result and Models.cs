using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsoleApp1.Models
{

        public class UploadResult
        {
            public string Originalfilename { get; set; } = string.Empty;
            public string ETag { get; set; } = string.Empty;
            public long Size { get; set; }
            public string Bucketname { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public List<string> Tags { get; set; } = new List<string>();
            public DateTime Uploadtime { get; set; }
        }



public class FileInfoModel
{
    public int Id { get; set; }
    public string StoredFileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string Bucketname { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string AbsolutePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public DateTime UploadTime { get; set; }
    public string Uploader { get; set; } = string.Empty;
    public string ETag { get; set; } = string.Empty;

    // 新增标签字段
    public List<string> Tags { get; set; } = new List<string>();
}



    public class MultipartUploadRequest
    {
        public string bucket { get; set; } = string.Empty;
        public string originalFileName { get; set; } = string.Empty;
        public string storedFileName { get; set; } = string.Empty;
        public string filePath { get; set; } = string.Empty;
        public string contentType { get; set; } = string.Empty;
        public string username { get; set; } = string.Empty;

        public List<string> Tags { get; set; } = new List<string>(); // 标签名
    }

    public class FileQueryResult
    {
        public List<FileInfoModel> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }

    [Table("file_info")]
    public class FileRecord
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("stored_file_name")]
        public string StoredFileName { get; set; }

        [Column("original_file_name")]
        public string OriginalFileName { get; set; }

        [Column("bucketname")]
        public string BucketName { get; set; }

        [Column("relative_path")]
        public string RelativePath { get; set; }

        [Column("absolute_path")]
        public string AbsolutePath { get; set; }

        [Column("file_size")]
        public long FileSize { get; set; }

        [Column("mime_type")]
        public string MimeType { get; set; }

        [Column("upload_time")]
        public DateTime UploadTime { get; set; }

        [Column("uploader")]
        public string Uploader { get; set; }

        public ICollection<FileTag> FileTags { get; set; }
    }

    [Table("tags")]
    public class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ICollection<FileTag> FileTags { get; set; }
    }

    [Table("file_tags")]
    public class FileTag
    {
        public int FileId { get; set; }
        public FileRecord FileRecord { get; set; }

        public int TagId { get; set; }
        public Tag Tag { get; set; }
    }


    public class CreateTagDto
    {
        public string Name { get; set; }
    }


public class OperationLog
{
    public long Id { get; set; }
    public string UserName { get; set; } = string.Empty;   // 只保留用户名
    public string OperationType { get; set; } = string.Empty; // Upload/Delete/Download
    public string FileName { get; set; } = string.Empty;
    public string Bucket { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Success"; // Success/Fail
    public string Message { get; set; } = string.Empty;
}

// 前端传过来的数据格式
public class EditFileDto
{
    public int Id { get; set; }              // 文件ID
    public string FileName { get; set; }     // 新的文件名
    public List<string> Tags { get; set; }   // 新的标签列表
}





}
