using MinioWebBackend.Dtos.FileInfoDTOs;
using MinioWebBackend.Models;

namespace MinioWebBackend.Dtos.QuertDTOs
{
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
}