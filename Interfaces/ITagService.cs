using MinioWebBackend.Models;

namespace MinioWebBackend.Interfaces
{
    public interface ITagService
    {
        Task<Tag> CreateTagAsync(string name);

        Task<List<Tag>> GetAllTagsAsync();

        Task EditFileAsync(EditFileDto dto);
    }

}