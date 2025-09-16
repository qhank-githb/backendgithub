//获取所有bucket名
using Amazon.S3;
using MinioWebBackend.Interfaces;

namespace MinioWebBackend.Service
{
    public class BucketService : IBucketService
    {
        private readonly IAmazonS3 _s3Client;

            public BucketService(IAmazonS3 s3Client)
        {
            _s3Client = s3Client;
        }

        public async Task<List<string>> ListBucketsAsync()
        {
            var response = await _s3Client.ListBucketsAsync();
            var bucketNames = new List<string>();
            foreach (var bucket in response.Buckets)
            {
                bucketNames.Add(bucket.BucketName);
            }
            return bucketNames;
        }
        
        
    }
}