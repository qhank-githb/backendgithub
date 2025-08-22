using ConsoleApp1.Models;
using ConsoleApp1.Interfaces;
using MySqlConnector;
using Microsoft.Extensions.Options;
using ConsoleApp1.Options;

namespace ConsoleApp1.Service
{
    public class QueryService : IQueryService
    {
        private readonly string _connectionString;

        public QueryService(IOptions<MinioOptions> options)
        {
            var minioOptions = options.Value ?? throw new ArgumentNullException(nameof(options));
            _connectionString = minioOptions.DbConnectionString;
        }

        public async Task<string?> GetStoredFileNameAsync(string originalFileName, string bucketName)
        {
            await using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT stored_file_name FROM file_info WHERE original_file_name = @originalFileName AND bucketname = @bucketname";

            using var command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@originalFileName", originalFileName);
            command.Parameters.AddWithValue("@bucketname", bucketName);

            var result = await command.ExecuteScalarAsync();
            return result?.ToString();
        }

        public async Task<List<FileInfoModel>> GetAllFilesAsync()
        {
            var files = new List<FileInfoModel>();
            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = new MySqlCommand("SELECT * FROM file_info ORDER BY upload_time DESC", conn);
            var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                files.Add(MapReaderToFileInfo(reader));
            }

            return files;
        }

        public async Task<(List<FileInfoModel> Items, int TotalCount)> QueryFilesAsync(
    int? id = null,
    string? uploader = null,
    string? fileName = null,
    string? bucket = null,
    DateTime? start = null,
    DateTime? end = null,
    int pageNumber = 1,
    int pageSize = 10,
    List<string>? tags = null,
    bool matchAllTags = false
)
{
    var files = new List<FileInfoModel>();
    int totalCount = 0;

    pageNumber = Math.Max(1, pageNumber);
    pageSize = Math.Clamp(pageSize, 10, 1000);
    int offset = (pageNumber - 1) * pageSize;

    await using var conn = new MySqlConnection(_connectionString);
    await conn.OpenAsync();

    var cmd = conn.CreateCommand();
    var conditions = new List<string>();
    string tagJoin = "";
    string tagCondition = "";

    // 基础条件
    if (id.HasValue) { conditions.Add("f.id = @id"); cmd.Parameters.AddWithValue("@id", id.Value); }
    if (!string.IsNullOrWhiteSpace(uploader)) { conditions.Add("f.uploader LIKE @uploader"); cmd.Parameters.AddWithValue("@uploader", $"%{uploader}%"); }
    if (!string.IsNullOrWhiteSpace(fileName)) { conditions.Add("f.original_file_name LIKE @fileName"); cmd.Parameters.AddWithValue("@fileName", $"%{fileName}%"); }
    if (!string.IsNullOrWhiteSpace(bucket)) { conditions.Add("f.bucketname LIKE @bucket"); cmd.Parameters.AddWithValue("@bucket", $"%{bucket}%"); }
    if (start.HasValue) { conditions.Add("f.upload_time >= @start"); cmd.Parameters.AddWithValue("@start", start.Value); }
    if (end.HasValue) { conditions.Add("f.upload_time <= @end"); cmd.Parameters.AddWithValue("@end", end.Value); }

    // 标签筛选
    if (tags != null && tags.Count > 0)
    {
        if (matchAllTags)
        {
            tagCondition = $@"
                f.id IN (
                    SELECT ft.FileId
                    FROM file_tags ft
                    JOIN tags t ON ft.TagId = t.Id
                    WHERE t.Name IN ({string.Join(",", tags.Select((t,i)=> $"@tag{i}"))})
                    GROUP BY ft.FileId
                    HAVING COUNT(DISTINCT t.Name) = {tags.Count}
                )
            ";
            conditions.Add(tagCondition);
        }
        else
        {
            tagJoin = "JOIN file_tags ft ON f.id = ft.FileId JOIN tags t ON ft.TagId = t.Id";
            tagCondition = $"t.Name IN ({string.Join(",", tags.Select((t,i)=> $"@tag{i}"))})";
            conditions.Add(tagCondition);
        }

        for (int i = 0; i < tags.Count; i++)
            cmd.Parameters.AddWithValue($"@tag{i}", tags[i]);
    }

    string whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

    // 查询总数
    var countCmd = conn.CreateCommand();
    countCmd.CommandText = $"SELECT COUNT(DISTINCT f.id) FROM file_info f {tagJoin} {whereClause}";
    foreach (MySqlParameter p in cmd.Parameters)
        countCmd.Parameters.AddWithValue(p.ParameterName, p.Value);

    totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

    // 查询分页数据
    cmd.CommandText = $@"
        SELECT DISTINCT f.*
        FROM file_info f
        {tagJoin}
        {whereClause}
        ORDER BY f.upload_time DESC
        LIMIT @pageSize OFFSET @offset
    ";
    cmd.Parameters.AddWithValue("@pageSize", pageSize);
    cmd.Parameters.AddWithValue("@offset", offset);

    var reader = await cmd.ExecuteReaderAsync();
    var fileList = new List<FileInfoModel>();
    while (await reader.ReadAsync())
    {
        fileList.Add(MapReaderToFileInfo(reader));
    }
    await reader.CloseAsync();

    // 为每个文件查询标签
    if (fileList.Count > 0)
    {
        var fileIds = fileList.Select(f => f.Id).ToList();
        var tagCmd = conn.CreateCommand();
        tagCmd.CommandText = $@"
            SELECT ft.FileId, t.Name
            FROM file_tags ft
            JOIN tags t ON ft.TagId = t.Id
            WHERE ft.FileId IN ({string.Join(",", fileIds)})
        ";
        var tagReader = await tagCmd.ExecuteReaderAsync();
        var tagDict = new Dictionary<int, List<string>>();
        while (await tagReader.ReadAsync())
        {
            int fileId = tagReader.GetInt32("FileId");
            string tagName = tagReader.GetString("Name");
            if (!tagDict.ContainsKey(fileId)) tagDict[fileId] = new List<string>();
            tagDict[fileId].Add(tagName);
        }
        await tagReader.CloseAsync();

        // 给文件模型赋值标签
        foreach (var file in fileList)
        {
            file.Tags = tagDict.ContainsKey(file.Id) ? tagDict[file.Id] : new List<string>();
        }
    }

    return (fileList, totalCount);
}


