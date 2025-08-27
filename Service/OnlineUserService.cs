// public class OnlineUserService
// {
//     private readonly HashSet<string> _onlineUsers = new();
//     private readonly object _lock = new();

//     // 上线：返回 true 表示状态确实从 offline -> online
//     public bool SetOnline(string username)
//     {
//         lock (_lock)
//         {
//             if (_onlineUsers.Contains(username))
//             {
//                 return false; // 已经在线
//             }
//             _onlineUsers.Add(username);
//             return true; // 新上线
//         }
//     }

//     // 下线：返回 true 表示状态确实从 online -> offline
//     public bool SetOffline(string username)
//     {
//         lock (_lock)
//         {
//             if (_onlineUsers.Remove(username))
//             {
//                 return true; // 确实断开
//             }
//             return false; // 原本就不在线
//         }
//     }

//     public List<string> GetOnlineUsers()
//     {
//         lock (_lock)
//         {
//             return _onlineUsers.ToList();
//         }
//     }
// }
