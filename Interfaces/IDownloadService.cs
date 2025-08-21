public interface IDownloadService
{
        Task<Stream> DownloadObjectAsStreamAsync(string bucketName, string objectName);

        Task<(Stream? FileStream, string? Error)> DownloadFileAsync(string bucket, string originalFileName);

        Task<(Stream ZipStream, string? Error)> BatchDownloadByIdsAsync(List<int> ids);

}
