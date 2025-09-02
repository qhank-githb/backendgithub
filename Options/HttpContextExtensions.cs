

namespace ConsoleApp1.Options
{
    public static class HttpContextExtensions
{
        /// <summary>
        /// 获取当前请求的用户名（从 JWT Claim 或 Identity）
        /// </summary>
        /// <param name="context">当前 HttpContext</param>
        /// <returns>用户名，如果未登录返回 null</returns>
        public static string? GetUsername(this HttpContext context)
        {
            if (context?.User == null)
                return null;

            // 优先用 Identity.Name
            var username = context.User.Identity?.Name;
            if (!string.IsNullOrEmpty(username))
                return username;

            // 再尝试用自定义 Claim
            username = context.User.FindFirst("username")?.Value;
            return string.IsNullOrEmpty(username) ? null : username;
        }
}
}

