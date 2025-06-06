using System.Text.Json;
using System.Collections.Concurrent;
using OpenManus.WebUI.Models;

namespace OpenManus.WebUI.Services;

/// <summary>
/// 用户服务实现类
/// 负责用户管理、会话隔离和用户数据持久化
/// </summary>
public class UserService : IUserService
{
    /// <summary>
    /// 数据存储路径
    /// </summary>
    private readonly string _dataPath;
    
    /// <summary>
    /// 用户数据文件路径
    /// </summary>
    private readonly string _usersFilePath;
    
    /// <summary>
    /// 用户会话关联文件路径
    /// </summary>
    private readonly string _userSessionsFilePath;
    
    /// <summary>
    /// 内存中的用户缓存
    /// </summary>
    private readonly ConcurrentDictionary<string, UserInfo> _userCache = new();
    
    /// <summary>
    /// 内存中的用户会话关联缓存
    /// </summary>
    private readonly ConcurrentDictionary<string, List<UserSession>> _userSessionCache = new();
    
    /// <summary>
    /// 当前用户ID（基于浏览器会话）
    /// </summary>
    private string? _currentUserId;
    
    /// <summary>
    /// 会话管理服务
    /// </summary>
    private readonly SessionManagementService _sessionManagementService;
    
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
    /// 构造函数
    /// </summary>
    /// <param name="sessionManagementService">会话管理服务</param>
    public UserService(SessionManagementService sessionManagementService)
    {
        _sessionManagementService = sessionManagementService;
        _dataPath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Users");
        _usersFilePath = Path.Combine(_dataPath, "users.json");
        _userSessionsFilePath = Path.Combine(_dataPath, "user-sessions.json");
        
        if (!Directory.Exists(_dataPath))
        {
            Directory.CreateDirectory(_dataPath);
        }
        
        LoadUsersFromFile();
        LoadUserSessionsFromFile();
    }

    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    public async Task<UserInfo> GetCurrentUserAsync()
    {
        if (string.IsNullOrEmpty(_currentUserId))
        {
            // 如果没有当前用户，创建一个新的游客用户
            var guestUser = await CreateGuestUserAsync();
            _currentUserId = guestUser.Id;
            return guestUser;
        }
        
        var user = await GetUserByIdAsync(_currentUserId);
        if (user == null)
        {
            // 如果用户不存在，创建一个新的游客用户
            var guestUser = await CreateGuestUserAsync();
            _currentUserId = guestUser.Id;
            return guestUser;
        }
        
        return user;
    }

    /// <summary>
    /// 根据用户ID获取用户信息
    /// </summary>
    public async Task<UserInfo?> GetUserByIdAsync(string userId)
    {
        await Task.Delay(1); // 异步方法占位
        
        if (_userCache.TryGetValue(userId, out var user))
        {
            return user;
        }
        
        return null;
    }

    /// <summary>
    /// 创建游客用户
    /// </summary>
    public async Task<UserInfo> CreateGuestUserAsync(string? name = null)
    {
        var userId = Guid.NewGuid().ToString();
        var userName = name ?? $"游客_{DateTime.Now:MMddHHmm}";
        
        var userInfo = new UserInfo
        {
            Id = userId,
            Name = userName,
            Type = UserType.Guest,
            Avatar = "fas fa-user-secret",
            Status = "在线",
            CreatedAt = DateTime.UtcNow,
            LastActivity = DateTime.UtcNow
        };
        
        _userCache.TryAdd(userId, userInfo);
        await SaveUsersToFileAsync();
        
        return userInfo;
    }

    /// <summary>
    /// 创建注册用户
    /// </summary>
    public async Task<UserInfo> CreateRegisteredUserAsync(string name, string avatar)
    {
        var userId = Guid.NewGuid().ToString();
        
        var userInfo = new UserInfo
        {
            Id = userId,
            Name = name,
            Type = UserType.Registered,
            Avatar = string.IsNullOrEmpty(avatar) ? "fas fa-user" : avatar,
            Status = "在线",
            CreatedAt = DateTime.UtcNow,
            LastActivity = DateTime.UtcNow
        };
        
        _userCache.TryAdd(userId, userInfo);
        await SaveUsersToFileAsync();
        
        return userInfo;
    }

