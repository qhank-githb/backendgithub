namespace MinioWebBackend.Interfaces
{
       public interface IDownloadService
{
        Task<Stream> DownloadObjectAsStreamAsync(string bucketName, string objectName);


        Task<(Stream ZipStream, string? Error)> BatchDownloadByIdsAsync(List<int> ids);

} 
}


