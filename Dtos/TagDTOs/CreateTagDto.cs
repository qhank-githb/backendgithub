using System.ComponentModel.DataAnnotations;

namespace MinioWebBackend.Dtos.TagDTOs
{
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

}