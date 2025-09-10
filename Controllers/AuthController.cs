using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MinioWebBackend.Interfaces;
using Serilog;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _AuthService;

    public AuthController(IAuthService AuthService)
    {
        _AuthService = AuthService;
    }

    /// <summary>
    /// 用户注册
    /// </summary>
    [HttpPost("register")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var user = await _AuthService.RegisterAsync(request.Username, request.Password, request.Role ?? "User");
            Log.Information("用户注册成功：{Username}", user.Username);

            return Ok(new { user.Id, user.Username, user.Role });
        }
        catch (Exception ex)
        {
            Log.Warning("用户注册失败：{Username}, 错误: {Error}", request.Username, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// 用户登录
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _AuthService.LoginAsync(request.Username, request.Password);
        if (user == null)
        {
            Log.Warning("用户登录失败：{Username}", request.Username);
            return Unauthorized(new { message = "用户名或密码错误" });
        }

        var token = _AuthService.GenerateJwtToken(user);

        Log.Information("用户登录成功：{Username}", user.Username);

        return Ok(new { token, user.Username, user.Role, user.LastLogin });
    }

    /// <summary>
    /// 用户登出
    /// </summary>
    [HttpPost("logout")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public IActionResult Logout()
    {
        var username = User.Identity?.Name ?? "Unknown";
        Log.Information("用户退出：{Username}", username);

        return Ok(new { message = "退出成功" });
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Role { get; set; }
}
