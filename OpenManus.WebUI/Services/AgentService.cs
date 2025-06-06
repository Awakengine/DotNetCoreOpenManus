using OpenManus.WebUI.Models;
using OpenManus.WebUI.Services.Tools;
using System.Text.Json;
using System.Text;
using System.Collections.Concurrent;

namespace OpenManus.WebUI.Services;

/// <summary>
/// AI代理服务类，负责管理AI代理的执行和工具调用
/// </summary>
public class AgentService
{
    /// <summary>
    /// 存储不同会话的代理内存
    /// </summary>
    private readonly ConcurrentDictionary<string, AgentMemory> _sessions = new();

    /// <summary>
    /// 可用的代理工具列表
    /// </summary>
    private readonly List<IAgentTool> _tools = new();

    /// <summary>
    /// HTTP客户端服务，用于网络请求
    /// </summary>
    private readonly IHttpClientService _httpClientService;

    /// <summary>
    /// 配置服务
    /// </summary>
    private readonly IConfigurationService _configurationService;
    
    /// <summary>
    /// 聊天历史服务
    /// </summary>
    private readonly IChatHistoryService _chatHistoryService;
    
    /// <summary>
    /// 服务提供者
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// 构造函数，初始化代理服务
    /// </summary>
    /// <param name="httpClientService">HTTP客户端服务</param>
    /// <param name="configurationService">配置服务</param>
    /// <param name="chatHistoryService">聊天历史服务</param>
    /// <param name="serviceProvider">服务提供者</param>
    public AgentService(
        IHttpClientService httpClientService,
        IConfigurationService configurationService,
        IChatHistoryService chatHistoryService,
        IServiceProvider serviceProvider)
    {
        _httpClientService = httpClientService;
        _configurationService = configurationService;
        _chatHistoryService = chatHistoryService;
        _serviceProvider = serviceProvider;
        // 延迟初始化工具，避免循环依赖
    }

    /// <summary>
    /// 初始化可用的工具
    /// </summary>
    private void InitializeTools()
    {
        if (_tools.Count > 0) return; // 避免重复初始化
        
        var fileService = _serviceProvider.GetRequiredService<FileManagementService>();
        _tools.Add(new FileOperationTool(fileService));
        _tools.Add(new PythonExecuteTool());
        _tools.Add(new SearchTool(_httpClientService));
        _tools.Add(new TerminateTool());
    }

