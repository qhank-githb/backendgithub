using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MinioWebBackend.Interfaces;
using MinioWebBackend.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MinioWebBackend.Service
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _db;
        private readonly PasswordHasher<User> _hasher = new PasswordHasher<User>();

        private readonly string _jwtSecret = "MySuperSecretKeyForJWTToken_32BytesOrMore!";
        private readonly string _issuer = "my_app_issuer";
        private readonly string _audience = "my_app_audience";

        // 默认管理员账号信息
        private const string _adminUsername = "admin";
        private const string _defaultAdminPassword = "admin";
        private const string _adminRole = "Admin";

        public AuthService(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// 注册用户
        /// </summary>
        public async Task<User> RegisterAsync(string username, string password, string role = "User")
        {
            // 检查用户名是否已存在
            if (await _db.Users.AnyAsync(u => u.Username == username))
                throw new InvalidOperationException("用户名已存在");

            // 禁止普通用户使用"admin"作为用户名
            if (role == "User" && username.Equals(_adminUsername, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("该用户名不允许使用");

            var user = new User
            {
                Username = username,
                Role = role
            };

            user.PasswordHash = _hasher.HashPassword(user, password);

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return user;
        }

        /// <summary>
        /// 用户登录（验证密码 + 更新最后登录时间）
        /// </summary>
        public async Task<User?> LoginAsync(string username, string password)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return null;

            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result == PasswordVerificationResult.Failed)
                return null;

            // 更新最后登录时间
            user.LastLogin = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return user;
        }

        /// <summary>
        /// 生成 JWT
        /// </summary>
        public string GenerateJwtToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// 初始化管理员账号（仅在管理员账号不存在时创建）
        /// </summary>
        public async Task InitializeAdminAccountAsync()
        {
            // 检查管理员账号是否已存在
            var existingAdmin = await _db.Users
                .FirstOrDefaultAsync(u => u.Username == _adminUsername || u.Role == _adminRole);

            // 如果管理员账号已存在，则不创建
            if (existingAdmin != null)
                return;

            // 创建默认管理员账号
            var adminUser = new User
            {
                Username = _adminUsername,
                Role = _adminRole
            };

            adminUser.PasswordHash = _hasher.HashPassword(adminUser, _defaultAdminPassword);
            adminUser.LastLogin = null; // 首次创建，尚未登录

            _db.Users.Add(adminUser);
            await _db.SaveChangesAsync();
        }
    }
}
    