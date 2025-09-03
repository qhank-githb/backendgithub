using MinioWebBackend.Models;

namespace MinioWebBackend.Interfaces
{

    public interface IUploadService
    {
        Task<UploadResult> MultipartUploadAsync(MultipartUploadRequest request);

    }

}