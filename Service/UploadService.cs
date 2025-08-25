using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using ConsoleApp1.Models;
using ConsoleApp1.Interfaces;
using MySqlConnector;
using Microsoft.Extensions.Options;
using ConsoleApp1.Options;
using Serilog;

namespace ConsoleApp1.Service
{
    public class UploadService : IUploadService
    {
        private readonly IQueryService _iQueryService;
        private readonly IAmazonS3 _s3Client;
        private readonly TransferUtility _transferUtility;
        private readonly string _dbConnectionString;


        public UploadService(
            IOptions<MinioOptions> options,
            IQueryService iQueryService,
            IAmazonS3 s3Client,
            TransferUtility transferUtility)
        {
            var minioOptions = options.Value ?? throw new ArgumentNullException(nameof(options));
            _s3Client = s3Client;
            _transferUtility = transferUtility;
            _dbConnectionString = minioOptions.DbConnectionString;
            _iQueryService = iQueryService ?? throw new ArgumentNullException(nameof(iQueryService));
        }



private async Task<int> InsertFileInfoAsync(
    UploadResult result,
    string originalFileName,
    string storedFileName,
    string bucketName,
    string relativePath,
    string absolutePath,
    string mimeType,
    string uploader)
{
    await using var conn = new MySqlConnection(_dbConnectionString);
    await conn.OpenAsync();
    await using var cmd = conn.CreateCommand();

cmd.CommandText = @"
    INSERT INTO file_info (
        stored_file_name,
        original_file_name,
        bucketname,
        relative_path,
        absolute_path,
        file_size,
        mime_type,
        upload_time,
        uploader,
        etag
    ) VALUES (
        @storedFileName,
        @originalFileName,
        @bucketName,
        @relativePath,
        @absolutePath,
        @fileSize,
        @mimeType,
        @uploadTime,
        @uploader,
        @etag
    );
    SELECT LAST_INSERT_ID();";
 // 返回自增ID

    cmd.Parameters.AddWithValue("@storedFileName", storedFileName);
    cmd.Parameters.AddWithValue("@originalFileName", originalFileName);
    cmd.Parameters.AddWithValue("@bucketName", bucketName);
    cmd.Parameters.AddWithValue("@relativePath", relativePath);
    cmd.Parameters.AddWithValue("@absolutePath", absolutePath);
    cmd.Parameters.AddWithValue("@fileSize", result.Size);
    cmd.Parameters.AddWithValue("@mimeType", mimeType);
     cmd.Parameters.AddWithValue("@uploadTime", result.Uploadtime); 
    cmd.Parameters.AddWithValue("@uploader", uploader);
    cmd.Parameters.AddWithValue("@etag", result.ETag);

    // 返回自增ID
    var fileId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
    return fileId;
}


        /// <summary>
        /// 分片上传
        /// </summary>
        /// <param name="bucket">要上传到的桶的名称。</param>
        /// <param name="key">上传到桶后的名称。</param>
        /// <param name="filePath">上传文件的路径。</param>
        /// <param name="contentType">文件类型</param>
        /// <returns>文件的ETAG：string ETag，和文件的大小Long Size。</returns>
       public async Task<UploadResult> MultipartUploadAsync(MultipartUploadRequest request)
{
    // 生成唯一文件名，避免数据库 unique 冲突
    request.storedFileName = $"{request.username}_{DateTime.Now:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}";

    // 检查桶是否存在
    await EnsureBucketExistsAsync(request.bucket);

    // 先检查桶内是否已有同名文件（理论上不可能，因为我们生成唯一名）
    try
    {
        var metadataRequest = new GetObjectMetadataRequest
        {
            BucketName = request.bucket,
            Key = request.storedFileName
        };
        var metadataResponse = await _s3Client.GetObjectMetadataAsync(metadataRequest);

        // 如果成功获取元数据，说明文件已存在
        throw new Exception($"文件已存在，不能上传同名文件: {request.storedFileName}");
    }
    catch (AmazonS3Exception ex)
    {
        if (ex.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            throw;
        }
    }

    // 分片上传
    const long partSize = 5 * 1024 * 1024;
    var fileLength = new FileInfo(request.filePath).Length;
    var partCount = (int)Math.Ceiling((double)fileLength / partSize);

    var initRequest = new InitiateMultipartUploadRequest
    {
        BucketName = request.bucket,
        Key = request.storedFileName,
        ContentType = request.contentType
    };
    var initResponse = await _s3Client.InitiateMultipartUploadAsync(initRequest);

    var partETags = new List<PartETag>();

    using var fs = new FileStream(request.filePath, FileMode.Open, FileAccess.Read);
    for (int i = 1; i <= partCount; i++)
    {
        var buffer = new byte[partSize];
        var bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length);

        using var memStream = new MemoryStream(buffer, 0, bytesRead);
        var uploadPartRequest = new UploadPartRequest
        {
            BucketName = request.bucket,
            Key = request.storedFileName,
            UploadId = initResponse.UploadId,
            PartNumber = i,
            PartSize = bytesRead,
            InputStream = memStream
        };

        var response = await UploadPartWithRetryAsync(uploadPartRequest);
        partETags.Add(new PartETag(i, response.ETag));
    }

