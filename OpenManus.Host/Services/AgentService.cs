using OpenManus.Host.Models;
using OpenManus.Host.Services.Tools;
using System.Text.Json;
using System.Text;

namespace OpenManus.Host.Services;

/// <summary>
/// AI代理服务类，负责管理AI代理的执行和工具调用
/// </summary>
public class AgentService
{
    /// <summary>
    /// 存储不同会话的代理内存
    /// </summary>
    private readonly Dictionary<string, AgentMemory> _sessions = new();
    
    /// <summary>
    /// 可用的代理工具列表
    /// </summary>
    private readonly List<IAgentTool> _tools = new();
    
    /// <summary>
    /// HTTP客户端，用于网络请求
    /// </summary>
    private readonly HttpClient _httpClient;
    
    /// <summary>
    /// 文件管理服务
    /// </summary>
    private readonly FileManagementService _fileService;
    
    /// <summary>
    /// 构造函数，初始化代理服务
    /// </summary>
    /// <param name="httpClient">HTTP客户端</param>
    /// <param name="fileService">文件管理服务</param>
    public AgentService(HttpClient httpClient, FileManagementService fileService)
    {
        _httpClient = httpClient;
        _fileService = fileService;
        InitializeTools();
    }
    
    /// <summary>
    /// 初始化可用的工具
    /// </summary>
    private void InitializeTools()
    {
        _tools.Add(new FileOperationTool(_fileService));
        _tools.Add(new PythonExecuteTool());
        _tools.Add(new SearchTool(_httpClient));
        _tools.Add(new TerminateTool());
    }
    
    /// <summary>
    /// 执行代理任务
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="userMessage">用户消息</param>
    /// <param name="maxSteps">最大执行步数</param>
    /// <returns>代理执行结果</returns>
    public async Task<AgentExecutionResult> ExecuteTaskAsync(string sessionId, string userMessage, int maxSteps = 10)
    {
        var memory = GetOrCreateSession(sessionId);
        memory.AddMessage("user", userMessage);
        
        var result = new AgentExecutionResult
        {
            SessionId = sessionId,
            Steps = new List<string>()
        };
        
        try
        {
            for (int step = 1; step <= maxSteps; step++)
            {
                var stepResult = await ExecuteStepAsync(memory, step);
                result.Steps.Add($"Step {step}: {stepResult.Content}");
                
                // 检查是否需要执行工具
                if (stepResult.ToolCalls.Any())
                {
                    foreach (var toolCall in stepResult.ToolCalls)
                    {
                        var toolResult = await ExecuteToolAsync(toolCall);
                        memory.AddMessage("tool", toolResult.Content, toolCall.Id);
                        result.Steps.Add($"Tool {toolCall.Name}: {toolResult.Content}");
                    }
                    
                    // 继续下一步处理工具结果
                    continue;
                }
                
                // 检查是否完成
                if (stepResult.IsFinished || stepResult.Content.Contains("terminate", StringComparison.OrdinalIgnoreCase))
                {
                    result.IsCompleted = true;
                    result.FinalResult = stepResult.Content;
                    break;
                }
            }
            
            if (!result.IsCompleted)
            {
                result.FinalResult = "Task execution reached maximum steps without completion";
            }
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
        }
        
        return result;
    }
    
    /// <summary>
    /// 执行单个步骤
    /// </summary>
    /// <param name="memory">代理内存</param>
    /// <param name="stepNumber">步骤编号</param>
    /// <returns>代理响应</returns>
    private async Task<AgentResponse> ExecuteStepAsync(AgentMemory memory, int stepNumber)
    {
        // 构建系统提示
        var systemPrompt = BuildSystemPrompt();
        memory.Messages.Insert(0, new AgentMessage { Role = "system", Content = systemPrompt });
        
        // 模拟AI响应 - 在实际实现中，这里应该调用真实的LLM API
        var response = await SimulateAIResponseAsync(memory, stepNumber);
        
        memory.AddMessage("assistant", response.Content);
        return response;
    }
    
