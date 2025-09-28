namespace MinioWebBackend.Dtos.UploadDTOs
{
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


}