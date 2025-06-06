using System.Text.Json;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using OpenManus.Web.Models;

namespace OpenManus.Web.Services;

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
    /// JWT服务
    /// </summary>
    private readonly IJwtService _jwtService;
    
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
    /// <param name="jwtService">JWT服务</param>
    public UserService(SessionManagementService sessionManagementService, IJwtService jwtService)
    {
        _sessionManagementService = sessionManagementService;
        _jwtService = jwtService;
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
    /// 创建带密码的注册用户
    /// </summary>
    public async Task<UserInfo> CreateRegisteredUserWithPasswordAsync(string name, string password, string avatar)
    {
        var userId = Guid.NewGuid().ToString();
        var salt = GenerateSalt();
        var passwordHash = ComputePasswordHash(password, salt);
        
        var userInfo = new UserInfo
        {
            Id = userId,
            Name = name,
            Type = UserType.Registered,
            Avatar = string.IsNullOrEmpty(avatar) ? "fas fa-user" : avatar,
            Status = "在线",
            CreatedAt = DateTime.UtcNow,
            LastActivity = DateTime.UtcNow,
            PasswordHash = passwordHash,
            PasswordSalt = salt
        };
        
        _userCache.TryAdd(userId, userInfo);
        await SaveUsersToFileAsync();
        
        return userInfo;
    }

    /// <summary>
    /// 验证用户登录并生成JWT令牌
    /// </summary>
    public async Task<(UserInfo? user, string? token)> LoginWithJwtAsync(string username, string password)
    {
        var user = await ValidateUserLoginWithPasswordAsync(username, password);
        if (user != null)
        {
            var token = _jwtService.GenerateToken(user);
            return (user, token);
        }
        return (null, null);
    }

    /// <summary>
    /// 通过JWT令牌获取当前用户
    /// </summary>
    public async Task<UserInfo?> GetUserByJwtTokenAsync(string token)
    {
        var userId = _jwtService.ValidateToken(token);
        if (!string.IsNullOrEmpty(userId))
        {
            return await GetUserByIdAsync(userId);
        }
        return null;
    }

    /// <summary>
    /// 验证用户登录（带密码）
    /// </summary>
    public async Task<UserInfo?> ValidateUserLoginWithPasswordAsync(string username, string password)
    {
        await Task.Delay(1);
        
        var user = _userCache.Values.FirstOrDefault(u => u.Name == username && u.Type == UserType.Registered);
        if (user != null && !string.IsNullOrEmpty(user.PasswordHash) && !string.IsNullOrEmpty(user.PasswordSalt))
        {
            if (VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
            {
                user.LastActivity = DateTime.UtcNow;
                await SaveUsersToFileAsync();
                return user;
            }
        }
        
        return null;
    }

    /// <summary>
    /// 根据用户名查找注册用户
    /// </summary>
    public async Task<UserInfo?> GetUserByNameAsync(string username)
    {
        await Task.Delay(1);
        return _userCache.Values.FirstOrDefault(u => u.Name == username && u.Type == UserType.Registered);
    }

    // 实现其他接口方法的简化版本
    public async Task<UserInfo> UpdateUserAsync(UserInfo userInfo)
    {
        _userCache.TryAdd(userInfo.Id, userInfo);
        await SaveUsersToFileAsync();
        return userInfo;
    }
    public Task<bool> DeleteUserAsync(string userId) => Task.FromResult(true);
    public Task<List<ChatSessionInfo>> GetUserSessionsAsync(string userId) => Task.FromResult(new List<ChatSessionInfo>());
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
        if (_userSessionCache.TryGetValue(userId, out var existingSessions))
        {
            existingSessions.Add(userSession);
        }
        else
        {
            _userSessionCache.TryAdd(userId, new List<UserSession> { userSession });
        }
        
        // 更新用户的会话列表
        if (_userCache.TryGetValue(userId, out var user))
        {
            user.SessionIds.Add(session.Id);
            await UpdateUserAsync(user);
        }
        
        await SaveUserSessionsToFileAsync();
        
        return session;
    }
    public Task<bool> DeleteUserSessionAsync(string userId, string sessionId) => Task.FromResult(true);
    public Task<bool> ValidateUserSessionAccessAsync(string userId, string sessionId)
    {
        if (_userSessionCache.TryGetValue(userId, out var sessions))
        {
            return Task.FromResult(sessions.Any(s => s.SessionId == sessionId && s.IsActive));
        }
        return Task.FromResult(false);
    }
    public Task UpdateUserActivityAsync(string userId) => Task.CompletedTask;
    public Task<List<UserInfo>> GetAllUsersAsync() => Task.FromResult(_userCache.Values.ToList());
    public Task<int> CleanupExpiredGuestUsersAsync(int expiredHours = 24) => Task.FromResult(0);
    public Task SetCurrentUserIdAsync(string userId) { _currentUserId = userId; return Task.CompletedTask; }
    public Task<string?> GetCurrentUserIdAsync() => Task.FromResult(_currentUserId);
    public Task LogoutAsync() { _currentUserId = null; return Task.CompletedTask; }
    public async Task<UserInfo> GetOrCreateGuestUserByFingerprintAsync(string browserFingerprint) => await CreateGuestUserAsync();
    public async Task<UserInfo?> ValidateUserLoginAsync(string username) => await GetUserByNameAsync(username);

    /// <summary>
    /// 生成密码盐值
    /// </summary>
    private static string GenerateSalt()
    {
        var saltBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(saltBytes);
        }
        return Convert.ToBase64String(saltBytes);
    }

    /// <summary>
    /// 计算密码哈希
    /// </summary>
    private static string ComputePasswordHash(string password, string salt)
    {
        var saltBytes = Convert.FromBase64String(salt);
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        
        var hashBytes = Rfc2898DeriveBytes.Pbkdf2(passwordBytes, saltBytes, 10000, HashAlgorithmName.SHA256, 32);
        
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// 验证密码
    /// </summary>
    private static bool VerifyPassword(string password, string hash, string salt)
    {
        var computedHash = ComputePasswordHash(password, salt);
        return computedHash == hash;
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
                
                _userCache.Clear();
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
    /// 从文件加载用户会话关联数据
    /// </summary>
    private void LoadUserSessionsFromFile()
    {
        try
        {
            if (File.Exists(_userSessionsFilePath))
            {
                var json = File.ReadAllText(_userSessionsFilePath);
                var userSessions = JsonSerializer.Deserialize<Dictionary<string, List<UserSession>>>(json, _jsonOptions) ?? new Dictionary<string, List<UserSession>>();
                
                _userSessionCache.Clear();
                foreach (var kvp in userSessions)
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
}