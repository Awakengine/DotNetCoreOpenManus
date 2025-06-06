using System.Net;
using System.Text.Json;

namespace OpenManus.Web.Middleware
{
    /// <summary>
    /// 全局异常处理中间件
    /// </summary>
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";
            
            string userFriendlyMessage = "处理请求时发生错误，请稍后重试。";
            string details = exception.Message;
            
            // 检查是否为认证相关的异常
            if (exception is InvalidOperationException && 
                exception.Message.Contains("authentication handler"))
            {
                userFriendlyMessage = "访问权限验证失败，请重新登录或使用游客模式。";
                details = "认证处理器配置错误";
            }
            
            var response = new
            {
                error = new
                {
                    message = userFriendlyMessage,
                    details = details,
                    type = exception.GetType().Name
                }
            };

            switch (exception)
            {
                case ArgumentNullException:
                case ArgumentException:
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;
                case UnauthorizedAccessException:
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    response = new
                    {
                        error = new
                        {
                            message = "您没有权限访问此资源，请先登录。",
                            details = "未授权访问",
                            type = exception.GetType().Name
                        }
                    };
                    break;
                case InvalidOperationException when exception.Message.Contains("authentication handler"):
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    break;
                case FileNotFoundException:
                case DirectoryNotFoundException:
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    response = new
                    {
                        error = new
                        {
                            message = "请求的资源不存在。",
                            details = "文件或目录未找到",
                            type = exception.GetType().Name
                        }
                    };
                    break;
                case TimeoutException:
                    context.Response.StatusCode = (int)HttpStatusCode.RequestTimeout;
                    response = new
                    {
                        error = new
                        {
                            message = "请求超时，请稍后重试。",
                            details = "操作超时",
                            type = exception.GetType().Name
                        }
                    };
                    break;
                default:
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    break;
            }

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }

    /// <summary>
    /// 全局异常处理中间件扩展方法
    /// </summary>
    public static class GlobalExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionMiddleware>();
        }
    }
}