namespace MinioWebBackend.Dtos.AuthDTOs
{
    /// <summary>
    /// 登录返回数据
    /// </summary>
    public class LoginResponse
    {
        /// <summary>JWT 访问令牌</summary>
        /// <example>eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...</example>
        public string Token { get; set; } = string.Empty;

        /// <summary>用户名</summary>
        /// <example>testuser</example>
        public string Username { get; set; } = string.Empty;

        /// <summary>用户角色</summary>
        /// <example>User</example>
        public string Role { get; set; } = string.Empty;

        /// <summary>最后登录时间（UTC）</summary>
        /// <example>2025-09-15T15:30:00Z</example>
        public DateTime? LastLogin { get; set; }
    }

}
