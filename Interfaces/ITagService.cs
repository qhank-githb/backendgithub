using ConsoleApp1.Models;
using ConsoleApp1.Interfaces;

namespace ConsoleApp1.Interfaces
{
    public interface ITagService
    {
        Task<Tag> CreateTagAsync(string name);

        Task<List<Tag>> GetAllTagsAsync();

        Task EditFileAsync(EditFileDto dto);
    }

}