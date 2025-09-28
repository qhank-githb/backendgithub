using Amazon.S3;
using Amazon.S3.Model;
using MinioWebBackend.Interfaces;
using System.IO.Compression;
using Microsoft.Extensions.Options;
using MinioWebBackend.Options;
using MinioWebBackend.Dtos.FileInfoDTOs;
using Serilog;



namespace MinioWebBackend.Service
{
    public class DownloadService : IDownloadService
{
    private readonly IQueryService _iQueryService;
    private readonly IAmazonS3 _s3Client;
    private readonly IHttpContextAccessor _httpContextAccessor;

        public DownloadService(
                IOptions<MinioOptions> options,
                IQueryService iQueryService,
                IAmazonS3 s3Client,
                IHttpContextAccessor httpContextAccessor)
        {
            var minioOptions = options.Value ?? throw new ArgumentNullException(nameof(options));//为什么要校验null

            _s3Client = s3Client;
            _iQueryService = iQueryService ?? throw new ArgumentNullException(nameof(IQueryService));
            _httpContextAccessor = httpContextAccessor;
        }


    
    
 
        //StoredFileName 下载 文件，查找原名只是为了显示
        public async Task<Stream> DownloadObjectAsStreamAsync(string bucketName, string objectName)
        {
            // 先查数据库里的原始文件名
            var originalFileName = await _iQueryService.GetOriginalFileNameAsync(objectName, bucketName);

            try
            {
                var request = new GetObjectRequest
                {
                    BucketName = bucketName,
                    Key = objectName
                };

                var response = await _s3Client.GetObjectAsync(request);

                // 打印控制台日志
                Console.WriteLine($"[DownloadObject] Bucket: {bucketName}, StoredName: {objectName}, OriginalName: {originalFileName}, Length: {response.ContentLength}");

            var username = _httpContextAccessor.HttpContext?.User?.Identity?.Name 
               ?? _httpContextAccessor.HttpContext?.User?.FindFirst("username")?.Value
               ?? "匿名用户";

                // Serilog 日志
                Log.Information("用户 {username} 下载文件 成功。桶: {bucket}, 原始名: {original}, 存储名: {stored}, 大小: {size} 字节",
                    _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "匿名用户",
                    bucketName,
                    originalFileName ?? "(未知)",
                    objectName,
                    response.ContentLength);

                // 推荐复制一份内容进内存中再返回
                var memoryStream = new MemoryStream();
                await response.ResponseStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                return memoryStream;
            }
            catch (AmazonS3Exception ex)
            {
                Log.Error(ex, "下载文件失败。桶: {bucket}, 存储名: {stored}, 原始名: {original}", 
                    bucketName, objectName, originalFileName ?? "(未知)");
                throw;
            }
        }




   
    //根据数据库中存储的多个文件 ID，批量下载对应文件，压缩为 Zip，返回 Stream
    public async Task<(Stream ZipStream, string? Error)> BatchDownloadByIdsAsync(List<int> ids)
        {
            if (ids == null || !ids.Any())
                return (Stream.Null, "未提供任何文件 ID");

            var zipStream = new MemoryStream();

            try
            {
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
                {
                    //用来记录每个文件名出现的次数
                    //避免不同文件同名时，压缩包里相互覆盖
                    var nameCount = new Dictionary<string, int>();

                    foreach (var id in ids)
                    {
                        var fileInfo = await _iQueryService.GetFileByIdAsync(id);
                        if (fileInfo == null)
                        {
                            Console.WriteLine($"[BatchDownloadService] ID={id} 文件未找到，跳过");
                            continue;
                        }

                        try
                        {
                            var stream = await DownloadObjectAsStreamAsync(fileInfo.Bucketname, fileInfo.StoredFileName);
                            if (stream == null)
                            {
                                Console.WriteLine($"[BatchDownloadService] 文件 {fileInfo.StoredFileName} 下载失败，跳过");
                                continue;
                            }

                            // 避免压缩包里多个文件重名
                            var entryName = fileInfo.OriginalFileName;

                            if (nameCount.ContainsKey(entryName))
                            {
                                nameCount[entryName]++;
                                var nameWithoutExt = Path.GetFileNameWithoutExtension(entryName);
                                var ext = Path.GetExtension(entryName);
                                entryName = $"{nameWithoutExt}_{nameCount[entryName]}{ext}";
                            }
                            else
                            {
                                nameCount[entryName] = 1;
                            }

                            var zipEntry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
                            using var entryStream = zipEntry.Open();
                            stream.Position = 0;
                            await stream.CopyToAsync(entryStream);
                            await stream.DisposeAsync();
                        }
                        catch (AmazonS3Exception ex)
                        {
                            Console.WriteLine($"[BatchDownloadService] 下载文件 ID={id} 出错：{ex.Message}");
                        }
                    }
                }

                zipStream.Position = 0;
                return (zipStream, null);
            }
            catch (Exception ex)
            {
                return (Stream.Null, $"批量下载失败: {ex.Message}");
            }
        }



}



}
