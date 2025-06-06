using OpenManus.WebUI.Components;
using OpenManus.WebUI.Services;
using OpenManus.WebUI.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// 注册 HttpClient 和 HttpClientService
builder.Services.AddHttpClient();
builder.Services.AddScoped<IHttpClientService, HttpClientService>();

// 注册自定义服务
builder.Services.AddSingleton<IConfigurationService, ConfigurationService>(); // 配置服务
builder.Services.AddSingleton<FileManagementService>(); // 文件管理服务
builder.Services.AddSingleton<IChatHistoryService, ChatHistoryService>(); // 聊天历史服务
builder.Services.AddSingleton<SessionManagementService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddSingleton<ChatService>(); // 聊天服务
builder.Services.AddScoped<AgentService>(); // AI代理服务
builder.Services.AddScoped<IJwtService, JwtService>(); // JWT服务

// 添加JWT认证
var jwtKey = builder.Configuration["Jwt:Key"] ?? "OpenManus_JWT_Secret_Key_2024_Very_Long_And_Secure_Key_For_Production_Use";
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// 添加全局异常处理中间件
app.UseGlobalExceptionHandling();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
