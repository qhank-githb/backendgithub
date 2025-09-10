using MinioWebBackend.Models;


namespace MinioWebBackend.Interfaces
{

    public interface IDownloadByIDService
    {
        Task<(Stream? FileStream, string? Error, FileInfoModel? FileInfo)> DownloadFileByIdAsync(int id);
    }

}