    /// <summary>
    /// 执行代理任务
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="userMessage">用户消息</param>
    /// <param name="maxSteps">最大执行步数</param>
    /// <returns>代理执行结果</returns>
    public async Task<AgentExecutionResult> ExecuteTaskAsync(string sessionId, string userMessage, int maxSteps = 2, string? customModel = null)
    {
        InitializeTools(); // 确保工具已初始化
        
        var memory = GetOrCreateSession(sessionId);
        memory.AddMessage(memory.Messages.Count + 1, "user", userMessage);

        var result = new AgentExecutionResult
        {
            SessionId = sessionId,
            Steps = new List<string>()
        };

        try
        {
            var totalUsage = new LlmUsage();

            for (int step = 1; step <= maxSteps; step++)
            {
                var (stepResult, usage) = await ExecuteStepAsync(memory, step, customModel);
                result.Steps.Add($"Step {step}: {stepResult.Content}");

                // 累计使用情况统计
                if (usage != null)
                {
                    totalUsage.PromptTokens += usage.PromptTokens;
                    totalUsage.CompletionTokens += usage.CompletionTokens;
                    totalUsage.TotalTokens += usage.TotalTokens;
                }

                // 检查是否需要执行工具
                if (stepResult.ToolCalls.Any())
                {
                    foreach (var toolCall in stepResult.ToolCalls)
                    {
                        var toolResult = await ExecuteToolAsync(toolCall);
                        memory.AddMessage(memory.Messages.Count + 1, "tool", toolResult.Content, toolCall.Id);
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

            // 设置总的使用情况统计
            if (totalUsage.TotalTokens > 0)
            {
                result.Usage = totalUsage;
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
    /// <param name="customModel">自定义模型</param>
    /// <returns>代理响应和使用情况统计</returns>
    private async Task<(AgentResponse response, LlmUsage? usage)> ExecuteStepAsync(AgentMemory memory, int stepNumber, string? customModel = null)
    {
        // 构建系统提示
        var systemPrompt = BuildSystemPrompt();
        memory.Messages.Insert(0, new AgentMessage { Role = "system", Content = systemPrompt });

        // 调用真实的LLM API
        (AgentResponse response, LlmUsage? usage) = await SimulateAIResponseAsync(memory, stepNumber, customModel);

        memory.AddMessage(memory.Messages.Count + 1, "assistant", response.Content);
        return (response, usage);
    }

    /// <summary>
    /// 调用真实的LLM API获取AI响应
    /// </summary>
    /// <param name="memory">代理内存</param>
    /// <param name="stepNumber">步骤编号</param>
    /// <returns>代理响应</returns>
    private async Task<(AgentResponse response, LlmUsage? usage)> SimulateAIResponseAsync(AgentMemory memory, int stepNumber, string? customModel = null)
    {
        try
        {
            var appSettings = _configurationService.GetAppSettings();
            var llmConfig = appSettings.LLMConfig;

            // 构建请求消息
            var messages = memory.Messages.Select(m => new
            {
                role = m.Role,
                content = m.Content
            }).ToList();

            // 构建请求体，优先使用自定义模型
            var requestBody = new
            {
                model = customModel ?? llmConfig.Model,
                messages = messages,
                max_tokens = llmConfig.MaxTokens,
                temperature = llmConfig.Temperature,
                stream = false
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            
            // 使用HttpClientService发送请求
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var response = await _httpClientService.PostAsync($"{llmConfig.BaseUrl.TrimEnd('/')}/chat/completions", content, llmConfig.ApiKey);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();

                // 使用新的DTO类解析响应
                var llmResponse = JsonSerializer.Deserialize<LlmApiResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    PropertyNameCaseInsensitive = true
                });

                if (llmResponse?.Choices != null && llmResponse.Choices.Count > 0)
                {
                    var firstChoice = llmResponse.Choices[0];
                    if (firstChoice.Message != null)
                    {
                        var aiResponse = firstChoice.Message.Content ?? "";

                        // 检查是否包含工具调用（这里可以根据实际LLM的响应格式进行调整）
                        var toolCalls = ParseToolCalls(aiResponse);

                        var agentResponse = new AgentResponse
                        {
                            Content = aiResponse,
                            ToolCalls = toolCalls,
                            IsFinished = aiResponse.Contains("任务完成", StringComparison.OrdinalIgnoreCase) ||
                                        aiResponse.Contains("task completed", StringComparison.OrdinalIgnoreCase) ||
                                        firstChoice.FinishReason == "stop"
                        };

                        return (agentResponse, llmResponse.Usage);
                    }
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"LLM API调用失败: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            // 如果API调用失败，返回错误信息
            var errorResponse = new AgentResponse
            {
                Content = $"抱歉，AI服务暂时不可用: {ex.Message}",
                IsFinished = false
            };
            return (errorResponse, null);
        }

        // 默认响应
        var defaultResponse = new AgentResponse
        {
            Content = "抱歉，无法获取AI响应。",
            IsFinished = false
        };
        return (defaultResponse, null);
    }

    /// <summary>
    /// 流式调用LLM API获取AI响应
    /// </summary>
    /// <param name="memory">代理内存</param>
    /// <param name="onContentReceived">接收到内容时的回调</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最终的代理响应和使用情况统计</returns>
    public async Task<(AgentResponse response, LlmUsage? usage)> StreamAIResponseAsync(AgentMemory memory, Func<string, Task> onContentReceived, CancellationToken cancellationToken = default, string? customModel = null)
    {
        try
        {
            var appSettings = _configurationService.GetAppSettings();
            var llmConfig = appSettings.LLMConfig;

            // 构建请求消息
            var messages = memory.Messages.OrderBy(o => o.OrderBy).Select(m => new
            {
                role = m.Role,
                content = m.Content
            }).ToList();

            var messages2 = memory.Messages.OrderByDescending(o => o.OrderBy).Select(m => new
            {
                role = m.Role,
                content = m.Content
            }).ToList();

            // 构建请求体（启用流式响应），优先使用自定义模型
            var requestBody = new
            {
                model = customModel ?? llmConfig.Model,
                messages = messages,
                max_tokens = llmConfig.MaxTokens,
                temperature = llmConfig.Temperature,
                stream = true
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            
            // 使用HttpClientService创建请求
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            using var request = _httpClientService.CreateRequestWithAuth(HttpMethod.Post, $"{llmConfig.BaseUrl.TrimEnd('/')}/chat/completions", llmConfig.ApiKey);
            request.Content = content;
            
            // 发送请求并获取流式响应
            var response = await _httpClientService.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var fullContent = new StringBuilder();
                LlmUsage? usage = null;
                bool isFinished = false;

                using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var reader = new StreamReader(stream);

                string? line;
                while ((line = await reader.ReadLineAsync()) != null && !cancellationToken.IsCancellationRequested)
                {
                    if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
                        continue;

                    var data = line.Substring(6); // 移除 "data: " 前缀

                    if (data == "[DONE]")
                    {
                        isFinished = true;
                        break;
                    }

                    try
                    {
                        // System.Console.WriteLine(data);
                        var streamResponse = JsonSerializer.Deserialize<LlmApiResponse>(data, new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                            PropertyNameCaseInsensitive = true
                        });

                        if (streamResponse?.Choices != null && streamResponse.Choices.Count > 0)
                        {
                            var choice = streamResponse.Choices[0];
                            if (choice.Message?.Content != null)
                            {
                                var deltaContent = choice.Message.Content;
                                fullContent.Append(deltaContent);

                                // 实时回调新内容
                                await onContentReceived(deltaContent);
                            }
                            else if (!string.IsNullOrWhiteSpace(choice.Delta.Content))
                            {
                                var deltaContent = choice.Delta.Content;
                                fullContent.Append(deltaContent);

                                // 实时回调新内容
                                await onContentReceived(deltaContent);
                            }

                            // 检查是否完成
                            if (choice.FinishReason == "stop")
                            {
                                isFinished = true;
                            }
                        }

                        // 获取使用情况统计（通常在最后一个响应中）
                        if (streamResponse.Usage != null)
                        {
                            usage = streamResponse.Usage;
                        }
                    }
                    catch (JsonException)
                    {
                        // 忽略JSON解析错误，继续处理下一行
                        continue;
                    }
                }

                var finalContent = fullContent.ToString();
                var toolCalls = ParseToolCalls(finalContent);

                var agentResponse = new AgentResponse
                {
                    Content = finalContent,
                    ToolCalls = toolCalls,
                    IsFinished = isFinished || finalContent.Contains("任务完成", StringComparison.OrdinalIgnoreCase) ||
                                finalContent.Contains("task completed", StringComparison.OrdinalIgnoreCase)
                };

                return (agentResponse, usage);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new Exception($"LLM API调用失败: {response.StatusCode} - {errorContent}");
            }
        }
        catch (OperationCanceledException)
        {
            var cancelledResponse = new AgentResponse
            {
                Content = "请求已取消",
                IsFinished = true
            };
            return (cancelledResponse, null);
        }
        catch (Exception ex)
        {
            var errorResponse = new AgentResponse
            {
                Content = $"抱歉，AI服务暂时不可用: {ex.Message}",
                IsFinished = false
            };
            return (errorResponse, null);
        }
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
    /// 解析AI响应中的工具调用
    /// </summary>
    /// <param name="aiResponse">AI响应内容</param>
    /// <returns>工具调用列表</returns>
    private List<ToolCall> ParseToolCalls(string aiResponse)
    {
        var toolCalls = new List<ToolCall>();

        // 这里可以根据实际LLM的响应格式来解析工具调用
        // 目前使用简单的关键词匹配逻辑

        if (aiResponse.Contains("文件", StringComparison.OrdinalIgnoreCase) ||
            aiResponse.Contains("file", StringComparison.OrdinalIgnoreCase))
        {
            toolCalls.Add(new ToolCall
            {
                Name = "file_operation",
                Arguments = new Dictionary<string, object>
                {
                    ["operation"] = "list",
                    ["directory"] = ""
                }
            });
        }

        if (aiResponse.Contains("python", StringComparison.OrdinalIgnoreCase) ||
            aiResponse.Contains("代码", StringComparison.OrdinalIgnoreCase))
        {
            toolCalls.Add(new ToolCall
            {
                Name = "python_execute",
                Arguments = new Dictionary<string, object>
                {
                    ["code"] = "print('Hello from Python!')"
                }
            });
        }

        if (aiResponse.Contains("搜索", StringComparison.OrdinalIgnoreCase) ||
            aiResponse.Contains("search", StringComparison.OrdinalIgnoreCase))
        {
            toolCalls.Add(new ToolCall
            {
                Name = "search",
                Arguments = new Dictionary<string, object>
                {
                    ["query"] = "搜索查询",
                    ["max_results"] = 3
                }
            });
        }

        return toolCalls;
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

    /// <summary>
    /// 获取所有可用的代理工具
    /// </summary>
    /// <returns>代理工具列表</returns>
    public List<IAgentTool> GetAvailableTools()
    {
        return _tools.ToList();
    }
}