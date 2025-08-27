// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;


// [ApiController]
// [Route("api/[controller]")]
// public class HeartbeatController : ControllerBase
// {
//     private readonly OnlineUserService _onlineUserService;

//     public HeartbeatController(OnlineUserService onlineUserService)
//     {
//         _onlineUserService = onlineUserService;
//     }

//     [HttpPost("ping")]
//     [Authorize]
//     public IActionResult Ping()
//     {
//         var username = User.Identity?.Name;
//         if (string.IsNullOrEmpty(username))
//             return Unauthorized();

//         if (_onlineUserService.SetOnline(username))
//         {
//             Console.WriteLine($"User {username} connected");
//         }

//         return Ok(new { message = "pong" });
//     }
// }


