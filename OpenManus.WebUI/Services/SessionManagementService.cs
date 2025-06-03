using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using OpenManus.WebUI.Models;

namespace OpenManus.WebUI.Services;

/// <summary>
/// 会话管理服务类，负责聊天会话的创建、更新、删除和查询
/// </summary>
public class SessionManagementService
{
    /// <summary>
    /// 数据存储路径
    /// </summary>
    private readonly string _dataPath;
    
    /// <summary>
    /// 会话信息文件路径
    /// </summary>
    private readonly string _sessionsFilePath;
    
    /// <summary>
    /// 会话信息列表
    /// </summary>
    private List<ChatSessionInfo> _sessions = new();

    /// <summary>
    /// 构造函数，初始化会话管理服务
    /// </summary>
    public SessionManagementService()
    {
        _dataPath = Path.Combine(Directory.GetCurrentDirectory(), "Data");
        _sessionsFilePath = Path.Combine(_dataPath, "sessions.json");

        if (!Directory.Exists(_dataPath))
        {
            Directory.CreateDirectory(_dataPath);
        }

        LoadSessions();
    }

    /// <summary>
    /// 获取所有会话信息，按最后活动时间降序排列
    /// </summary>
    /// <returns>会话信息列表</returns>
    public async Task<List<ChatSessionInfo>> GetSessionsAsync()
    {
        try
        {
            return _sessions.OrderByDescending(s => s.LastActivity).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"获取会话失败: {ex.Message}");
            return new List<ChatSessionInfo>();
        }
        finally
        {
            await Task.Delay(1);
        }
    }

    /// <summary>
    /// 创建新的聊天会话
    /// </summary>
    /// <param name="title">会话标题，如果为空则自动生成</param>
    /// <returns>新创建的会话信息</returns>
    public async Task<ChatSessionInfo> CreateNewSessionAsync(string? title = null)
    {
        var newSession = new ChatSessionInfo
        {
            Id = Guid.NewGuid().ToString(),
            Title = title ?? $"新对话 {_sessions.Count + 1}",
            LastActivity = DateTime.Now,
            MessageCount = 0,
            CreatedAt = DateTime.Now
        };

        _sessions.Insert(0, newSession);
        await SaveSessionsAsync();

        return newSession;
    }

    /// <summary>
    /// 更新会话信息
    /// </summary>
    /// <param name="session">要更新的会话信息</param>
    public async Task UpdateSessionAsync(ChatSessionInfo session)
    {
        var existingSession = _sessions.FirstOrDefault(s => s.Id == session.Id);
        if (existingSession != null)
        {
            existingSession.Title = session.Title;
            existingSession.LastActivity = session.LastActivity;
            existingSession.MessageCount = session.MessageCount;

            await SaveSessionsAsync();
        }
    }

    /// <summary>
    /// 删除指定的会话及其相关数据
    /// </summary>
    /// <param name="sessionId">要删除的会话ID</param>
    public async Task DeleteSessionAsync(string sessionId)
    {
        _sessions.RemoveAll(s => s.Id == sessionId);
        await SaveSessionsAsync();

        // 同时删除对话消息文件
        var messageFilePath = Path.Combine(_dataPath, $"{sessionId}.json");
        if (File.Exists(messageFilePath))
        {
            File.Delete(messageFilePath);
        }
    }

    /// <summary>
    /// 根据会话ID获取会话信息
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <returns>会话信息，如果不存在则返回null</returns>
    public async Task<ChatSessionInfo?> GetSessionAsync(string sessionId)
    {
        await Task.Delay(1);

        return _sessions.FirstOrDefault(s => s.Id == sessionId);
    }

    /// <summary>
    /// 增加指定会话的消息计数并更新最后活动时间
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    public async Task IncrementMessageCountAsync(string sessionId)
    {
        var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
        if (session != null)
        {
            session.MessageCount++;
            session.LastActivity = DateTime.Now;
            await SaveSessionsAsync();
        }
    }

    /// <summary>
    /// 从文件加载会话信息
    /// </summary>
    private void LoadSessions()
    {
        try
        {
            if (File.Exists(_sessionsFilePath))
            {
                var json = File.ReadAllText(_sessionsFilePath);
                _sessions = JsonSerializer.Deserialize<List<ChatSessionInfo>>(json) ?? new List<ChatSessionInfo>();
            }
            else
            {
                // 创建默认会话
                _sessions = new List<ChatSessionInfo>
                {
                    new ChatSessionInfo
                    {
                        Id = "default-session",
                        Title = "当前对话",
                        LastActivity = DateTime.Now,
                        MessageCount = 0,
                        CreatedAt = DateTime.Now
                    }
                };
                // 同步保存初始会话数据
                var json = JsonSerializer.Serialize(_sessions, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_sessionsFilePath, json);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载会话失败: {ex.Message}");
            _sessions = new List<ChatSessionInfo>();
        }
    }

    /// <summary>
    /// 异步保存会话信息到文件
    /// </summary>
    private async Task SaveSessionsAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(_sessions, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_sessionsFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"保存会话失败: {ex.Message}");
        }
    }
}