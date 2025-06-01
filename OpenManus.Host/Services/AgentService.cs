using OpenManus.Host.Models;
using OpenManus.Host.Services.Tools;
using System.Text.Json;
using System.Text;

namespace OpenManus.Host.Services;

public class AgentService
{
    private readonly Dictionary<string, AgentMemory> _sessions = new();
    private readonly List<IAgentTool> _tools = new();
    private readonly HttpClient _httpClient;
    private readonly FileManagementService _fileService;
    
    public AgentService(HttpClient httpClient, FileManagementService fileService)
    {
        _httpClient = httpClient;
        _fileService = fileService;
        InitializeTools();
    }
    
    private void InitializeTools()
    {
        _tools.Add(new FileOperationTool(_fileService));
        _tools.Add(new PythonExecuteTool());
        _tools.Add(new SearchTool(_httpClient));
        _tools.Add(new TerminateTool());
    }
    
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
    
    private AgentMemory GetOrCreateSession(string sessionId)
    {
        if (!_sessions.ContainsKey(sessionId))
        {
            _sessions[sessionId] = new AgentMemory();
        }
        return _sessions[sessionId];
    }
    
    public AgentMemory? GetSession(string sessionId)
    {
        return _sessions.TryGetValue(sessionId, out var session) ? session : null;
    }
    
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