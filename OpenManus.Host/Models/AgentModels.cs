using System.Text.Json.Serialization;

namespace OpenManus.Host.Models;

public enum AgentState
{
    Idle,
    Running,
    Finished,
    Error
}

public class AgentMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? ToolCallId { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public class AgentMemory
{
    public List<AgentMessage> Messages { get; set; } = new();
    
    public void AddMessage(string role, string content, string? toolCallId = null)
    {
        Messages.Add(new AgentMessage
        {
            Role = role,
            Content = content,
            ToolCallId = toolCallId
        });
    }
    
    public void Clear()
    {
        Messages.Clear();
    }
}

public class ToolCall
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object> Arguments { get; set; } = new();
}

public class ToolResult
{
    public string ToolCallId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsSuccess { get; set; } = true;
    public string? Error { get; set; }
}

public class AgentResponse
{
    public string Content { get; set; } = string.Empty;
    public List<ToolCall> ToolCalls { get; set; } = new();
    public bool IsFinished { get; set; }
    public string? Error { get; set; }
}

public class AgentExecutionResult
{
    public string SessionId { get; set; } = string.Empty;
    public List<string> Steps { get; set; } = new();
    public bool IsCompleted { get; set; }
    public string? FinalResult { get; set; }
    public string? Error { get; set; }
}