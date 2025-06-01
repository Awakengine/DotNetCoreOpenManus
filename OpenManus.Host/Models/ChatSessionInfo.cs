namespace OpenManus.Host.Models;

/// <summary>
/// 聊天会话信息类
/// </summary>
public class ChatSessionInfo
{
    /// <summary>
    /// 会话唯一标识符
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// 会话标题
    /// </summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// 最后活动时间
    /// </summary>
    public DateTime LastActivity { get; set; }
    
    /// <summary>
    /// 消息数量
    /// </summary>
    public int MessageCount { get; set; }
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
}