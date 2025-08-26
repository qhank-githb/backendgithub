using ConsoleApp1.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace ConsoleApp1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BucketsController : ControllerBase
    {
        private readonly IBucketService _bucketService;

        public BucketsController(IBucketService bucketService)
        {
            _bucketService = bucketService;
        }

        /// <summary>
        /// 获取所有桶名
        /// GET: /api/buckets
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<string>>> GetBuckets()
        {
            var buckets = await _bucketService.ListBucketsAsync();
            return Ok(buckets);
        }
    }
}
