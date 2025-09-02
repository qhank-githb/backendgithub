using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using ConsoleApp1.Models;
using ConsoleApp1.Interfaces;
using MySqlConnector;
using Microsoft.Extensions.Options;
using ConsoleApp1.Options;
using Serilog;
using Microsoft.EntityFrameworkCore;

namespace ConsoleApp1.Service
{
    public class UploadService : IUploadService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly AppDbContext _dbContext;



        public UploadService(
            AppDbContext dbContext,
            IOptions<MinioOptions> options,
            IAmazonS3 s3Client)
        {
            _dbContext = dbContext;
            _s3Client = s3Client;
        }



        private async Task<int> InsertFileInfoAsync(FileInfoModel fileInfo)
        {
            var entity = new FileRecord
            {
                StoredFileName = fileInfo.StoredFileName,
                OriginalFileName = fileInfo.OriginalFileName,
                BucketName     = fileInfo.Bucketname,   // 注意模型字段名大小写不一致
                RelativePath   = fileInfo.RelativePath,
                AbsolutePath   = fileInfo.AbsolutePath,
                FileSize       = fileInfo.FileSize,
                MimeType       = fileInfo.MimeType,
                UploadTime     = fileInfo.UploadTime,
                Uploader       = fileInfo.Uploader,
                ETag           = fileInfo.ETag
            };

            _dbContext.FileRecords.Add(entity);
            await _dbContext.SaveChangesAsync();
            return entity.Id;  // EF 自动回填自增主键
            }
            

            private async Task UpsertFileTagsAsync(int fileId, IEnumerable<string> tagNames)
            {
                if (tagNames == null) return;

                var names = tagNames
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (names.Count == 0) return;

                // 先查已有标签
                var existing = await _dbContext.Tags
                    .Where(t => names.Contains(t.Name))
                    .ToListAsync();

                var existingNames = existing.Select(t => t.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
                var toCreateNames = names.Where(n => !existingNames.Contains(n)).ToList();

                // 批量创建新的标签
                foreach (var n in toCreateNames)
                    _dbContext.Tags.Add(new Models.Tag { Name = n });

                if (toCreateNames.Count > 0)
                    await _dbContext.SaveChangesAsync(); // 确保新标签拿到 Id

                // 取所有需要的标签（新旧合并）
                var allTagIds = await _dbContext.Tags
                    .Where(t => names.Contains(t.Name))
                    .Select(t => t.Id)
                    .ToListAsync();

                // 已有关联
                var existingFileTagIds = await _dbContext.FileTags
                    .Where(ft => ft.FileId == fileId)
                    .Select(ft => ft.TagId)
                    .ToListAsync();

                var needRelate = allTagIds.Except(existingFileTagIds).ToList();

                foreach (var tagId in needRelate)
                    _dbContext.FileTags.Add(new FileTag { FileId = fileId, TagId = tagId });

                if (needRelate.Count > 0)
                    await _dbContext.SaveChangesAsync();
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
            try
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
            
            // 1) 组装 FileInfoModel（业务模型）
            var fileInfo = new FileInfoModel
            {
                StoredFileName  = request.storedFileName,
                OriginalFileName= request.originalFileName,
                Bucketname      = request.bucket,
                RelativePath = request.storedFileName,
                AbsolutePath = $"/{request.bucket}/{request.storedFileName}",
                FileSize        = fileLength,
                MimeType        = request.contentType,
                UploadTime      = uploadTime,
                Uploader        = request.username,
                ETag            = completeResponse.ETag,
                Tags            = request.Tags ?? new List<string>()
            };

            // 2) 用事务保证“文件 + 标签关联”一致
            await using var tx = await _dbContext.Database.BeginTransactionAsync();

            var fileId = await InsertFileInfoAsync(fileInfo);
            await UpsertFileTagsAsync(fileId, fileInfo.Tags);

            await tx.CommitAsync();



                Console.WriteLine($"上传完成：文件名: {request.originalFileName}, ETag: {completeResponse.ETag}, 大小: {fileLength} 字节, Tags: {string.Join(",", request.Tags ?? new List<string>())}");

                Log.Information("用户 {Username} 于 {UploadTime} 成功上传文件 {OriginalFileName}, 存储名 {StoredFileName}, 标签：{tags} , 大小 {FileSize} 字节",
                 request.username,
                 uploadTime,
                 request.originalFileName,
                 request.storedFileName,
                 string.Join(",", request.Tags ?? new List<string>()),
                 fileLength);
                return result;

            }
            catch (Exception ex)
            {
                Log.Error(ex, "用户 {Username} 上传文件 {OriginalFileName} 失败，存储名 {StoredFileName}",
            request.username, request.originalFileName, request.storedFileName ?? "未生成");
                throw; // 继续抛出，交给全局异常过滤器处理
            }

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

