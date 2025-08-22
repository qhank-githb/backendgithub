using ConsoleApp1.Models;
namespace ConsoleApp1.Interfaces
{
        public interface IOperationLogService
{
    Task LogAsync(OperationLog log);
}

}


