using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MinioWebBackend.Interfaces;
using Serilog;
using Swashbuckle.AspNetCore.Annotations;

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
/// 用户注册（仅管理员可操作）
/// </summary>
/// <remarks>
/// 功能：
/// - 管理员创建新用户账号
/// - 密码会被加密存储
/// - 注册成功后用户状态为“启用”
/// 
/// 示例请求 JSON：
/// ```json
/// {
///   "username": "test_user_001",
///   "password": "Test@123456",
///   "role": "User"
/// }
/// ```
/// </remarks>
/// <param name="request">注册请求参数（JSON 格式，从请求体获取）</param>
/// <returns>
/// 成功响应（200 OK）：
/// ```json
/// {
///   "id": 1001,
///   "username": "test_user_001",
///   "role": "User",
///   "message": "注册成功",
///   "createdAt": "2023-10-01T10:30:00"
/// }
/// ```
/// 失败响应（400 Bad Request）：
/// ```json
/// {
///   "error": "用户名已存在"
/// }
/// ```
/// 无权限响应（403 Forbidden）：
/// ```json
/// {
///   "error": "仅管理员可执行此操作"
/// }
/// ```
/// </returns>
/// <response code="200">注册成功，返回用户基础信息</response>
/// <response code="400">参数无效或业务校验失败（如用户名重复）</response>
/// <response code="401">未登录（无有效 Token）</response>
/// <response code="403">已登录但非管理员角色</response>
[HttpPost("register")]
[Authorize(Roles = "Admin")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
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
    /// <remarks>
    /// 功能说明：验证用户凭据并生成访问令牌
    /// 
    /// 示例请求：
    /// ```json
    /// {
    ///   "username": "testUser",
    ///   "password": "Test@123456"
    /// }
    /// ```
    /// </remarks>
    /// <param name="request">登录请求参数对象，来源于请求体（FromBody）</param>
    /// <param name="request.Username">用户名，字符串类型，必填项
    /// <list type="bullet">
    /// <item>与注册时填写的用户名一致</item>
    /// <item>示例值："testUser123"</item>
    /// </list>
    /// </param>
    /// <param name="request.Password">用户密码，字符串类型，必填项
    /// <list type="bullet">
    /// <item>与注册时设置的密码一致</item>
    /// <item>示例值："Test@123456"</item>
    /// </list>
    /// </param>
    /// <returns>
    /// 成功响应（200 OK）：
    /// ```json
    /// {
    ///   "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    ///   "username": "testUser",
    ///   "role": "User",
    ///   "lastLoginTime": "2023-10-01T14:30:00"
    /// }
    /// ```
    /// 
    /// 失败响应（401 Unauthorized）：
    /// ```json
    /// {
    ///   "error": "用户名或密码错误"
    /// }
    /// ```
    /// </returns>
    /// <response code="200">登录成功，返回JWT令牌、用户名、角色和最后登录时间</response>
    /// <response code="400">请求参数不完整</response>
    /// <response code="401">用户名或密码错误</response>
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
    /// 用户登出（清除登录状态）
    /// </summary>
    /// <remarks>
    /// 功能说明：使当前用户的令牌失效，清除登录状态
    /// 
    /// 调用说明：
    /// - 需要在请求头中携带有效的Authorization令牌
    /// - 示例请求头：Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
    /// </remarks>
    /// <returns>
    /// 成功响应（200 OK）：
    /// ```json
    /// {
    ///   "message": "退出成功"
    /// }
    /// ```
    /// 
    /// 失败响应（401 Unauthorized）：
    /// ```json
    /// {
    ///   "error": "未授权访问"
    /// }
    /// ```
    /// </returns>
    /// <response code="200">登出成功</response>
    /// <response code="401">未登录状态下调用此接口</response>
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

/// <summary>
/// 用户注册请求参数模型
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// 用户名
    /// </summary>
    /// <remarks>
    /// 约束：
    /// - 长度 3-20 字符
    /// - 仅允许字母、数字、下划线（_）
    /// - 全局唯一，不可重复
    /// </remarks>
    /// <example>test_user_001</example>
    [Required(ErrorMessage = "用户名不能为空")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// 密码
    /// </summary>
    /// <remarks>
    /// 安全约束：
    /// - 长度 8-20 字符
    /// - 必须包含：大写字母、小写字母、数字、特殊符号（!@#$%^&* 等）
    /// </remarks>
    /// <example>Test@123456</example>
    [Required(ErrorMessage = "密码不能为空")]
    public string Password { get; set; } = string.Empty;
        
    /// <summary>
    /// 用户角色
    /// </summary>
    /// <remarks>
    /// 可选值：
    /// - "Admin"：管理员（所有权限）
    /// - "User"：普通用户（默认值，基础权限）
    /// 注意：非管理员调用注册接口时，此参数会被强制设为 "User"
    /// </remarks>
    /// <example>User</example>
    [RegularExpression(@"^(Admin|User)$", ErrorMessage = "角色只能是 Admin 或 User")]
    public string? Role { get; set; }
}
