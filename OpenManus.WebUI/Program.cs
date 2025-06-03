using OpenManus.WebUI.Components;
using OpenManus.WebUI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 注册 HttpClient 服务
builder.Services.AddHttpClient();

// 注册自定义服务
builder.Services.AddSingleton<FileManagementService>(); // 文件管理服务
builder.Services.AddSingleton<ChatService>(); // 聊天服务
builder.Services.AddSingleton<IChatHistoryService, ChatHistoryService>(); // 聊天历史服务
builder.Services.AddSingleton<AgentService>(); // AI代理服务
builder.Services.AddSingleton<IConfigurationService, ConfigurationService>(); // 配置服务

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
