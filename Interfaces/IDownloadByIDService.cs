using ConsoleApp1.Models;


namespace ConsoleApp1.Interfaces
{

    public interface IDownloadByIDService
    {
        Task<(Stream? Stream, string? Error, FileInfoModel? FileInfo)> DownloadByIdAsync(int id);
    }

}