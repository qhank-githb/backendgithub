using Amazon.S3;
using MinioWebBackend.Models;
using MinioWebBackend.Interfaces;


namespace MinioWebBackend.Service
{
    public class DownloadByIDService : IDownloadByIDService
{
    private readonly IQueryService _iQueryService;
    private readonly IDownloadService _downloadService;

    public DownloadByIDService(IAmazonS3 s3Client, IQueryService iQueryService, IDownloadService downloadService)
    {
        _iQueryService = iQueryService ?? throw new ArgumentNullException(nameof(iQueryService));
        _downloadService = downloadService;
    }

    public async Task<(Stream? Stream, string? Error, FileInfoModel? FileInfo)> DownloadByIdAsync(int id)
    {
        var fileInfo = await _iQueryService.GetFileByIdAsync(id);
        if (fileInfo == null)
        {
            return (null, $"文件 ID={id} 不存在", null);
        }

        var (stream, error) = await _downloadService.DownloadFileAsync(fileInfo.Bucketname, fileInfo.OriginalFileName);

        if (stream == null)
        {
            return (null, error ?? "文件在存储中不存在", fileInfo);
        }

        return (stream, null, fileInfo);
    }
}

}