using MinioWebBackend.Models;

namespace MinioWebBackend.Interfaces
{
    public interface IFileTagService
{
    Task AddTagsToFileAsync(int fileId, List<int> tagIds);
    Task<List<FileRecord>> GetFilesByTagAsync(string tagName);
    Task<List<Tag>> GetTagsByFileAsync(int fileId);
        Task<List<FileWithTagsDto>> GetFilesByTagsAsync(List<string> tagNames, bool matchAll);
}

}
