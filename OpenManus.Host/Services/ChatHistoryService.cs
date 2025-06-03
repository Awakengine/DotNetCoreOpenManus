using OpenManus.Host.Models;
using System.Text.Json;
using System.Collections.Concurrent;

namespace OpenManus.Host.Services;

/// <summary>
/// 聊天历史服务实现类，负责管理对话记录的内存缓存和持久化存储
/// </summary>
public class ChatHistoryService : IChatHistoryService
{
    /// <summary>
    /// 数据存储路径
    /// </summary>
    private readonly string _dataPath;
    
    /// <summary>
    /// 内存中的会话缓存，使用线程安全的字典
    /// </summary>
    private readonly ConcurrentDictionary<string, AgentMemory> _sessionCache = new();
    
    /// <summary>
    /// 后台保存任务队列
    /// </summary>
    private readonly ConcurrentQueue<(string sessionId, AgentMemory memory)> _saveQueue = new();
    
    /// <summary>
    /// 后台保存服务的取消令牌
    /// </summary>
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    
    /// <summary>
    /// 后台保存任务
    /// </summary>
    private readonly Task _backgroundSaveTask;
    
    /// <summary>
    /// JSON序列化选项
    /// </summary>
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// 构造函数，初始化聊天历史服务
    /// </summary>
    public ChatHistoryService()
    {
        _dataPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "ChatHistory");
        if (!Directory.Exists(_dataPath))
        {
            Directory.CreateDirectory(_dataPath);
        }
        
