using MinioWebBackend.Models;
using System.Threading.Tasks;

namespace MinioWebBackend.Interfaces
{
    public interface IAuthService
    {
        /// <summary>
        /// 注册用户
        /// </summary>
        Task<User> RegisterAsync(string username, string password, string role = "User");
        
        /// <summary>
        /// 用户登录
        /// </summary>
        Task<User?> LoginAsync(string username, string password);
        
        /// <summary>
        /// 生成JWT令牌
        /// </summary>
        string GenerateJwtToken(User user);
        
        /// <summary>
        /// 初始化管理员账号
        /// </summary>
        Task InitializeAdminAccountAsync();
    }
}
    