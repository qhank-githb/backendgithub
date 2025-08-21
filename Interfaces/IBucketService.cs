

namespace ConsoleApp1.Interfaces
{

    public interface IBucketService
    {
        Task<List<string>> ListBucketsAsync();

    }

}