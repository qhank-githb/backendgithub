using Microsoft.Extensions.Hosting;

public class OfflineChecker : BackgroundService
{
    private readonly OnlineUserService _onlineUserService;
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);

    public OfflineChecker(OnlineUserService onlineUserService)
    {
        _onlineUserService = onlineUserService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var offlineUsers = _onlineUserService.GetOfflineUsers(_timeout);
            foreach (var user in offlineUsers)
            {
                Console.WriteLine($"User {user} is offline");
                // 可触发事件或更新数据库状态
            }

            await Task.Delay(5000, stoppingToken);
        }
    }
}
