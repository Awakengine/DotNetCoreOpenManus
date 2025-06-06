using Microsoft.AspNetCore.Mvc;
using OpenManus.Web.Services;
using OpenManus.Web.Models;
using System.Text.Json;

namespace OpenManus.Web.Controllers
{
    public class AgentController : Controller
    {
        private readonly IUserService _userService;
        private readonly SessionManagementService _sessionService;
        private readonly ILogger<AgentController> _logger;

        public AgentController(
            IUserService userService,
            SessionManagementService sessionService,
            ILogger<AgentController> logger)
        {
            _userService = userService;
            _sessionService = sessionService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? sessionId = null)
        {
            try
            {
                // 获取或创建当前用户（支持游客模式）
                var currentUser = await _userService.GetCurrentUserAsync();
                var userId = currentUser.Id;
                
                ChatSessionInfo? session = null;
                
                if (!string.IsNullOrEmpty(sessionId))
                {
                    // 验证会话访问权限
                    var hasAccess = await _userService.ValidateUserSessionAccessAsync(userId, sessionId);
                    if (!hasAccess)
                    {
                        return Forbid("无权访问此会话");
                    }
                    
                    // 获取现有会话
                    session = await _sessionService.GetSessionAsync(sessionId);
                    if (session == null)
                    {
                        return NotFound("会话不存在");
                    }
                }
                else
                {
                    // 创建新会话
                    session = await _userService.CreateUserSessionAsync(userId, "新对话");
                    
                    // 重定向到带有会话ID的URL
                    return RedirectToAction("Index", new { sessionId = session.Id });
                }
                
                ViewBag.SessionId = session.Id;
                ViewBag.SessionTitle = session.Title;
                ViewBag.CurrentUser = currentUser;
                return View(session);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "访问Agent页面时发生错误");
                return StatusCode(500, "服务器内部错误");
            }
        }
    }
}