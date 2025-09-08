using Amazon.S3;
using Amazon.S3.Model;
using MinioWebBackend.Interfaces;
using System.IO.Compression;
using Microsoft.Extensions.Options;
using MinioWebBackend.Options;
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

    

    //通过“原始文件名”和“桶名”在数据库中查找对应的“存储文件名”，然后下载文件
public async Task<(Stream? FileStream, string? Error)> DownloadFileAsync(string bucket, string originalFileName)
{
    // 提前获取用户名，避免 try/catch 里作用域问题
    var userName = _httpContextAccessor.HttpContext?.User.Claims
        .FirstOrDefault(c => c.Type == "username")?.Value
        ?? _httpContextAccessor.HttpContext?.User.Identity?.Name
        ?? "未知用户";

    string? key = null;

    try
    {
        key = await _iQueryService.GetStoredFileNameAsync(originalFileName, bucket);
        if (string.IsNullOrEmpty(key))
            return (null, $"在桶 {bucket} 中找不到原始文件名为 {originalFileName} 的文件");

        var stream = await DownloadObjectAsStreamAsync(bucket, key);
        if (stream == null)
            return (null, $"在桶 {bucket} 中找不到文件 {key}");


        return (stream, null);
    }
    catch (AmazonS3Exception ex)
    {
        return (null, $"MinIO访问异常: {ex.Message}");
    }
    catch (Exception ex)
    {

        return (null, $"未知错误: {ex.Message}");
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

                            // 文件名加桶名前缀避免冲突
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
