using MinioWebBackend.Dtos.EditFileDTOs;
using MinioWebBackend.Dtos.LogDtos;
using MinioWebBackend.Models;

namespace MinioWebBackend.Interfaces
{
    public interface ITagService
    {
        Task<Tag> CreateTagAsync(string name);

        Task<List<TagDto>> GetAllTagsAsync();

        Task EditFileAsync(EditFileDto dto);
    }

}