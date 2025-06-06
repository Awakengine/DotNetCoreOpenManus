using System.Text;

namespace OpenManus.WebUI.Services
{
    /// <summary>
    /// HTTP客户端服务接口
    /// </summary>
    public interface IHttpClientService
    {
        /// <summary>
        /// 发送POST请求
        /// </summary>
        Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content, string? authToken = null);
        
        /// <summary>
        /// 发送GET请求
        /// </summary>
        Task<HttpResponseMessage> GetAsync(string requestUri, string? authToken = null);
        
        /// <summary>
        /// 发送自定义请求
        /// </summary>
        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// 创建带认证头的请求消息
        /// </summary>
        HttpRequestMessage CreateRequestWithAuth(HttpMethod method, string requestUri, string? authToken = null);
    }

    /// <summary>
    /// HTTP客户端服务实现
    /// </summary>
    public class HttpClientService : IHttpClientService
    {
        private readonly HttpClient _httpClient;

        public HttpClientService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// 发送POST请求
        /// </summary>
        public async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content, string? authToken = null)
        {
            using var request = CreateRequestWithAuth(HttpMethod.Post, requestUri, authToken);
            request.Content = content;
            return await _httpClient.SendAsync(request);
        }

        /// <summary>
        /// 发送GET请求
        /// </summary>
        public async Task<HttpResponseMessage> GetAsync(string requestUri, string? authToken = null)
        {
            using var request = CreateRequestWithAuth(HttpMethod.Get, requestUri, authToken);
            return await _httpClient.SendAsync(request);
        }

        /// <summary>
        /// 发送自定义请求
        /// </summary>
        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead, CancellationToken cancellationToken = default)
        {
            return await _httpClient.SendAsync(request, completionOption, cancellationToken);
        }

        /// <summary>
        /// 创建带认证头的请求消息
        /// </summary>
        public HttpRequestMessage CreateRequestWithAuth(HttpMethod method, string requestUri, string? authToken = null)
        {
            var request = new HttpRequestMessage(method, requestUri);
            
            if (!string.IsNullOrEmpty(authToken))
            {
                request.Headers.Add("Authorization", $"Bearer {authToken}");
            }
            
            return request;
        }
    }
}