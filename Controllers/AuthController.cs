using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // 1. 校验用户名密码（示例，实际可查数据库）
        if (request.Username != "bolo-vue-test" || request.Password != "123456")
            return Unauthorized(new { message = "用户名或密码错误" });

        // 2. 生成 JWT
        var token = GenerateJwtToken(request.Username);

        return Ok(new { token });
    }

    private string GenerateJwtToken(string username)
    {
        // ⚠️ 密钥长度必须 >= 32 字节
        var secretKey = "MySuperSecretKeyForJWTToken_32BytesOrMore!";
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "my_app_issuer",   // 与 Program.cs 中一致
            audience: "my_app_audience", // 与 Program.cs 中一致
            expires: DateTime.Now.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