        // 启动后台保存任务
        _backgroundSaveTask = Task.Run(BackgroundSaveWorker, _cancellationTokenSource.Token);
    }

    /// <summary>
    /// 获取指定会话的代理内存
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <returns>代理内存对象</returns>
    public async Task<AgentMemory> GetSessionMemoryAsync(string sessionId)
    {
        // 首先检查内存缓存
        if (_sessionCache.TryGetValue(sessionId, out var cachedMemory))
        {
            return cachedMemory;
        }

        // 从文件加载
        var filePath = GetSessionFilePath(sessionId);
        if (File.Exists(filePath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                var memory = JsonSerializer.Deserialize<AgentMemory>(json, _jsonOptions) ?? new AgentMemory();
                
                // 添加到缓存
                _sessionCache.TryAdd(sessionId, memory);
                return memory;
            }
            catch (Exception ex)
            {
                // 如果文件损坏，创建新的内存对象
                Console.WriteLine($"Error loading session {sessionId}: {ex.Message}");
            }
        }

        // 创建新的会话内存
        var newMemory = new AgentMemory();
        _sessionCache.TryAdd(sessionId, newMemory);
        return newMemory;
    }
    
    /// <summary>
    /// 保存会话内存到持久化存储
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="memory">代理内存对象</param>
    /// <returns>异步任务</returns>
    public async Task SaveSessionMemoryAsync(string sessionId, AgentMemory memory)
    {
        // 更新缓存
        _sessionCache.AddOrUpdate(sessionId, memory, (key, oldValue) => memory);
        
        // 添加到后台保存队列
        _saveQueue.Enqueue((sessionId, memory));
        
        // 对于重要操作，也可以立即保存
        await SaveSessionToFileAsync(sessionId, memory);
    }
    
    /// <summary>
    /// 添加消息到会话并实时保存
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="message">要添加的消息</param>
    /// <returns>异步任务</returns>
    public async Task AddMessageAsync(string sessionId, AgentMessage message)
    {
        var memory = await GetSessionMemoryAsync(sessionId);
        memory.AddMessage(message.Role, message.Content, message.ToolCallId);
        
        // 实时保存到后台队列
        _saveQueue.Enqueue((sessionId, memory));
    }
    
    /// <summary>
    /// 清除指定会话的所有消息
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <returns>异步任务</returns>
    public async Task ClearSessionAsync(string sessionId)
    {
        // 清除缓存
        if (_sessionCache.TryRemove(sessionId, out _))
        {
            // 删除文件
            var filePath = GetSessionFilePath(sessionId);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        
        await Task.CompletedTask;
    }
    
    /// <summary>
    /// 获取所有会话的基本信息
    /// </summary>
    /// <returns>会话信息列表</returns>
    public async Task<List<ChatSessionInfo>> GetSessionsAsync()
    {
        var sessions = new List<ChatSessionInfo>();
        
        // 扫描数据目录中的所有会话文件
        var files = Directory.GetFiles(_dataPath, "*.json");
        
        foreach (var file in files)
        {
            try
            {
                var sessionId = Path.GetFileNameWithoutExtension(file);
                var fileInfo = new System.IO.FileInfo(file);
                
                // 尝试从缓存获取或加载文件
                var memory = await GetSessionMemoryAsync(sessionId);
                
                var sessionInfo = new ChatSessionInfo
                {
                    Id = sessionId,
                    Title = GenerateSessionTitle(memory),
                    LastActivity = fileInfo.LastWriteTime,
                    MessageCount = memory.Messages.Count(m => m.Role != "system"),
                    CreatedAt = fileInfo.CreationTime
                };
                
                sessions.Add(sessionInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading session info from {file}: {ex.Message}");
            }
        }
        
        return sessions.OrderByDescending(s => s.LastActivity).ToList();
    }
    
    /// <summary>
    /// 删除指定会话
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <returns>异步任务</returns>
    public async Task DeleteSessionAsync(string sessionId)
    {
        await ClearSessionAsync(sessionId);
    }
    
    /// <summary>
    /// 获取所有会话的详细信息（包含消息内容）
    /// </summary>
    /// <returns>会话ID和代理内存的键值对字典</returns>
    public async Task<Dictionary<string, AgentMemory>> GetAllSessionsAsync()
    {
        var result = new Dictionary<string, AgentMemory>();
        
        if (!Directory.Exists(_dataPath))
        {
            return result;
        }
        
        var files = Directory.GetFiles(_dataPath, "*.json");
        foreach (var file in files)
        {
            try
            {
                var sessionId = Path.GetFileNameWithoutExtension(file);
                var memory = await GetSessionMemoryAsync(sessionId);
                if (memory != null)
                {
                    result[sessionId] = memory;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading session from {file}: {ex.Message}");
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// 获取会话文件路径
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <returns>文件路径</returns>
    private string GetSessionFilePath(string sessionId)
    {
        // 清理会话ID中的非法字符
        var cleanSessionId = string.Join("", sessionId.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_dataPath, $"{cleanSessionId}.json");
    }
    
    /// <summary>
    /// 保存会话到文件
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="memory">代理内存</param>
    /// <returns>异步任务</returns>
    private async Task SaveSessionToFileAsync(string sessionId, AgentMemory memory)
    {
        try
        {
            var filePath = GetSessionFilePath(sessionId);
            var json = JsonSerializer.Serialize(memory, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving session {sessionId}: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 后台保存工作线程
    /// </summary>
    /// <returns>异步任务</returns>
    private async Task BackgroundSaveWorker()
    {
        var processedSessions = new HashSet<string>();
        
        while (!_cancellationTokenSource.Token.IsCancellationRequested)
        {
            try
            {
                // 处理队列中的保存请求
                while (_saveQueue.TryDequeue(out var saveRequest))
                {
                    processedSessions.Add(saveRequest.sessionId);
                }
                
                // 批量保存已处理的会话
                foreach (var sessionId in processedSessions)
                {
                    if (_sessionCache.TryGetValue(sessionId, out var memory))
                    {
                        await SaveSessionToFileAsync(sessionId, memory);
                    }
                }
                
                processedSessions.Clear();
                
                // 等待一段时间再处理下一批
                await Task.Delay(2000, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Background save worker error: {ex.Message}");
                await Task.Delay(5000, _cancellationTokenSource.Token);
            }
        }
    }
    
    /// <summary>
    /// 生成会话标题
    /// </summary>
    /// <param name="memory">代理内存</param>
    /// <returns>会话标题</returns>
    private string GenerateSessionTitle(AgentMemory memory)
    {
        var userMessages = memory.Messages.Where(m => m.Role == "user").ToList();
        if (userMessages.Any())
        {
            var firstMessage = userMessages.First().Content;
            // 截取前30个字符作为标题
            return firstMessage.Length > 30 ? firstMessage.Substring(0, 30) + "..." : firstMessage;
        }
        
        return "新对话";
    }
    
    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        try
        {
            _backgroundSaveTask.Wait(5000);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error disposing ChatHistoryService: {ex.Message}");
        }
        finally
        {
            _cancellationTokenSource.Dispose();
        }
    }
}