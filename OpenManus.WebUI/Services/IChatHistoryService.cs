using OpenManus.WebUI.Models;

namespace OpenManus.WebUI.Services;

/// <summary>
/// 聊天历史服务接口，负责管理对话记录的内存缓存和持久化存储
/// </summary>
public interface IChatHistoryService
{
    /// <summary>
    /// 获取指定会话的代理内存（带用户隔离）
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="userId">用户ID，用于数据隔离</param>
    /// <returns>代理内存对象</returns>
    Task<AgentMemory> GetSessionMemoryAsync(string sessionId, string? userId = null);
    
    /// <summary>
    /// 保存会话内存到持久化存储（带用户隔离）
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="memory">代理内存对象</param>
    /// <param name="userId">用户ID，用于数据隔离</param>
    /// <returns>异步任务</returns>
    Task SaveSessionMemoryAsync(string sessionId, AgentMemory memory, string? userId = null);
    
    /// <summary>
    /// 添加消息到会话并实时保存（带用户隔离）
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="message">要添加的消息</param>
    /// <param name="userId">用户ID，用于数据隔离</param>
    /// <returns>异步任务</returns>
    Task<AgentMemory> AddMessageAsync(string sessionId, AgentMessage message, string? userId = null);
    
    /// <summary>
    /// 删除指定会话的所有数据（带用户隔离）
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="userId">用户ID，用于数据隔离</param>
    /// <returns>异步任务</returns>
    Task DeleteSessionAsync(string sessionId, string? userId = null);
    
    /// <summary>
    /// 清空指定会话的所有数据（带用户隔离）
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="userId">用户ID，用于数据隔离</param>
    /// <returns>异步任务</returns>
    Task ClearSessionAsync(string sessionId, string? userId = null);
    
    /// <summary>
    /// 获取所有会话的基本信息（带用户隔离）
    /// </summary>
    /// <param name="userId">用户ID，用于数据隔离</param>
    /// <returns>会话信息列表</returns>
    Task<List<ChatSessionInfo>> GetSessionsAsync(string? userId = null);
    
    /// <summary>
    /// 获取所有会话的详细信息（包含消息内容，带用户隔离）
    /// </summary>
    /// <param name="userId">用户ID，用于数据隔离</param>
    /// <returns>会话ID和代理内存的键值对字典</returns>
    Task<Dictionary<string, AgentMemory>> GetAllSessionsAsync(string? userId = null);
}