    /// <summary>
    /// 更新用户信息
    /// </summary>
    public async Task<UserInfo> UpdateUserAsync(UserInfo userInfo)
    {
        userInfo.LastActivity = DateTime.UtcNow;
        _userCache.AddOrUpdate(userInfo.Id, userInfo, (key, oldValue) => userInfo);
        await SaveUsersToFileAsync();
        return userInfo;
    }

    /// <summary>
    /// 删除用户
    /// </summary>
    public async Task<bool> DeleteUserAsync(string userId)
    {
        var removed = _userCache.TryRemove(userId, out _);
        if (removed)
        {
            // 同时删除用户的所有会话关联
            _userSessionCache.TryRemove(userId, out _);
            await SaveUsersToFileAsync();
            await SaveUserSessionsToFileAsync();
        }
        return removed;
    }

    /// <summary>
    /// 获取用户的所有会话
    /// </summary>
    public async Task<List<ChatSessionInfo>> GetUserSessionsAsync(string userId)
    {
        var userSessions = new List<ChatSessionInfo>();
        
        if (_userSessionCache.TryGetValue(userId, out var sessions))
        {
            var allSessions = await _sessionManagementService.GetSessionsAsync();
            
            foreach (var userSession in sessions.Where(s => s.IsActive))
            {
                var session = allSessions.FirstOrDefault(s => s.Id == userSession.SessionId);
                if (session != null)
                {
                    userSessions.Add(session);
                }
            }
        }
        
        return userSessions.OrderByDescending(s => s.LastActivity).ToList();
    }

    /// <summary>
    /// 为用户创建新会话
    /// </summary>
    public async Task<ChatSessionInfo> CreateUserSessionAsync(string userId, string? title = null)
    {
        // 创建新会话
        var session = await _sessionManagementService.CreateNewSessionAsync(title);
        
        // 创建用户会话关联
        var userSession = new UserSession
        {
            UserId = userId,
            SessionId = session.Id,
            CreatedAt = DateTime.UtcNow,
            LastAccessed = DateTime.UtcNow,
            IsActive = true
        };
        
        // 添加到用户会话缓存
        _userSessionCache.AddOrUpdate(userId, 
            new List<UserSession> { userSession },
            (key, existingSessions) => 
            {
                existingSessions.Add(userSession);
                return existingSessions;
            });
        
        // 更新用户的会话列表
        if (_userCache.TryGetValue(userId, out var user))
        {
            user.SessionIds.Add(session.Id);
            await UpdateUserAsync(user);
        }
        
        await SaveUserSessionsToFileAsync();
        
        return session;
    }

    /// <summary>
    /// 删除用户会话
    /// </summary>
    public async Task<bool> DeleteUserSessionAsync(string userId, string sessionId)
    {
        var success = false;
        
        if (_userSessionCache.TryGetValue(userId, out var sessions))
        {
            var sessionToRemove = sessions.FirstOrDefault(s => s.SessionId == sessionId);
            if (sessionToRemove != null)
            {
                sessionToRemove.IsActive = false;
                success = true;
            }
        }
        
        // 从用户信息中移除会话ID
        if (_userCache.TryGetValue(userId, out var user))
        {
            user.SessionIds.Remove(sessionId);
            await UpdateUserAsync(user);
        }
        
        // 删除实际的会话
        await _sessionManagementService.DeleteSessionAsync(sessionId);
        
        if (success)
        {
            await SaveUserSessionsToFileAsync();
        }
        
        return success;
    }

    /// <summary>
    /// 验证用户是否有权限访问指定会话
    /// </summary>
    public async Task<bool> ValidateUserSessionAccessAsync(string userId, string sessionId)
    {
        await Task.Delay(1); // 异步方法占位
        
        if (_userSessionCache.TryGetValue(userId, out var sessions))
        {
            return sessions.Any(s => s.SessionId == sessionId && s.IsActive);
        }
        
        return false;
    }

