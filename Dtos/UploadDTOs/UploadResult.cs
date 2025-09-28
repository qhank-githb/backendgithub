namespace MinioWebBackend.Dtos.UploadDTOs
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

}