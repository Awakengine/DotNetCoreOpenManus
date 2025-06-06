using Microsoft.AspNetCore.Mvc;
using OpenManus.Web.Services;
using OpenManus.Web.Models;
using System.ComponentModel.DataAnnotations;

namespace OpenManus.Web.Controllers
{
    public class UserManagementController : Controller
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserManagementController> _logger;

        public UserManagementController(
            IUserService userService,
            ILogger<UserManagementController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            try
            {
                var user = await _userService.ValidateUserLoginWithPasswordAsync(model.Name, model.Password);
                if (user != null)
                {
                    // 登录成功，设置会话
                    HttpContext.Session.SetString("UserId", user.Id);
                    HttpContext.Session.SetString("UserName", user.Name);
                    
                    // 重定向到Agent页面
                    return RedirectToAction("Index", "Agent");
                }
                else
                {
                    ViewBag.ErrorMessage = "用户名或密码错误";
                    return View("Index", model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登录时发生错误");
                ViewBag.ErrorMessage = "登录失败，请稍后重试";
                return View("Index", model);
            }
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // 检查用户是否已存在
                var existingUser = await _userService.GetUserByNameAsync(model.Name);
                if (existingUser != null)
                {
                    ViewBag.ErrorMessage = "用户名已存在";
                    return View(model);
                }

                // 创建新用户
                await _userService.CreateRegisteredUserWithPasswordAsync(model.Name, model.Password, "");
                
                // 注册成功，重定向到登录页面
                ViewBag.SuccessMessage = "注册成功，请登录";
                return View("Index", new LoginViewModel());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "注册时发生错误");
                ViewBag.ErrorMessage = "注册失败，请稍后重试";
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> LoginAsGuest()
        {
            try
            {
                // 创建游客用户
                var guestUser = await _userService.CreateGuestUserAsync();
                
                // 设置会话
                HttpContext.Session.SetString("UserId", guestUser.Id);
                HttpContext.Session.SetString("UserName", guestUser.Name);
                
                // 重定向到Agent页面
                return RedirectToAction("Index", "Agent");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "游客登录时发生错误");
                ViewBag.ErrorMessage = "游客登录失败，请稍后重试";
                return View("Index", new LoginViewModel());
            }
        }

        [HttpPost]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }
    }

    public class LoginViewModel
    {
        [Required(ErrorMessage = "请输入用户名")]
        [Display(Name = "用户名")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "请输入密码")]
        [Display(Name = "密码")]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "请输入用户名")]
        [Display(Name = "用户名")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "请输入密码")]
        [MinLength(6, ErrorMessage = "密码至少6位")]
        [Display(Name = "密码")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "请确认密码")]
        [Compare("Password", ErrorMessage = "两次输入的密码不一致")]
        [Display(Name = "确认密码")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "请输入有效的邮箱地址")]
        [Display(Name = "邮箱")]
        public string? Email { get; set; }
    }
}