    var completeRequest = new CompleteMultipartUploadRequest
    {
        BucketName = request.bucket,
        Key = request.storedFileName,
        UploadId = initResponse.UploadId
    };
    completeRequest.AddPartETags(partETags);

    var completeResponse = await _s3Client.CompleteMultipartUploadAsync(completeRequest);

    var uploadTime = DateTime.Now;
    var result = new UploadResult
    {
        Originalfilename = request.originalFileName,
        ETag = completeResponse.ETag,
        Size = fileLength,
        Bucketname = request.bucket,
        Username = request.username,
        Tags = request.Tags,
        Uploadtime = uploadTime,
    };

            // 写入 file_info 并返回自增ID
            // 写入 file_info 并返回自增ID
            var fileId = await InsertFileInfoAsync(
                result,
                request.originalFileName,
                request.storedFileName,
                request.bucket,
                request.originalFileName,
                $"/{request.bucket}/{request.originalFileName}",
                request.contentType,
                request.username
            );


           Log.Information("用户 {Username} 于 {UploadTime} 成功上传文件 {OriginalFileName}, 存储名 {StoredFileName}, 大小 {FileSize} 字节",
            request.username,
            uploadTime,
            request.originalFileName,
            request.storedFileName,
            fileLength);


            // 写入标签关联
            if (request.Tags != null && request.Tags.Count > 0)
            {
                await using var conn = new MySqlConnection(_dbConnectionString);
                await conn.OpenAsync();
                await using var cmd = conn.CreateCommand();

                foreach (var tagName in request.Tags)
                {
                    int tagId;

                    // 查询标签是否存在
                    cmd.CommandText = "SELECT Id FROM tags WHERE Name = @tagName LIMIT 1;";
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@tagName", tagName);
                    var tagIdObj = await cmd.ExecuteScalarAsync();

                    if (tagIdObj == null)
                    {
                        // 不存在就插入
                        cmd.CommandText = "INSERT INTO tags (Name) VALUES (@tagName); SELECT LAST_INSERT_ID();";
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@tagName", tagName);
                        tagId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }
                    else
                    {
                        tagId = Convert.ToInt32(tagIdObj);
                    }

                    // 插入 file_tags
                    cmd.CommandText = "INSERT IGNORE INTO file_tags (FileId, TagId) VALUES (@fileId, @tagId);";
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@fileId", fileId);
                    cmd.Parameters.AddWithValue("@tagId", tagId);
                    await cmd.ExecuteNonQueryAsync();
                }
            }


    Console.WriteLine($"上传完成：文件名: {request.originalFileName}, ETag: {completeResponse.ETag}, 大小: {fileLength} 字节, Tags: {string.Join(",", request.Tags ?? new List<string>())}");

    return result;
}





        /// <summary>
        /// 上传单个分片到 Amazon S3，包含失败重试机制和超时控制。
        /// </summary>
        /// <param name="request">包含分片上传详细信息的 UploadPartRequest 对象。</param>
        /// <param name="maxRetries">允许的最大重试次数，默认值为 3。</param>
        /// <returns>
        /// 分片上传成功后返回 UploadPartResponse。
        /// 若超过最大重试次数仍上传失败，则抛出异常。
        /// </returns>
        private async Task<UploadPartResponse> UploadPartWithRetryAsync(UploadPartRequest request, int maxRetries = 3)
        {
            int retryCount = 0;//记录重试次数
            while (true)
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)); //为分片上传设置超时控制，10 秒内未完成则取消操作
                    Console.WriteLine($"开始上传 Part {request.PartNumber}, 时间: {DateTime.Now:HH:mm:ss}");
                    return await _s3Client.UploadPartAsync(request, cts.Token); // 传入 CancellationToken
                }
                catch (OperationCanceledException)
                {
                    retryCount++;
                    Console.WriteLine($"上传 Part {request.PartNumber} 超时，重试中 ({retryCount}/{maxRetries})...");
                    if (retryCount > maxRetries)// 重试次数耗尽后终止
                        throw new Exception($"上传 Part {request.PartNumber} 超时，重试{maxRetries}次后失败。");
                    await Task.Delay(1000 * retryCount);
                }
                catch (Exception ex)
                {
                    retryCount++;

                    if (retryCount > maxRetries)
                        throw new Exception($"上传 Part {request.PartNumber} 失败，重试{maxRetries}次后失败。", ex);

                    Console.WriteLine($"上传 Part {request.PartNumber} 失败，重试中 ({retryCount}/{maxRetries})...");

                    await Task.Delay(1000 * retryCount);
                }
            }
        }


        /// <summary>
        /// 查询bucket是否存在，否则创建新bucket
        /// </summary>
        private async Task EnsureBucketExistsAsync(string bucket)
        {
            var exists = await _s3Client.DoesS3BucketExistAsync(bucket);
            if (!exists)
            {
                await _s3Client.PutBucketAsync(new PutBucketRequest
                {
                    BucketName = bucket
                });
            }
        }
    
        
}

}

