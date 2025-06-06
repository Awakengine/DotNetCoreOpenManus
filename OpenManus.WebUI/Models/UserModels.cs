using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OpenManus.WebUI.Models;

/// <summary>
/// 用户类型枚举
/// </summary>
public enum UserType
{
    /// <summary>
    /// 游客用户
    /// </summary>
    Guest,
    
    /// <summary>
    /// 注册用户
    /// </summary>
    Registered
}

/// <summary>
/// 用户信息类
/// </summary>
public class UserInfo
{
    /// <summary>
    /// 用户唯一标识符
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 用户类型
    /// </summary>
    public UserType Type { get; set; } = UserType.Guest;
    
    /// <summary>
    /// 用户头像URL或图标
    /// </summary>
    public string Avatar { get; set; } = "fas fa-user";
    
    /// <summary>
    /// 用户状态
    /// </summary>
    public string Status { get; set; } = "在线";
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 最后活动时间
    /// </summary>
    public DateTime LastActivity { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 用户会话列表
    /// </summary>
    public List<string> SessionIds { get; set; } = new();
    
    /// <summary>
    /// 用户设置
    /// </summary>
    public UserSettings Settings { get; set; } = new();
}

/// <summary>
/// 用户设置类
/// </summary>
public class UserSettings
{
    /// <summary>
    /// 主题设置
    /// </summary>
    public string Theme { get; set; } = "light";
    
    /// <summary>
    /// 语言设置
    /// </summary>
    public string Language { get; set; } = "zh-CN";
    
    /// <summary>
    /// 是否启用通知
    /// </summary>
    public bool EnableNotifications { get; set; } = true;
    
    /// <summary>
    /// 自动保存间隔（秒）
    /// </summary>
    public int AutoSaveInterval { get; set; } = 30;
    
    /// <summary>
    /// 最大会话数量
    /// </summary>
    public int MaxSessions { get; set; } = 50;
}

/// <summary>
/// 用户会话关联类
/// </summary>
public class UserSession
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// 会话ID
    /// </summary>
    public string SessionId { get; set; } = string.Empty;
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 最后访问时间
    /// </summary>
    public DateTime LastAccessed { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// 是否为活跃会话
    /// </summary>
    public bool IsActive { get; set; } = true;
}