using ConsoleApp1.Models;

namespace ConsoleApp1.Interfaces
{
    public interface ITagService
    {
        Task<Tag> CreateTagAsync(string name);

        Task<List<Tag>> GetAllTagsAsync();

        Task EditFileAsync(EditFileDto dto);
    }

}