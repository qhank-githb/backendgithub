using Microsoft.AspNetCore.Mvc;
using MinioWebBackend.Interfaces;
using Microsoft.AspNetCore.Authorization;

namespace MinioWebBackend.Controllers
{
    /// <summary>
    /// 文件批量下载控制器（需登录访问）
    /// </summary>
    /// <remarks>
    /// 提供文件批量下载功能：  
    /// - 接收多个文件 ID  
    /// - 将对应文件打包成 ZIP 压缩包  
    /// - 返回给调用方  
    /// </remarks>
    [ApiController]
    [Route("api/file")]
    //[Authorize] // ✅ 所有接口需要身份验证（JWT）
    public class FileDownloadController : ControllerBase
    {
        private readonly IDownloadService _idownloadService;

        /// <summary>
        /// 构造函数，注入文件下载服务
        /// </summary>
        /// <param name="idownloadService">文件下载服务接口</param>
        public FileDownloadController(IDownloadService idownloadService)
        {
            _idownloadService = idownloadService;
        }

        /// <summary>
        /// 批量下载文件（返回 ZIP 压缩包）
        /// </summary>
        /// /// <param name="ids">文件 ID 列表（数据库主键 ID），示例：["2","1"]</param>
        /// <returns>
        /// - 成功：返回一个 ZIP 文件流（包含所有文件）  
        /// - 失败：返回 400，包含错误信息  
        /// </returns>
        /// <response code="200">下载成功，返回 zip 文件</response>
        /// <response code="400">下载失败，可能是文件不存在或打包失败</response>
        [HttpPost("batch-download")]
        public async Task<IActionResult> BatchDownloadByIds([FromBody] List<int> ids)
        {
            // 调用服务，执行批量下载逻辑
            (Stream? zipStream, string? error) = await _idownloadService.BatchDownloadByIdsAsync(ids);

            // 如果返回流为空，表示失败
            if (zipStream == null || zipStream == Stream.Null)
            {
                return BadRequest(new { Error = "BatchDownloadFailed", Message = error });
            }

            // 返回文件流，MIME 类型为 application/zip，文件名自动带时间戳
            return File(zipStream, "application/zip", $"batch_{DateTime.Now:yyyyMMddHHmmss}.zip");
        }
    }
}
