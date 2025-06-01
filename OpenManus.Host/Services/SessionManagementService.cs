using System.Text.Json;
using OpenManus.Host.Models;

namespace OpenManus.Host.Services;

public class SessionManagementService
{
    private readonly string _dataPath;
    private readonly string _sessionsFilePath;
    private List<ChatSessionInfo> _sessions = new();

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

    public async Task<List<ChatSessionInfo>> GetSessionsAsync()
    {
        return _sessions.OrderByDescending(s => s.LastActivity).ToList();
    }

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

    public async Task<ChatSessionInfo?> GetSessionAsync(string sessionId)
    {
        return _sessions.FirstOrDefault(s => s.Id == sessionId);
    }

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

public class ChatSessionInfo
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime LastActivity { get; set; }
    public int MessageCount { get; set; }
    public DateTime CreatedAt { get; set; }
}