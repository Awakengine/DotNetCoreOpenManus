using OpenManus.Host.Models;
using System.Text.Json;

namespace OpenManus.Host.Services;

/// <summary>
/// 聊天服务类，负责管理聊天消息的存储和检索
/// </summary>
public class ChatService
{
    /// <summary>
    /// 数据存储路径
    /// </summary>
    private readonly string _dataPath;
    
    /// <summary>
    /// 内存中的会话缓存
    /// </summary>
    private readonly Dictionary<string, List<ChatMessage>> _sessions = new();

    /// <summary>
    /// 构造函数，初始化聊天服务
    /// </summary>
    public ChatService()
    {
        _dataPath = Path.Combine(Directory.GetCurrentDirectory(), "Data");
        if (!Directory.Exists(_dataPath))
        {
            Directory.CreateDirectory(_dataPath);
        }
    }

    /// <summary>
    /// 获取指定会话的消息列表
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <returns>消息列表</returns>
    public async Task<List<ChatMessage>> GetMessagesAsync(string sessionId)
    {
        if (_sessions.ContainsKey(sessionId))
        {
            return _sessions[sessionId];
        }

        var filePath = Path.Combine(_dataPath, $"{sessionId}.json");
        if (File.Exists(filePath))
        {
            var json = await File.ReadAllTextAsync(filePath);
            var messages = JsonSerializer.Deserialize<List<ChatMessage>>(json) ?? new List<ChatMessage>();
            _sessions[sessionId] = messages;
            return messages;
        }

        _sessions[sessionId] = new List<ChatMessage>();
        return _sessions[sessionId];
    }

    /// <summary>
    /// 添加消息到指定会话
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="message">要添加的消息</param>
    public async Task AddMessageAsync(string sessionId, ChatMessage message)
    {
        if (!_sessions.ContainsKey(sessionId))
        {
            _sessions[sessionId] = new List<ChatMessage>();
        }

        message.SessionId = sessionId;
        _sessions[sessionId].Add(message);

        await SaveSessionAsync(sessionId);
    }

    /// <summary>
    /// 清除指定会话的所有消息
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    public async Task ClearMessagesAsync(string sessionId)
    {
        if (_sessions.ContainsKey(sessionId))
        {
            _sessions[sessionId].Clear();
        }

        var filePath = Path.Combine(_dataPath, $"{sessionId}.json");
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    /// <summary>
    /// 保存会话到文件
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    private async Task SaveSessionAsync(string sessionId)
    {
        if (!_sessions.ContainsKey(sessionId))
        {
            return;
        }

        var filePath = Path.Combine(_dataPath, $"{sessionId}.json");
        var json = JsonSerializer.Serialize(_sessions[sessionId], new JsonSerializerOptions
        {
            WriteIndented = true
        });
        
        await File.WriteAllTextAsync(filePath, json);
    }
}

