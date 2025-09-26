namespace MinioWebBackend.Dtos.AuthDTOs
{
    /// <summary>
    /// 登录请求DTO（数据传输对象），封装用户登录时的输入参数
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// 登录用户名（必须与注册时的用户名一致）
        /// </summary>
        /// <example>testuser</example>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 登录密码（明文，后端会与数据库中的加密密码比对）
        /// </summary>
        /// <example>Test@123456</example>
        public string Password { get; set; } = string.Empty;
    }
}
