using OpenManus.WebUI.Models;
using System.Text.Json;
using System.Collections.Concurrent;

namespace OpenManus.WebUI.Services;

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
    /// 最大缓存会话数量，防止内存溢出
    /// </summary>
    private const int MaxCachedSessions = 100;

    /// <summary>
    /// 后台保存任务队列（包含用户ID用于数据隔离）
    /// </summary>
    private readonly ConcurrentQueue<(string sessionId, AgentMemory memory, string? userId)> _saveQueue = new();
    
    /// <summary>
    /// 批量保存的会话ID集合
    /// </summary>
    private readonly HashSet<string> _pendingSaves = new();
    
    /// <summary>
    /// 批量保存锁
    /// </summary>
    private readonly object _pendingSavesLock = new();

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
    /// 获取指定会话的代理内存（带用户隔离）
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="userId">用户ID，用于数据隔离</param>
    /// <returns>代理内存对象</returns>
    public async Task<AgentMemory> GetSessionMemoryAsync(string sessionId, string? userId = null)
    {
        // 首先检查内存缓存
        if (_sessionCache.TryGetValue(sessionId, out var cachedMemory))
        {
            return cachedMemory;
        }

        // 从文件加载（考虑用户隔离）
        var filePath = GetSessionFilePath(sessionId, userId);
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
        
        // 检查缓存大小，如果超过限制则清理最旧的会话
        if (_sessionCache.Count >= MaxCachedSessions)
        {
            CleanupOldSessions();
        }
        
        _sessionCache.TryAdd(sessionId, newMemory);
        return newMemory;
    }

    /// <summary>
    /// 保存会话内存到持久化存储（带用户隔离）
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="memory">代理内存对象</param>
    /// <param name="userId">用户ID，用于数据隔离</param>
    /// <returns>异步任务</returns>
    public async Task SaveSessionMemoryAsync(string sessionId, AgentMemory memory, string? userId = null)
    {
        // 更新缓存
        _sessionCache.AddOrUpdate(sessionId, memory, (key, oldValue) => memory);

        // 添加到后台保存队列（包含用户ID）
        _saveQueue.Enqueue((sessionId, memory, userId));

        // 对于重要操作，也可以立即保存
        await SaveSessionToFileAsync(sessionId, memory, userId);
    }

    /// <summary>
    /// 异步保存会话到文件
    /// </summary>
    public Task SaveSessionAsync(string sessionId, AgentMemory memory)
    {
        try
        {
            // 检查是否已经在待保存列表中，避免重复保存
            lock (_pendingSavesLock)
            {
                if (_pendingSaves.Contains(sessionId))
                {
                    return Task.CompletedTask; // 已经在队列中，跳过
                }
                _pendingSaves.Add(sessionId);
            }
            
            // 添加到后台保存队列（包含用户ID）
            _saveQueue.Enqueue((sessionId, memory, null));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error queuing session save: {ex.Message}");
            
            // 如果出错，从待保存列表中移除
            lock (_pendingSavesLock)
            {
                _pendingSaves.Remove(sessionId);
            }
        }
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// 添加消息到会话并实时保存（支持用户隔离）
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="message">要添加的消息</param>
    /// <param name="userId">用户ID，用于数据隔离</param>
    /// <returns>更新后的代理内存</returns>
    public async Task<AgentMemory> AddMessageAsync(string sessionId, AgentMessage message, string? userId = null)
    {
        var memory = await GetSessionMemoryAsync(sessionId, userId);
        memory.AddMessage(message.OrderBy, message.Role, message.Content, message.ToolCallId);

        // 立即保存到内存缓存
        _sessionCache.AddOrUpdate(sessionId, memory, (key, oldValue) => memory);
        
        // 异步保存到文件（包含用户ID）
        await SaveSessionMemoryAsync(sessionId, memory, userId);
        
        return memory;
    }

    /// <summary>
    /// 清除指定会话的所有消息（带用户隔离）
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="userId">用户ID，用于数据隔离</param>
    /// <returns>异步任务</returns>
    public async Task ClearSessionAsync(string sessionId, string? userId = null)
    {
        // 清除缓存
        if (_sessionCache.TryRemove(sessionId, out _))
        {
            // 删除文件（考虑用户隔离）
            var filePath = GetSessionFilePath(sessionId, userId);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 获取所有会话的基本信息（支持用户隔离）
    /// </summary>
    /// <param name="userId">用户ID，用于数据隔离</param>
    /// <returns>会话信息列表</returns>
    public async Task<List<ChatSessionInfo>> GetSessionsAsync(string? userId = null)
    {
        var sessions = new List<ChatSessionInfo>();
        
        try
        {
            string dataPath;
             if (!string.IsNullOrEmpty(userId))
             {
                 // 用户特定的数据目录
                 dataPath = Path.Combine(_dataPath, "Users", userId);
             }
             else
             {
                 // 默认数据目录（兼容旧版本）
                 dataPath = _dataPath;
             }
            
            // 确保数据目录存在
            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);
                return sessions;
            }
            
            // 扫描数据目录中的所有会话文件
            var files = Directory.GetFiles(dataPath, "*.json");

            foreach (var file in files)
            {
                try
                {
                    var sessionId = Path.GetFileNameWithoutExtension(file);
                    var fileInfo = new System.IO.FileInfo(file);

                    // 尝试从缓存获取或加载文件
                    var memory = await GetSessionMemoryAsync(sessionId, userId);

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
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error scanning sessions: {ex.Message}");
        }

        return sessions.OrderByDescending(s => s.LastActivity).ToList();
    }

    /// <summary>
    /// 删除指定会话（带用户隔离）
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="userId">用户ID，用于数据隔离</param>
    /// <returns>异步任务</returns>
    public async Task DeleteSessionAsync(string sessionId, string? userId = null)
    {
        await ClearSessionAsync(sessionId, userId);
    }

    /// <summary>
    /// 获取所有会话的详细信息（包含消息内容，支持用户隔离）
    /// </summary>
    /// <param name="userId">用户ID，用于数据隔离</param>
    /// <returns>会话ID和代理内存的键值对字典</returns>
    public async Task<Dictionary<string, AgentMemory>> GetAllSessionsAsync(string? userId = null)
    {
        var result = new Dictionary<string, AgentMemory>();

        string dataPath;
        if (!string.IsNullOrEmpty(userId))
        {
            // 用户特定的数据目录
            dataPath = Path.Combine(_dataPath, "Users", userId);
        }
        else
        {
            // 默认数据目录（兼容旧版本）
            dataPath = _dataPath;
        }

        if (!Directory.Exists(dataPath))
        {
            return result;
        }

        var files = Directory.GetFiles(dataPath, "*.json");
        foreach (var file in files)
        {
            try
            {
                var sessionId = Path.GetFileNameWithoutExtension(file);
                var memory = await GetSessionMemoryAsync(sessionId, userId);
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
    /// 获取会话文件路径（支持用户隔离）
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="userId">用户ID，用于数据隔离</param>
    /// <returns>文件路径</returns>
    private string GetSessionFilePath(string sessionId, string? userId = null)
    {
        // 清理会话ID中的非法字符
        var cleanSessionId = string.Join("", sessionId.Split(Path.GetInvalidFileNameChars()));
        
        if (!string.IsNullOrEmpty(userId))
        {
            // 为每个用户创建独立的子目录
            var userDataPath = Path.Combine(_dataPath, userId);
            if (!Directory.Exists(userDataPath))
            {
                Directory.CreateDirectory(userDataPath);
            }
            return Path.Combine(userDataPath, $"{cleanSessionId}.json");
        }
        
        // 兼容旧版本，没有用户ID时使用原路径
        return Path.Combine(_dataPath, $"{cleanSessionId}.json");
    }

    /// <summary>
    /// 保存会话到文件（支持用户隔离）
    /// </summary>
    /// <param name="sessionId">会话ID</param>
    /// <param name="memory">代理内存</param>
    /// <param name="userId">用户ID，用于数据隔离</param>
    /// <returns>异步任务</returns>
    private async Task SaveSessionToFileAsync(string sessionId, AgentMemory memory, string? userId = null)
    {
        try
        {
            var filePath = GetSessionFilePath(sessionId, userId);
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
                var saveRequests = new List<(string sessionId, AgentMemory memory, string? userId)>();
                while (_saveQueue.TryDequeue(out var saveRequest))
                {
                    saveRequests.Add(saveRequest);
                }

                // 批量保存已处理的会话
                if (saveRequests.Count > 0)
                {
                    var saveTasks = new List<Task>();
                    
                    foreach (var request in saveRequests)
                    {
                        if (_sessionCache.TryGetValue(request.sessionId, out var memory))
                        {
                            // 并行保存以提高性能（包含用户ID）
                            saveTasks.Add(SaveSessionToFileAsync(request.sessionId, memory, request.userId));
                        }
                        
                        // 从待保存列表中移除
                        lock (_pendingSavesLock)
                        {
                            _pendingSaves.Remove(request.sessionId);
                        }
                    }
                    
                    // 等待所有保存任务完成
                    await Task.WhenAll(saveTasks);
                    saveRequests.Clear();
                }

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
    /// 清理旧的缓存会话，保留最近使用的会话
    /// </summary>
    private void CleanupOldSessions()
    {
        try
        {
            // 移除超出限制的最旧会话（简单的FIFO策略）
            var sessionsToRemove = _sessionCache.Count - MaxCachedSessions + 10; // 多清理10个以减少频繁清理
            var keysToRemove = _sessionCache.Keys.Take(sessionsToRemove).ToList();
            
            foreach (var key in keysToRemove)
            {
                _sessionCache.TryRemove(key, out _);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error cleaning up old sessions: {ex.Message}");
        }
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        _cancellationTokenSource.Cancel();
        try
        {
            // 使用异步等待避免阻塞，设置合理的超时时间
            if (!_backgroundSaveTask.Wait(TimeSpan.FromSeconds(5)))
            {
                Console.WriteLine("Background save task did not complete within timeout");
            }
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