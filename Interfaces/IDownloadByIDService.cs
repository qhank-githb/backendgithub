using MinioWebBackend.Models;
using MinioWebBackend.Dtos.FileInfoDTOs;


namespace MinioWebBackend.Interfaces
{

    public interface IDownloadByIDService
    {
        Task<(Stream? FileStream, string? Error, FileInfoModel? FileInfo)> DownloadFileByIdAsync(int id);
    }

}