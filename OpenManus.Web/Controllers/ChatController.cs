using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using OpenManus.Web.Services;
using OpenManus.Web.Models;
using System.Security.Claims;

namespace OpenManus.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly SessionManagementService _sessionService;
        private readonly ILogger<ChatController> _logger;

        public ChatController(IUserService userService, SessionManagementService sessionService, ILogger<ChatController> logger)
        {
            _userService = userService;
            _sessionService = sessionService;
            _logger = logger;
        }

        /// <summary>
        /// 获取用户的所有聊天会话
        /// </summary>
        [HttpGet("sessions")]
        public async Task<IActionResult> GetSessions()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "无效的令牌" });
                }

                var sessions = await _userService.GetUserSessionsAsync(userId);
                return Ok(new { success = true, sessions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取聊天会话时发生错误");
                return StatusCode(500, new { message = "获取聊天会话失败" });
            }
        }

        /// <summary>
        /// 创建新的聊天会话
        /// </summary>
        [HttpPost("sessions")]
        public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "无效的令牌" });
                }

                var session = await _userService.CreateUserSessionAsync(userId, request.Title ?? "新对话");

                return Ok(new { success = true, session });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建聊天会话时发生错误");
                return StatusCode(500, new { message = "创建聊天会话失败" });
            }
        }

        /// <summary>
        /// 获取指定会话的详细信息
        /// </summary>
        [HttpGet("sessions/{sessionId}")]
        public async Task<IActionResult> GetSession(string sessionId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "无效的令牌" });
                }

                // 验证用户是否有权访问该会话
                var hasAccess = await _userService.ValidateUserSessionAccessAsync(userId, sessionId);
                if (!hasAccess)
                {
                    return Forbid("无权访问该会话");
                }

                var session = await _sessionService.GetSessionAsync(sessionId);
                if (session == null)
                {
                    return NotFound(new { message = "会话不存在" });
                }

                return Ok(new { success = true, session });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取会话详情时发生错误");
                return StatusCode(500, new { message = "获取会话详情失败" });
            }
        }

        /// <summary>
        /// 更新会话标题
        /// </summary>
        [HttpPut("sessions/{sessionId}")]
        public async Task<IActionResult> UpdateSession(string sessionId, [FromBody] UpdateSessionRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "无效的令牌" });
                }

                // 验证用户是否有权访问该会话
                var hasAccess = await _userService.ValidateUserSessionAccessAsync(userId, sessionId);
                if (!hasAccess)
                {
                    return Forbid("无权访问该会话");
                }

                var session = await _sessionService.GetSessionAsync(sessionId);
                if (session == null)
                {
                    return NotFound(new { message = "会话不存在" });
                }

                // 更新会话标题
                session.Title = request.Title ?? session.Title;
                await _sessionService.UpdateSessionAsync(session);

                return Ok(new { success = true, session });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新会话时发生错误");
                return StatusCode(500, new { message = "更新会话失败" });
            }
        }

        /// <summary>
        /// 删除会话
        /// </summary>
        [HttpDelete("sessions/{sessionId}")]
        public async Task<IActionResult> DeleteSession(string sessionId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "无效的令牌" });
                }

                // 验证用户是否有权访问该会话
                var hasAccess = await _userService.ValidateUserSessionAccessAsync(userId, sessionId);
                if (!hasAccess)
                {
                    return Forbid("无权访问该会话");
                }

                await _userService.DeleteUserSessionAsync(userId, sessionId);
                return Ok(new { success = true, message = "会话删除成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除会话时发生错误");
                return StatusCode(500, new { message = "删除会话失败" });
            }
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        [HttpPost("send")]
        [AllowAnonymous]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                string? userId = null;
                
                // 尝试从JWT令牌获取用户ID
                if (User.Identity?.IsAuthenticated == true)
                {
                    userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                }
                
                // 如果没有JWT令牌，尝试从会话获取用户ID
                if (string.IsNullOrEmpty(userId))
                {
                    userId = HttpContext.Session.GetString("UserId");
                }
                
                // 如果仍然没有用户ID，返回未授权
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "用户未登录" });
                }

                // 验证用户是否有权访问该会话
                var hasAccess = await _userService.ValidateUserSessionAccessAsync(userId, request.SessionId);
                if (!hasAccess)
                {
                    return Forbid("无权访问该会话");
                }

                // 获取会话信息
                var session = await _sessionService.GetSessionAsync(request.SessionId);
                if (session == null)
                {
                    return NotFound(new { message = "会话不存在" });
                }

                // 添加用户消息到会话
                var userMessage = new ChatMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    Role = "user",
                    Content = request.Message,
                    Timestamp = DateTime.UtcNow
                };
                session.Messages.Add(userMessage);

                // 这里应该调用AI服务来生成回复
                // 目前先返回一个简单的回复
                var aiResponse = $"收到您的消息：{request.Message}。这是一个测试回复。";
                
                var assistantMessage = new ChatMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    Role = "assistant",
                    Content = aiResponse,
                    Timestamp = DateTime.UtcNow
                };
                session.Messages.Add(assistantMessage);

                // 更新会话
                await _sessionService.UpdateSessionAsync(session);
                await _sessionService.IncrementMessageCountAsync(request.SessionId);
                await _userService.UpdateUserActivityAsync(userId);

                return Ok(new { success = true, response = aiResponse });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送消息时发生错误");
                return StatusCode(500, new { success = false, error = "发送消息失败" });
            }
        }

        /// <summary>
        /// 更新会话活动时间
        /// </summary>
        [HttpPost("sessions/{sessionId}/activity")]
        public async Task<IActionResult> UpdateSessionActivity(string sessionId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "无效的令牌" });
                }

                // 验证用户是否有权访问该会话
                var hasAccess = await _userService.ValidateUserSessionAccessAsync(userId, sessionId);
                if (!hasAccess)
                {
                    return Forbid("无权访问该会话");
                }

                await _userService.UpdateUserActivityAsync(userId);
                await _sessionService.IncrementMessageCountAsync(sessionId);

                return Ok(new { success = true, message = "活动时间更新成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新会话活动时间时发生错误");
                return StatusCode(500, new { message = "更新活动时间失败" });
            }
        }
    }

    // 请求模型
    public class CreateSessionRequest
    {
        public string? Title { get; set; }
    }

    public class UpdateSessionRequest
    {
        public string? Title { get; set; }
    }

    public class SendMessageRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}