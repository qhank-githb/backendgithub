using MinioWebBackend.Dtos.LogDtos;
using MinioWebBackend.Models;

namespace MinioWebBackend.Interfaces
{
    public interface IFileTagService
{
    Task AddTagsToFileAsync(int fileId, List<int> tagIds);
        Task<List<FileRecordESDto>> GetFilesByTagAsync(string tagName);
    Task<List<Tag>> GetTagsByFileAsync(int fileId);
        Task<List<FileWithTagsDto>> GetFilesByTagsAsync(List<string> tagNames, bool matchAll);
}

}
