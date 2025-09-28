using System.ComponentModel.DataAnnotations;

namespace MinioWebBackend.Dtos.EditFileDTOs
{
        /// <summary>
    /// 文件编辑请求 DTO
    /// </summary>
    public class EditFileDto
    {
        /// <summary>
        /// 文件 ID
        /// </summary>
        /// <example>15</example>
        [Required]
        public int Id { get; set; }

        /// <summary>
        /// 新的文件名
        /// </summary>
        /// <example>测试.docx</example>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 新的标签列表（覆盖原有标签）
        /// </summary>
        /// <example>["PPTX","PDF"]</example>
        public List<string>? Tags { get; set; }
    }

}
