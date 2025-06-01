using System.Text.Json;
using System.Text;

namespace OpenManus.Host.Services.Tools;

/// <summary>
/// 搜索工具，用于在互联网上搜索信息
/// </summary>
public class SearchTool : BaseAgentTool
{
    /// <summary>
    /// HTTP客户端
    /// </summary>
    private readonly HttpClient _httpClient;
    
    /// <summary>
    /// 构造函数，初始化搜索工具
    /// </summary>
    /// <param name="httpClient">HTTP客户端</param>
    public SearchTool(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
    /// <summary>
    /// 工具名称
    /// </summary>
    public override string Name => "search";
    
    /// <summary>
    /// 工具描述
    /// </summary>
    public override string Description => "Search the internet for information";
    
    /// <summary>
    /// 执行搜索操作
    /// </summary>
    /// <param name="arguments">包含query和max_results参数的字典</param>
    /// <returns>搜索结果</returns>
    public override async Task<string> ExecuteAsync(Dictionary<string, object> arguments)
    {
        await Task.CompletedTask; // 避免async警告
        
        var query = GetArgument<string>(arguments, "query", "");
        var maxResults = GetArgument<int>(arguments, "max_results", 5);
        
        if (string.IsNullOrWhiteSpace(query))
        {
            return "Error: No search query provided";
        }
        
        try
        {
            // 模拟搜索结果 - 在实际实现中，这里应该调用真实的搜索API
            var searchResults = new List<object>
            {
                new { title = "Search Result 1", url = "https://example.com/1", snippet = $"Information about {query}" },
                new { title = "Search Result 2", url = "https://example.com/2", snippet = $"More details on {query}" },
                new { title = "Search Result 3", url = "https://example.com/3", snippet = $"Additional context for {query}" }
            };
            
            var results = searchResults.Take(maxResults).ToList();
            var resultText = new StringBuilder();
            resultText.AppendLine($"Search results for '{query}':");
            
            for (int i = 0; i < results.Count; i++)
            {
                var result = results[i];
                var resultJson = JsonSerializer.Serialize(result);
                var resultObj = JsonSerializer.Deserialize<Dictionary<string, object>>(resultJson);
                
                resultText.AppendLine($"{i + 1}. {resultObj?["title"]}");
                resultText.AppendLine($"   URL: {resultObj?["url"]}");
                resultText.AppendLine($"   Snippet: {resultObj?["snippet"]}");
                resultText.AppendLine();
            }
            
            return resultText.ToString();
        }
        catch (Exception ex)
        {
            return $"Error performing search: {ex.Message}";
        }
    }
    
    /// <summary>
    /// 获取工具的JSON Schema定义
    /// </summary>
    /// <returns>工具的Schema定义</returns>
    public override Dictionary<string, object> GetSchema()
    {
        return new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["query"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "Search query"
                },
                ["max_results"] = new Dictionary<string, object>
                {
                    ["type"] = "integer",
                    ["description"] = "Maximum number of search results to return",
                    ["default"] = 5
                }
            },
            ["required"] = new[] { "query" }
        };
    }
}

/// <summary>
/// 终止工具，用于终止当前任务执行
/// </summary>
public class TerminateTool : BaseAgentTool
{
    /// <summary>
    /// 工具名称
    /// </summary>
    public override string Name => "terminate";
    
    /// <summary>
    /// 工具描述
    /// </summary>
    public override string Description => "Terminate the current task execution";
    
    /// <summary>
    /// 执行终止操作
    /// </summary>
    /// <param name="arguments">包含reason参数的字典</param>
    /// <returns>终止结果</returns>
    public override Task<string> ExecuteAsync(Dictionary<string, object> arguments)
    {
        var reason = GetArgument<string>(arguments, "reason", "Task completed");
        return Task.FromResult($"Task terminated: {reason}");
    }
    
    /// <summary>
    /// 获取工具的JSON Schema定义
    /// </summary>
    /// <returns>工具的Schema定义</returns>
    public override Dictionary<string, object> GetSchema()
    {
        return new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["reason"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "Reason for termination",
                    ["default"] = "Task completed"
                }
            }
        };
    }
}