    /// <summary>
    /// 模拟AI响应（在实际实现中应该调用真实的LLM API）
    /// </summary>
    /// <param name="memory">代理内存</param>
    /// <param name="stepNumber">步骤编号</param>
    /// <returns>代理响应</returns>
    private async Task<AgentResponse> SimulateAIResponseAsync(AgentMemory memory, int stepNumber)
    {
        await Task.CompletedTask; // 避免async警告
        
        // 获取最后的用户消息
        var lastUserMessage = memory.Messages.LastOrDefault(m => m.Role == "user")?.Content ?? "";
        
        // 简单的响应逻辑 - 在实际实现中应该调用真实的LLM
        if (lastUserMessage.Contains("file", StringComparison.OrdinalIgnoreCase) || 
            lastUserMessage.Contains("read", StringComparison.OrdinalIgnoreCase))
        {
            return new AgentResponse
            {
                Content = "I'll help you with file operations.",
                ToolCalls = new List<ToolCall>
                {
                    new ToolCall
                    {
                        Name = "file_operation",
                        Arguments = new Dictionary<string, object>
                        {
                            ["operation"] = "list",
                            ["directory"] = ""
                        }
                    }
                }
            };
        }
        
        if (lastUserMessage.Contains("python", StringComparison.OrdinalIgnoreCase) || 
            lastUserMessage.Contains("code", StringComparison.OrdinalIgnoreCase))
        {
            return new AgentResponse
            {
                Content = "I'll execute some Python code for you.",
                ToolCalls = new List<ToolCall>
                {
                    new ToolCall
                    {
                        Name = "python_execute",
                        Arguments = new Dictionary<string, object>
                        {
                            ["code"] = "print('Hello from Python!')\nprint('Current time:', __import__('datetime').datetime.now())"
                        }
                    }
                }
            };
        }
        
        if (lastUserMessage.Contains("search", StringComparison.OrdinalIgnoreCase))
        {
            return new AgentResponse
            {
                Content = "I'll search for information.",
                ToolCalls = new List<ToolCall>
                {
                    new ToolCall
                    {
                        Name = "search",
                        Arguments = new Dictionary<string, object>
                        {
                            ["query"] = ExtractSearchQuery(lastUserMessage),
                            ["max_results"] = 3
                        }
                    }
                }
            };
        }
        
        // 默认响应
        if (stepNumber >= 3)
        {
            return new AgentResponse
            {
                Content = $"I've completed the analysis of your request: '{lastUserMessage}'. The task has been processed successfully.",
                IsFinished = true
            };
        }
        
        return new AgentResponse
        {
            Content = $"I understand your request: '{lastUserMessage}'. Let me process this step by step."
        };
    }
    
    /// <summary>
    /// 从消息中提取搜索查询
    /// </summary>
    /// <param name="message">用户消息</param>
    /// <returns>提取的搜索查询</returns>
    private string ExtractSearchQuery(string message)
    {
        // 简单的查询提取逻辑
        var words = message.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var searchIndex = Array.FindIndex(words, w => w.Contains("search", StringComparison.OrdinalIgnoreCase));
        
        if (searchIndex >= 0 && searchIndex < words.Length - 1)
        {
            return string.Join(" ", words.Skip(searchIndex + 1));
        }
        
        return message;
    }
    
    /// <summary>
    /// 执行工具调用
    /// </summary>
    /// <param name="toolCall">工具调用信息</param>
    /// <returns>工具执行结果</returns>
    private async Task<ToolResult> ExecuteToolAsync(ToolCall toolCall)
    {
        var tool = _tools.FirstOrDefault(t => t.Name == toolCall.Name);
        if (tool == null)
        {
            return new ToolResult
            {
                ToolCallId = toolCall.Id,
                Content = $"Unknown tool: {toolCall.Name}",
                IsSuccess = false,
                Error = "Tool not found"
            };
        }
        
        try
        {
            var result = await tool.ExecuteAsync(toolCall.Arguments);
            return new ToolResult
            {
                ToolCallId = toolCall.Id,
                Content = result,
                IsSuccess = true
            };
        }
        catch (Exception ex)
        {
            return new ToolResult
            {
                ToolCallId = toolCall.Id,
                Content = $"Tool execution failed: {ex.Message}",
                IsSuccess = false,
                Error = ex.Message
            };
        }
    }
    
    /// <summary>
    /// 构建系统提示词
    /// </summary>
    /// <returns>系统提示词</returns>
    private string BuildSystemPrompt()
    {
        var toolDescriptions = _tools.Select(t => $"- {t.Name}: {t.Description}").ToList();
        
        return $@"You are Manus, a versatile AI assistant that can help with various tasks.

Available tools:
{string.Join("\n", toolDescriptions)}

You should:
1. Analyze the user's request carefully
2. Use appropriate tools to accomplish the task
3. Provide clear and helpful responses
4. Break down complex tasks into manageable steps

When you need to use a tool, clearly indicate which tool you're using and why.";
    }
    
    /// <summary>
    /// 获取或创建会话
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <returns>代理内存</returns>
    private AgentMemory GetOrCreateSession(string sessionId)
    {
        if (!_sessions.ContainsKey(sessionId))
        {
            _sessions[sessionId] = new AgentMemory();
        }
        return _sessions[sessionId];
    }
    
    /// <summary>
    /// 获取指定会话
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <returns>代理内存，如果不存在则返回null</returns>
    public AgentMemory? GetSession(string sessionId)
    {
        return _sessions.TryGetValue(sessionId, out var session) ? session : null;
    }
    
    /// <summary>
    /// 清除指定会话
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    public void ClearSession(string sessionId)
    {
        if (_sessions.ContainsKey(sessionId))
        {
            _sessions[sessionId].Clear();
        }
    }
    
    public List<IAgentTool> GetAvailableTools()
    {
        return _tools.ToList();
    }
}