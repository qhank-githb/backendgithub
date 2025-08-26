public class OnlineUserService
{
    private readonly Dictionary<string, DateTime> _onlineUsers = new();
    private readonly object _lock = new();

    public void UpdateHeartbeat(string username)
    {
        lock (_lock)
        {
            _onlineUsers[username] = DateTime.UtcNow;
        }
    }

    public List<string> GetOnlineUsers()
    {
        lock (_lock)
        {
            return _onlineUsers.Keys.ToList();
        }
    }

    public List<string> GetOfflineUsers(TimeSpan timeout)
    {
        var now = DateTime.UtcNow;
        lock (_lock)
        {
            return _onlineUsers
                .Where(kv => now - kv.Value > timeout)
                .Select(kv => kv.Key)
                .ToList();
        }
    }
}
