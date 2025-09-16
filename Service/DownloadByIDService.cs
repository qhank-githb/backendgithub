//这里就是DownloadService套了一层查询

using Amazon.S3;
using MinioWebBackend.Models;
using MinioWebBackend.Interfaces;


namespace MinioWebBackend.Service
{
    public class DownloadByIDService : IDownloadByIDService
{
    private readonly IQueryService _iQueryService;
    private readonly IDownloadService _downloadService;
    private readonly IHttpContextAccessor _httpContextAccessor;


        public DownloadByIDService(IAmazonS3 s3Client, IQueryService iQueryService, IDownloadService downloadService, IHttpContextAccessor httpContextAccessor)
        {
            _iQueryService = iQueryService ?? throw new ArgumentNullException(nameof(iQueryService));
            _downloadService = downloadService;
            _httpContextAccessor = httpContextAccessor;
        }

            public async Task<(Stream? FileStream, string? Error, FileInfoModel? FileInfo)> DownloadFileByIdAsync(int id)
            {
                var userName = _httpContextAccessor.HttpContext?.User.Claims
                    .FirstOrDefault(c => c.Type == "username")?.Value
                    ?? _httpContextAccessor.HttpContext?.User.Identity?.Name
                    ?? "未知用户";

                try
                {
                    var fileInfo = await _iQueryService.GetFileByIdAsync(id);
                    if (fileInfo == null)
                        return (null, $"未找到 ID={id} 的文件", null);

                    var stream = await _downloadService.DownloadObjectAsStreamAsync(fileInfo.Bucketname, fileInfo.StoredFileName);
                    if (stream == null)
                        return (null, $"在桶 {fileInfo.Bucketname} 中找不到文件 {fileInfo.StoredFileName}", fileInfo);

                    return (stream, null, fileInfo);
                }
                catch (AmazonS3Exception ex)
                {
                    return (null, $"MinIO 访问异常: {ex.Message}", null);
                }
                catch (Exception ex)
                {
                    return (null, $"未知错误: {ex.Message}", null);
                }
            }


}

}