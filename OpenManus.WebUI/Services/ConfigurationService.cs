using Microsoft.Extensions.Configuration;
using OpenManus.WebUI.Models;
using System.Text.Json;

namespace OpenManus.WebUI.Services
{
    /// <summary>
    /// 配置服务接口
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// 获取应用程序设置
        /// </summary>
        /// <returns>应用程序设置</returns>
        AppSettings GetAppSettings();

        /// <summary>
        /// 保存应用程序设置
        /// </summary>
        /// <param name="settings">要保存的设置</param>
        Task SaveAppSettingsAsync(AppSettings settings);
    }

    /// <summary>
    /// 配置服务实现类，负责管理应用程序配置的读取和保存
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        /// <summary>
        /// 配置对象
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// Web主机环境
        /// </summary>
        private readonly IWebHostEnvironment _environment;

        /// <summary>
        /// 当前设置缓存
        /// </summary>
        private AppSettings _currentSettings = new AppSettings();

        private static JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };


        /// <summary>
        /// 构造函数，初始化配置服务
        /// </summary>
        /// <param name="configuration">配置对象</param>
        /// <param name="environment">Web主机环境</param>
        public ConfigurationService(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _environment = environment;
            LoadSettings();
        }

        /// <summary>
        /// 获取应用程序设置
        /// </summary>
        /// <returns>应用程序设置</returns>
        public AppSettings GetAppSettings()
        {
            return _currentSettings;
        }

        /// <summary>
        /// 保存应用程序设置到配置文件
        /// </summary>
        /// <param name="settings">要保存的设置</param>
        public async Task SaveAppSettingsAsync(AppSettings settings)
        {
            _currentSettings = settings;

            // 确定要更新的配置文件
            var fileName = _environment.IsDevelopment() ? "appsettings.Development.json" : "appsettings.json";
            var filePath = Path.Combine(_environment.ContentRootPath, fileName);

            // 读取现有配置
            var json = await File.ReadAllTextAsync(filePath);
            var config = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();

            // 更新AppSettings部分
            config["AppSettings"] = JsonSerializer.SerializeToElement(settings);

            // 写回文件
            var updatedJson = JsonSerializer.Serialize(config, options);
            await File.WriteAllTextAsync(filePath, updatedJson);
        }

        /// <summary>
        /// 从配置文件加载设置
        /// </summary>
        private void LoadSettings()
        {
            _currentSettings = new AppSettings();
            _configuration.GetSection("AppSettings").Bind(_currentSettings);
        }
    }
}