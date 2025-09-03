using MinioWebBackend.Models;


namespace MinioWebBackend.Interfaces
{

    public interface IDownloadByIDService
    {
        Task<(Stream? Stream, string? Error, FileInfoModel? FileInfo)> DownloadByIdAsync(int id);
    }

}