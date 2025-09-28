using MinioWebBackend.Models;
using MinioWebBackend.Dtos.FileInfoDTOs;

namespace MinioWebBackend.Interfaces
{

    public interface IQueryService
    {
        Task<string?> GetOriginalFileNameAsync(string storedFileName, string bucketName);

        Task<(List<FileInfoModel> Items, int TotalCount)> QueryFilesAsync(
                int? id = null,
                string? uploader = null,
                string? fileName = null,
                string? bucket = null,
                DateTime? start = null,
                DateTime? end = null,
                int pageNumber = 1,
                int pageSize = 10,
                List<string>? tags = null,         // 可选标签
                bool matchAllTags = false          // true=全部匹配, false=任意匹配
            );

        Task<List<int>> QueryFileIdsAsync(
            int? id = null,
            string? uploader = null,
            string? fileName = null,
            string? bucket = null,
            DateTime? start = null,
            DateTime? end = null);

        Task<List<FileInfoModel>> GetAllFilesAsync();

        Task<FileInfoModel?> GetFileByIdAsync(int id);
    }

}

