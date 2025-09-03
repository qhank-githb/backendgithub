

namespace MinioWebBackend.Interfaces
{

    public interface IBucketService
    {
        Task<List<string>> ListBucketsAsync();

    }

}