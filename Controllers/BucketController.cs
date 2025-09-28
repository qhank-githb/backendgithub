using MinioWebBackend.Interfaces;
using Microsoft.AspNetCore.Mvc;


namespace MinioWebBackend.Controllers
{
    /// <summary>
    /// 存储桶控制器，提供与对象存储桶相关的API接口
    /// </summary>
    /// <remarks>
    /// 负责处理存储桶的查询操作，当前包含获取所有桶名的功能
    /// 依赖于<see cref="IBucketService"/>接口处理底层业务逻辑
    /// 路由规则：api/buckets（控制器名复数形式）
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    public class BucketsController : ControllerBase
    {
        /// <summary>
        /// 存储桶服务接口，用于处理存储桶的业务逻辑
        /// </summary>
        /// <remarks>
        /// 包含存储桶的查询、创建、删除等核心操作的实现
        /// 通过依赖注入获取具体实现
        /// </remarks>
        private readonly IBucketService _bucketService;

        /// <summary>
        /// 构造函数，注入存储桶服务依赖
        /// </summary>
        /// <param name="bucketService">存储桶服务的具体实现实例</param>
        public BucketsController(IBucketService bucketService)
        {
            _bucketService = bucketService;
        }

        /// <summary>
        /// 获取系统中所有存储桶的名称列表
        /// </summary>
        /// <remarks>
        /// 功能：查询并返回所有存储桶的名称集合<br/>
        /// 权限：默认允许所有已认证用户访问（可根据实际需求添加[Authorize]特性）<br/>
        /// 数据来源：通过IBucketService从底层存储系统获取
        /// </remarks>
        /// <returns>
        /// 成功：200 OK，返回字符串列表，包含所有桶名<br/>
        /// 异常：500 Internal Server Error，返回错误信息（由全局异常过滤器处理）
        /// </returns>
        /// <response code="200">成功,返回桶名列表</response>
        /// <response code="500">服务器处理错误</response>
        [HttpGet]
        public async Task<ActionResult<List<string>>> GetBuckets()
        {
            // 调用服务层方法获取所有桶名
            var buckets = await _bucketService.ListBucketsAsync();
            // 返回200状态码和桶名列表
            return Ok(buckets);
        }
    }
}