    /// <summary>
    /// 更新用户活动时间
    /// </summary>
    public async Task UpdateUserActivityAsync(string userId)
    {
        if (_userCache.TryGetValue(userId, out var user))
        {
            user.LastActivity = DateTime.UtcNow;
            await UpdateUserAsync(user);
        }
    }

    /// <summary>
    /// 获取所有用户列表
    /// </summary>
    public async Task<List<UserInfo>> GetAllUsersAsync()
    {
        await Task.Delay(1); // 异步方法占位
        return _userCache.Values.ToList();
    }

    /// <summary>
    /// 清理过期的游客用户
    /// </summary>
    public async Task<int> CleanupExpiredGuestUsersAsync(int expireDays = 7)
    {
        var expireDate = DateTime.UtcNow.AddDays(-expireDays);
        var expiredUsers = _userCache.Values
            .Where(u => u.Type == UserType.Guest && u.LastActivity < expireDate)
            .ToList();
        
        var cleanedCount = 0;
        foreach (var user in expiredUsers)
        {
            if (await DeleteUserAsync(user.Id))
            {
                cleanedCount++;
            }
        }
        
        return cleanedCount;
    }

    /// <summary>
    /// 设置当前用户ID（用于会话管理）
    /// </summary>
    /// <param name="userId">用户ID</param>
    public async Task SetCurrentUserIdAsync(string userId)
    {
        _currentUserId = userId;
        // 可以在这里添加持久化逻辑，比如保存到本地存储
        await Task.CompletedTask;
    }
    
    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    /// <returns>当前用户ID</returns>
    public async Task<string?> GetCurrentUserIdAsync()
    {
        // 可以在这里添加从本地存储读取的逻辑
        await Task.CompletedTask;
        return _currentUserId;
    }
    
    /// <summary>
    /// 用户退出登录
    /// </summary>
    public async Task LogoutAsync()
    {
        _currentUserId = null;
        // 可以在这里添加清理本地存储的逻辑
        await Task.CompletedTask;
    }

    /// <summary>
    /// 从文件加载用户数据
    /// </summary>
    private void LoadUsersFromFile()
    {
        try
        {
            if (File.Exists(_usersFilePath))
            {
                var json = File.ReadAllText(_usersFilePath);
                var users = JsonSerializer.Deserialize<List<UserInfo>>(json, _jsonOptions) ?? new List<UserInfo>();
                
                foreach (var user in users)
                {
                    _userCache.TryAdd(user.Id, user);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载用户数据失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 保存用户数据到文件
    /// </summary>
    private async Task SaveUsersToFileAsync()
    {
        try
        {
            var users = _userCache.Values.ToList();
            var json = JsonSerializer.Serialize(users, _jsonOptions);
            await File.WriteAllTextAsync(_usersFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"保存用户数据失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 从文件加载用户会话关联数据
    /// </summary>
    private void LoadUserSessionsFromFile()
    {
        try
        {
            if (File.Exists(_userSessionsFilePath))
            {
                var json = File.ReadAllText(_userSessionsFilePath);
                var userSessionsDict = JsonSerializer.Deserialize<Dictionary<string, List<UserSession>>>(json, _jsonOptions) 
                    ?? new Dictionary<string, List<UserSession>>();
                
                foreach (var kvp in userSessionsDict)
                {
                    _userSessionCache.TryAdd(kvp.Key, kvp.Value);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载用户会话关联数据失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 保存用户会话关联数据到文件
    /// </summary>
    private async Task SaveUserSessionsToFileAsync()
    {
        try
        {
            var userSessionsDict = _userSessionCache.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            var json = JsonSerializer.Serialize(userSessionsDict, _jsonOptions);
            await File.WriteAllTextAsync(_userSessionsFilePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"保存用户会话关联数据失败: {ex.Message}");
        }
    }
}