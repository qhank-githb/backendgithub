using ConsoleApp1.Models;

namespace ConsoleApp1.Interfaces
{
    public interface IFileTagService
{
    Task AddTagsToFileAsync(int fileId, List<int> tagIds);
    Task<List<FileRecord>> GetFilesByTagAsync(string tagName);
    Task<List<Tag>> GetTagsByFileAsync(int fileId);
    Task<List<FileRecord>> GetFilesByTagsAsync(List<string> tagNames, bool matchAll);
}

}
