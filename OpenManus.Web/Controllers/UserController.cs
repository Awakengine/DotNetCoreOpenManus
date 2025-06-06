using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OpenManus.Web.Services;
using OpenManus.Web.Models;
using System.Security.Claims;

namespace OpenManus.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IJwtService _jwtService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, IJwtService jwtService, ILogger<UserController> logger)
        {
            _userService = userService;
            _jwtService = jwtService;
            _logger = logger;
        }

        /// <summary>
        /// 用户登录
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                var (user, token) = await _userService.LoginWithJwtAsync(request.Username, request.Password);
                
                if (user != null && !string.IsNullOrEmpty(token))
                {
                    return Ok(new LoginResponse
                    {
                        Success = true,
                        Token = token,
                        User = user,
                        Message = "登录成功"
                    });
                }
                
                return Unauthorized(new LoginResponse
                {
                    Success = false,
                    Message = "用户名或密码错误"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登录过程中发生错误");
                return StatusCode(500, new LoginResponse
                {
                    Success = false,
                    Message = "登录过程中发生错误"
                });
            }
        }

        /// <summary>
        /// 用户注册
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                // 检查用户名是否已存在
                var existingUser = await _userService.GetUserByNameAsync(request.Username);
                if (existingUser != null)
                {
                    return BadRequest(new RegisterResponse
                    {
                        Success = false,
                        Message = "用户名已存在"
                    });
                }

                // 创建新用户
                var newUser = await _userService.CreateRegisteredUserWithPasswordAsync(
                    request.Username, 
                    request.Password, 
                    request.Avatar ?? "fas fa-user"
                );

                // 生成JWT令牌
                var token = _jwtService.GenerateToken(newUser);

                return Ok(new RegisterResponse
                {
                    Success = true,
                    Token = token,
                    User = newUser,
                    Message = "注册成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "注册过程中发生错误");
                return StatusCode(500, new RegisterResponse
                {
                    Success = false,
                    Message = "注册过程中发生错误"
                });
            }
        }

        /// <summary>
        /// 创建游客用户
        /// </summary>
        [HttpPost("guest")]
        public async Task<IActionResult> CreateGuest([FromBody] GuestRequest? request = null)
        {
            try
            {
                var guestUser = await _userService.CreateGuestUserAsync(request?.Name);
                var token = _jwtService.GenerateToken(guestUser);

                return Ok(new LoginResponse
                {
                    Success = true,
                    Token = token,
                    User = guestUser,
                    Message = "游客用户创建成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建游客用户过程中发生错误");
                return StatusCode(500, new LoginResponse
                {
                    Success = false,
                    Message = "创建游客用户失败"
                });
            }
        }

        /// <summary>
        /// 获取当前用户信息
        /// </summary>
        [HttpGet("current")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "无效的令牌" });
                }

                var user = await _userService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "用户不存在" });
                }

                return Ok(new { success = true, user });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取当前用户信息时发生错误");
                return StatusCode(500, new { message = "获取用户信息失败" });
            }
        }

        /// <summary>
        /// 用户退出登录
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await _userService.LogoutAsync();
                return Ok(new { success = true, message = "退出登录成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "退出登录过程中发生错误");
                return StatusCode(500, new { message = "退出登录失败" });
            }
        }
    }

    // 请求和响应模型
    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Avatar { get; set; }
    }

    public class GuestRequest
    {
        public string? Name { get; set; }
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public UserInfo? User { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class RegisterResponse
    {
        public bool Success { get; set; }
        public string? Token { get; set; }
        public UserInfo? User { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}