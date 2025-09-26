namespace MinioWebBackend.Dtos.AuthDTOs
{
    /// <summary>
/// 注册请求DTO（数据传输对象），封装用户注册时的输入参数
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// 注册用户名（必须唯一，不允许为"admin"（普通用户角色时））
    /// </summary>
    /// <example>newuser</example>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 注册密码（明文，后端会进行加密存储）
    /// </summary>
    /// <example>New@123456</example>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 用户角色（可选，默认为"User"）
    /// </summary>
    /// <remarks>
    /// 允许值："Admin"（管理员）、"User"（普通用户）<br/>
    /// 仅管理员可指定"Admin"角色
    /// </remarks>
    /// <example>User</example>
    public string? Role { get; set; }
}

}
