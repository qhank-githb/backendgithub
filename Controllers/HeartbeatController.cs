using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class HeartbeatController : ControllerBase
{
    private readonly OnlineUserService _onlineUserService;

    public HeartbeatController(OnlineUserService onlineUserService)
    {
        _onlineUserService = onlineUserService;
    }

    // POST api/heartbeat/ping
    [HttpPost("ping")]
    [Authorize]
    public IActionResult Ping()
    {
        var username = User.Identity?.Name;
        if (string.IsNullOrEmpty(username))
            return Unauthorized();

        _onlineUserService.UpdateHeartbeat(username);
        return Ok(new { message = "pong" });
    }

    // GET api/heartbeat/online-users
    [HttpGet("online-users")]
    public IActionResult GetOnlineUsers()
    {
        var users = _onlineUserService.GetOnlineUsers();
        return Ok(users);
    }
}
