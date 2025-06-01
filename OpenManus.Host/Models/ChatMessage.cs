namespace OpenManus.Host.Models;

/// <summary>
/// 聊天消息类
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// 消息唯一标识符
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// 消息内容
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// 是否为用户消息
    /// </summary>
    public bool IsUser { get; set; }
    
    /// <summary>
    /// 消息时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 所属会话ID
    /// </summary>
    public string SessionId { get; set; } = string.Empty;
}

