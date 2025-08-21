using Amazon.S3;
using ConsoleApp1.Interfaces;

namespace ConsoleApp1.Service
{
    public class BucketService : IBucketService
    {
        private readonly IAmazonS3 _s3Client;

            public BucketService(IAmazonS3 s3Client)
        {
            _s3Client = s3Client;
        }

        /// 查询所有桶名
        /// </summary>
        /// <returns>桶名字符串列表</returns>
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