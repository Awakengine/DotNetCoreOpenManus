using Microsoft.IdentityModel.Tokens;
using OpenManus.WebUI.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace OpenManus.WebUI.Services;

public class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;
    private readonly string _secretKey;
    private readonly SymmetricSecurityKey _key;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
        _secretKey = _configuration["Jwt:Key"] ?? "OpenManus_JWT_Secret_Key_2024_Very_Long_And_Secure_Key_For_Production_Use";
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
    }

    /// <summary>
    /// 生成JWT令牌
    /// </summary>
    public string GenerateToken(UserInfo user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var credentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256Signature);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.Name),
            new("user_type", user.Type.ToString()),
            new("avatar", user.Avatar ?? string.Empty)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7), // 7天过期
            SigningCredentials = credentials
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// 验证JWT令牌
    /// </summary>
    public string? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = _key,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            
            return userIdClaim?.Value;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 从令牌中获取用户ID
    /// </summary>
    public string? GetUserIdFromToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jsonToken = tokenHandler.ReadJwtToken(token);
            var userIdClaim = jsonToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
            
            return userIdClaim?.Value;
        }
        catch
        {
            return null;
        }
    }
}