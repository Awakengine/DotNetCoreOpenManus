using OpenManus.WebUI.Models;

namespace OpenManus.WebUI.Services;

/// <summary>
/// 用户服务接口
/// 定义用户管理的核心功能，包括用户创建、认证、会话管理等
/// </summary>
public interface IUserService
{
    /// <summary>
    /// 获取当前用户信息
    /// </summary>
    /// <returns>当前用户信息</returns>
    Task<UserInfo> GetCurrentUserAsync();
    
    /// <summary>
    /// 根据用户ID获取用户信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>用户信息</returns>
    Task<UserInfo?> GetUserByIdAsync(string userId);
    
    /// <summary>
    /// 创建游客用户
    /// </summary>
    /// <param name="name">用户名称，可选</param>
    /// <returns>新创建的游客用户信息</returns>
    Task<UserInfo> CreateGuestUserAsync(string? name = null);
    
    /// <summary>
    /// 创建注册用户
    /// </summary>
    /// <param name="name">用户名称</param>
    /// <param name="avatar">用户头像</param>
    /// <returns>新创建的注册用户信息</returns>
    Task<UserInfo> CreateRegisteredUserAsync(string name, string avatar);
    
    /// <summary>
    /// 创建带密码的注册用户
    /// </summary>
    /// <param name="name">用户名称</param>
    /// <param name="password">密码</param>
    /// <param name="avatar">用户头像</param>
    /// <returns>新创建的注册用户信息</returns>
    Task<UserInfo> CreateRegisteredUserWithPasswordAsync(string name, string password, string avatar);
    
    /// <summary>
    /// 更新用户信息
    /// </summary>
    /// <param name="userInfo">用户信息</param>
    /// <returns>更新后的用户信息</returns>
    Task<UserInfo> UpdateUserAsync(UserInfo userInfo);
    
    /// <summary>
    /// 删除用户
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>是否删除成功</returns>
    Task<bool> DeleteUserAsync(string userId);
    
    /// <summary>
    /// 获取用户的所有会话
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>用户会话列表</returns>
    Task<List<ChatSessionInfo>> GetUserSessionsAsync(string userId);
    
    /// <summary>
    /// 为用户创建新会话
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="title">会话标题</param>
    /// <returns>新创建的会话信息</returns>
    Task<ChatSessionInfo> CreateUserSessionAsync(string userId, string? title = null);
    
    /// <summary>
    /// 删除用户会话
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="sessionId">会话ID</param>
    /// <returns>是否删除成功</returns>
    Task<bool> DeleteUserSessionAsync(string userId, string sessionId);
    
    /// <summary>
    /// 验证用户是否有权限访问指定会话
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="sessionId">会话ID</param>
    /// <returns>是否有权限</returns>
    Task<bool> ValidateUserSessionAccessAsync(string userId, string sessionId);
    
    /// <summary>
    /// 更新用户活动时间
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <returns>任务</returns>
    Task UpdateUserActivityAsync(string userId);
    
    /// <summary>
    /// 获取所有用户列表（管理员功能）
    /// </summary>
    /// <returns>用户列表</returns>
    Task<List<UserInfo>> GetAllUsersAsync();
    
    /// <summary>
    /// 清理过期的游客用户
    /// </summary>
    /// <param name="expiredHours">过期小时数，默认24小时</param>
    /// <returns>清理的用户数量</returns>
    Task<int> CleanupExpiredGuestUsersAsync(int expiredHours = 24);
    
    /// <summary>
    /// 设置当前用户ID
    /// </summary>
    /// <param name="userId">用户ID</param>
    Task SetCurrentUserIdAsync(string userId);
    
    /// <summary>
    /// 获取当前用户ID
    /// </summary>
    /// <returns>当前用户ID</returns>
    Task<string?> GetCurrentUserIdAsync();
    
    /// <summary>
    /// 用户退出登录
    /// </summary>
    Task LogoutAsync();
    
    /// <summary>
    /// 根据用户名查找注册用户
    /// </summary>
    /// <param name="username">用户名</param>
    /// <returns>用户信息</returns>
    Task<UserInfo?> GetUserByNameAsync(string username);
    
    /// <summary>
    /// 根据浏览器指纹获取或创建游客用户
    /// </summary>
    /// <param name="browserFingerprint">浏览器指纹</param>
    /// <returns>用户信息</returns>
    Task<UserInfo> GetOrCreateGuestUserByFingerprintAsync(string browserFingerprint);
    
    /// <summary>
    /// 验证用户登录
    /// </summary>
    /// <param name="username">用户名</param>
    /// <returns>用户信息，如果验证失败返回null</returns>
    Task<UserInfo?> ValidateUserLoginAsync(string username);
    
    /// <summary>
    /// 验证用户登录（带密码）
    /// </summary>
    /// <param name="username">用户名</param>
    /// <param name="password">密码</param>
    /// <returns>用户信息，如果验证失败返回null</returns>
    Task<UserInfo?> ValidateUserLoginWithPasswordAsync(string username, string password);

    /// <summary>
    /// 验证用户登录并生成JWT令牌
    /// </summary>
    Task<(UserInfo? user, string? token)> LoginWithJwtAsync(string username, string password);

    /// <summary>
    /// 通过JWT令牌获取当前用户
    /// </summary>
    Task<UserInfo?> GetUserByJwtTokenAsync(string token);
}