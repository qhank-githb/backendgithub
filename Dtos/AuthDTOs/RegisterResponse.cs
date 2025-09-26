namespace MinioWebBackend.Dtos.AuthDTOs
{
    /// <summary>
    /// 用户注册返回对象
    /// </summary>
    public class RegisterResponse
    {
        /// <summary>用户ID</summary>
        public int Id { get; set; }

        /// <summary>用户名</summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>角色</summary>
        public string Role { get; set; } = string.Empty;
    }

}
