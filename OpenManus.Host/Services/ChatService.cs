using OpenManus.Host.Models;
using System.Text.Json;

namespace OpenManus.Host.Services;

public class ChatService
{
    private readonly string _dataPath;
    private readonly Dictionary<string, List<ChatMessage>> _sessions = new();

    public ChatService()
    {
        _dataPath = Path.Combine(Directory.GetCurrentDirectory(), "Data");
        if (!Directory.Exists(_dataPath))
        {
            Directory.CreateDirectory(_dataPath);
        }
    }

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

