using OpenManus.Host.Components;
using OpenManus.Host.Services;

// 创建Web应用程序构建器
var builder = WebApplication.CreateBuilder(args);

// 添加服务到容器
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 注册自定义服务
builder.Services.AddSingleton<FileManagementService>(); // 文件管理服务
builder.Services.AddSingleton<ChatService>(); // 聊天服务
builder.Services.AddSingleton<IChatHistoryService, ChatHistoryService>(); // 聊天历史服务
builder.Services.AddSingleton<AgentService>(); // AI代理服务
builder.Services.AddSingleton<IConfigurationService, ConfigurationService>(); // 配置服务
// builder.Services.AddSingleton<SessionManagementService>(); // 会话管理服务（已注释）
builder.Services.AddHttpClient(); // HTTP客户端服务

// 构建应用程序
var app = builder.Build();

// 配置HTTP请求管道
// 如果不是开发环境，配置异常处理和HSTS
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // 默认HSTS值为30天，生产环境可能需要调整
    app.UseHsts();
}

// 启用HTTPS重定向
app.UseHttpsRedirection();

// 启用静态文件服务
app.UseStaticFiles();
// 启用防伪令牌
app.UseAntiforgery();

// 映射Razor组件并启用交互式服务器渲染模式
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// 运行应用程序
app.Run();

