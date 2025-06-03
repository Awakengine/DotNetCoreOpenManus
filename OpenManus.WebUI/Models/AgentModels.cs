using System.Text.Json.Serialization;

namespace OpenManus.WebUI.Models;

/// <summary>
/// 智能体状态枚举
/// </summary>
public enum AgentState
{
    /// <summary>
    /// 空闲状态
    /// </summary>
    Idle,
    
    /// <summary>
    /// 运行状态
    /// </summary>
    Running,
    
    /// <summary>
    /// 完成状态
    /// </summary>
    Finished,
    
    /// <summary>
    /// 错误状态
    /// </summary>
    Error
}

/// <summary>
/// 智能体消息类
/// </summary>
public class AgentMessage
{
    /// <summary>
    /// 消息角色（user/assistant/system等）
    /// </summary>
    public string Role { get; set; } = string.Empty;
    
    /// <summary>
    /// 消息内容
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 工具调用ID
    /// </summary>
    public string? ToolCallId { get; set; }
    
    /// <summary>
    /// 元数据
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
    
    /// <summary>
    /// 执行状态信息（仅对assistant消息有效）
    /// </summary>
    public AgentExecutionResult? ExecutionStatus { get; set; }
}

/// <summary>
/// 智能体记忆类，用于存储对话历史
/// </summary>
public class AgentMemory
{
    /// <summary>
    /// 消息列表
    /// </summary>
    public List<AgentMessage> Messages { get; set; } = new();
    
    /// <summary>
    /// 添加消息到记忆中
    /// </summary>
    /// <param name="role">消息角色</param>
    /// <param name="content">消息内容</param>
    /// <param name="toolCallId">工具调用ID</param>
    public void AddMessage(string role, string content, string? toolCallId = null)
    {
        Messages.Add(new AgentMessage
        {
            Role = role,
            Content = content,
            ToolCallId = toolCallId
        });
    }
    
    /// <summary>
    /// 添加带执行状态的消息到记忆中
    /// </summary>
    /// <param name="role">消息角色</param>
    /// <param name="content">消息内容</param>
    /// <param name="executionStatus">执行状态</param>
    /// <param name="toolCallId">工具调用ID</param>
    public void AddMessage(string role, string content, AgentExecutionResult? executionStatus, string? toolCallId = null)
    {
        Messages.Add(new AgentMessage
        {
            Role = role,
            Content = content,
            ToolCallId = toolCallId,
            ExecutionStatus = executionStatus
        });
    }
    
    /// <summary>
    /// 清空记忆
    /// </summary>
    public void Clear()
    {
        Messages.Clear();
    }
}

/// <summary>
/// 工具调用类
/// </summary>
public class ToolCall
{
    /// <summary>
    /// 工具调用唯一标识符
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// 工具名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 工具参数
    /// </summary>
    public Dictionary<string, object> Arguments { get; set; } = new();
}

/// <summary>
/// 工具执行结果类
/// </summary>
public class ToolResult
{
    /// <summary>
    /// 对应的工具调用ID
    /// </summary>
    public string ToolCallId { get; set; } = string.Empty;
    
    /// <summary>
    /// 执行结果内容
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否执行成功
    /// </summary>
    public bool IsSuccess { get; set; } = true;
    
    /// <summary>
    /// 错误信息
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// 智能体响应类
/// </summary>
public class AgentResponse
{
    /// <summary>
    /// 响应内容
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// 工具调用列表
    /// </summary>
    public List<ToolCall> ToolCalls { get; set; } = new();
    
    /// <summary>
    /// 是否已完成
    /// </summary>
    public bool IsFinished { get; set; }
    
    /// <summary>
    /// 错误信息
    /// </summary>
    public string? Error { get; set; }
}

/// <summary>
/// LLM API响应DTO类
/// </summary>
public class LlmApiResponse
{
    /// <summary>
    /// 响应ID
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// 对象类型
    /// </summary>
    public string Object { get; set; } = string.Empty;
    
    /// <summary>
    /// 创建时间戳
    /// </summary>
    public long Created { get; set; }
    
    /// <summary>
    /// 使用的模型
    /// </summary>
    public string Model { get; set; } = string.Empty;
    
    /// <summary>
    /// 选择列表
    /// </summary>
    public List<LlmChoice> Choices { get; set; } = new();
    
    /// <summary>
    /// 使用情况统计
    /// </summary>
    public LlmUsage? Usage { get; set; }
    
    /// <summary>
    /// 统计信息
    /// </summary>
    public object? Stats { get; set; }
    
    /// <summary>
    /// 系统指纹
    /// </summary>
    public string? SystemFingerprint { get; set; }
}

/// <summary>
/// LLM选择项DTO类
/// </summary>
public class LlmChoice
{
    /// <summary>
    /// 选择索引
    /// </summary>
    public int Index { get; set; }
    
    /// <summary>
    /// 日志概率
    /// </summary>
    public object? Logprobs { get; set; }
    
    /// <summary>
    /// 完成原因
    /// </summary>
    public string? FinishReason { get; set; }
    
    /// <summary>
    /// 消息内容
    /// </summary>
    public LlmMessage? Message { get; set; }
}

/// <summary>
/// LLM消息DTO类
/// </summary>
public class LlmMessage
{
    /// <summary>
    /// 消息角色
    /// </summary>
    public string Role { get; set; } = string.Empty;
    
    /// <summary>
    /// 消息内容
    /// </summary>
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// LLM使用情况统计DTO类
/// </summary>
public class LlmUsage
{
    /// <summary>
    /// 提示词令牌数
    /// </summary>
    public int PromptTokens { get; set; }
    
    /// <summary>
    /// 完成令牌数
    /// </summary>
    public int CompletionTokens { get; set; }
    
    /// <summary>
    /// 总令牌数
    /// </summary>
    public int TotalTokens { get; set; }
}

/// <summary>
/// 智能体执行结果类
/// </summary>
public class AgentExecutionResult
{
    /// <summary>
    /// 会话ID
    /// </summary>
    public string SessionId { get; set; } = string.Empty;
    
    /// <summary>
    /// 执行步骤列表
    /// </summary>
    public List<string> Steps { get; set; } = new();
    
    /// <summary>
    /// 是否已完成
    /// </summary>
    public bool IsCompleted { get; set; }
    
    /// <summary>
    /// 最终结果
    /// </summary>
    public string? FinalResult { get; set; }
    
    /// <summary>
    /// 错误信息
    /// </summary>
    public string? Error { get; set; }
    
    /// <summary>
    /// LLM使用情况统计
    /// </summary>
    public LlmUsage? Usage { get; set; }
}