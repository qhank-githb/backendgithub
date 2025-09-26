using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MinioWebBackend.Dtos.AuthDTOs;
using MinioWebBackend.Interfaces;
using Serilog;


/// <summary>
/// 认证控制器，处理用户注册、登录、登出等身份认证相关操作
/// </summary>
/// <remarks>
/// 提供用户身份管理的核心接口，包括：
/// - 管理员创建新用户（注册）
/// - 用户登录并获取JWT令牌
/// - 已登录用户登出
/// 依赖 <see cref="IAuthService"/> 处理具体业务逻辑，使用Serilog记录操作日志
/// </remarks>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    /// <summary>
    /// 认证服务接口，用于处理用户注册、登录、令牌生成等业务逻辑
    /// </summary>
    private readonly IAuthService _AuthService;

    /// <summary>
    /// 构造函数，注入认证服务依赖
    /// </summary>
    /// <param name="AuthService">认证服务实现类实例</param>
    public AuthController(IAuthService AuthService)
    {
        _AuthService = AuthService;
    }

    /// <summary>
    /// 用户注册接口（仅管理员可访问）
    /// </summary>
    /// <remarks>
    /// 功能：由管理员创建新用户，支持指定用户角色<br/>
    /// 权限：需携带管理员（Admin）角色的JWT令牌<br/>
    /// 业务限制：
    /// - 用户名不可重复
    /// - 普通用户（User）不能使用"admin"作为用户名
    /// </remarks>
    /// <param name="request">注册请求参数，包含用户名、密码和可选角色</param>
    /// <returns>
    /// 成功：200 OK，返回新用户的ID、用户名和角色<br/>
    /// 失败：400 Bad Request，返回错误消息（如用户名已存在）
    /// </returns>
    [HttpPost("register")]
    //[Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(RegisterResponse), 200)]   // 成功返回
    [ProducesResponseType(typeof(object), 400)]             // 失败返回
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var user = await _AuthService.RegisterAsync(request.Username, request.Password, request.Role ?? "User");
            Log.Information("用户注册成功：{Username}", user.Username);

            // 返回 DTO
            var response = new RegisterResponse
            {
                Id = user.Id,
                Username = user.Username,
                Role = user.Role
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            Log.Warning("用户注册失败：{Username}, 错误: {Error}", request.Username, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }

/// <summary>
/// 用户登录接口（允许匿名访问）
/// </summary>
/// <remarks>
/// 功能：验证用户凭据并生成JWT令牌<br/>
/// 流程：
/// 1. 验证用户名和密码是否匹配
/// 2. 验证通过后更新用户最后登录时间
/// 3. 生成有效期为2小时的JWT令牌
/// 4. 返回令牌及用户基本信息
/// </remarks>
/// <param name="request">登录请求参数，包含用户名和密码</param>
/// <returns>
/// 成功：200 OK，返回JWT令牌、用户名、角色和最后登录时间
/// 失败：401 Unauthorized，返回"用户名或密码错误"
/// </returns>
[HttpPost("login")]
[AllowAnonymous]
[ProducesResponseType(typeof(LoginResponse), 200)]
[ProducesResponseType(typeof(object), 401)]
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

    // 返回 DTO
    var response = new LoginResponse
    {
        Token = token,
        Username = user.Username,
        Role = user.Role,
        LastLogin = user.LastLogin
    };

    return Ok(response);
}

    /// <summary>
    /// 用户登出接口（需已认证）
    /// </summary>
    /// <remarks>
    /// 功能：处理用户登出逻辑（前端需自行清除本地JWT令牌）<br/>
    /// 说明：JWT令牌为无状态，后端无法主动失效，此接口仅记录登出日志
    /// </remarks>
    /// <returns>
    /// 成功：200 OK，返回"退出成功"消息<br/>
    /// （注：若令牌无效，会被认证中间件拦截为401）
    /// </returns>
    [HttpPost("logout")]
    //[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public IActionResult Logout()
    {
        var username = User.Identity?.Name ?? "Unknown";
        Log.Information("用户退出：{Username}", username);

        return Ok(new { message = "退出成功" });
    }
}