        private FileInfoModel MapReaderToFileInfo(MySqlDataReader reader)
        {
            return new FileInfoModel
            {
                Id = reader.GetInt32("id"),
                StoredFileName = reader.GetString("stored_file_name"),
                OriginalFileName = reader.GetString("original_file_name"),
                Bucketname = reader.GetString("bucketname"),
                RelativePath = reader.GetString("relative_path"),
                AbsolutePath = reader.GetString("absolute_path"),
                FileSize = reader.GetInt64("file_size"),
                MimeType = reader.GetString("mime_type"),
                UploadTime = reader.GetDateTime("upload_time"),
                Uploader = reader.GetString("uploader"),
                ETag = reader.GetString("etag")
            };
        }

        public async Task<List<int>> QueryFileIdsAsync(
            int? id = null,
            string? uploader = null,
            string? fileName = null,
            string? bucket = null,
            DateTime? start = null,
            DateTime? end = null
        )
        {
            var ids = new List<int>();
            await using var conn = new MySqlConnection(_connectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            var conditions = new List<string>();

            if (id.HasValue) { conditions.Add("id = @id"); cmd.Parameters.AddWithValue("@id", id.Value); }
            if (!string.IsNullOrWhiteSpace(uploader)) { conditions.Add("uploader LIKE @uploader"); cmd.Parameters.AddWithValue("@uploader", $"%{uploader}%"); }
            if (!string.IsNullOrWhiteSpace(fileName)) { conditions.Add("original_file_name LIKE @fileName"); cmd.Parameters.AddWithValue("@fileName", $"%{fileName}%"); }
            if (!string.IsNullOrWhiteSpace(bucket)) { conditions.Add("bucketname LIKE @bucket"); cmd.Parameters.AddWithValue("@bucket", $"%{bucket}%"); }
            if (start.HasValue) { conditions.Add("upload_time >= @start"); cmd.Parameters.AddWithValue("@start", start.Value); }
            if (end.HasValue) { conditions.Add("upload_time <= @end"); cmd.Parameters.AddWithValue("@end", end.Value); }

            string whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";
            cmd.CommandText = $@"
                SELECT id FROM file_info
                {whereClause}
                ORDER BY upload_time DESC
            ";

            var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                ids.Add(reader.GetInt32("id"));
            }

            return ids;
        }

        public async Task<FileInfoModel?> GetFileByIdAsync(int id)
        {
            var result = await QueryFilesAsync(id: id);
            return result.Items.FirstOrDefault();
        }
    }
}
