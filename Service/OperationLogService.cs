using ConsoleApp1.Interfaces;
using ConsoleApp1.Models;

public class OperationLogService : IOperationLogService
{
    private readonly AppDbContext _db;

    public OperationLogService(AppDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(OperationLog log)
    {
        _db.OperationLogs.Add(log);
        await _db.SaveChangesAsync();
    }
}