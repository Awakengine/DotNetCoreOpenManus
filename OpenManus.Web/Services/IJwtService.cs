using OpenManus.Web.Models;

namespace OpenManus.Web.Services;

public interface IJwtService
{
    /// <summary>
    /// 生成JWT令牌
    /// </summary>
    /// <param name="user">用户信息</param>
    /// <returns>JWT令牌</returns>
    string GenerateToken(UserInfo user);

    /// <summary>
    /// 验证JWT令牌
    /// </summary>
    /// <param name="token">JWT令牌</param>
    /// <returns>用户ID，如果令牌无效则返回null</returns>
    string? ValidateToken(string token);

    /// <summary>
    /// 从令牌中获取用户ID
    /// </summary>
    /// <param name="token">JWT令牌</param>
    /// <returns>用户ID</returns>
    string? GetUserIdFromToken(string token);
}