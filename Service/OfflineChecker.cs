using Microsoft.Extensions.Hosting;

public class OfflineChecker : BackgroundService
{
    private readonly OnlineUserService _onlineUserService;
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);

    // 用于记录每个用户上次心跳
    private readonly Dictionary<string, DateTime> _lastSeen = new();

    public OfflineChecker(OnlineUserService onlineUserService)
    {
        _onlineUserService = onlineUserService;
    }

    public void UpdateHeartbeat(string username)
    {
        _lastSeen[username] = DateTime.UtcNow;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            foreach (var kv in _lastSeen.ToList())
            {
                if (now - kv.Value > _timeout)
                {
                    if (_onlineUserService.SetOffline(kv.Key))
                    {
                        Console.WriteLine($"User {kv.Key} disconnected");
                    }
                    _lastSeen.Remove(kv.Key);
                }
            }
            await Task.Delay(5000, stoppingToken);
        }
    }
}

