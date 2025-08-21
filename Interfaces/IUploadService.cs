using ConsoleApp1.Models;

namespace ConsoleApp1.Interfaces
{

    public interface IUploadService
    {
        Task<UploadResult> MultipartUploadAsync(MultipartUploadRequest request);

    }

}