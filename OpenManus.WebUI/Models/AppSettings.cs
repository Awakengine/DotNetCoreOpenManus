namespace OpenManus.WebUI.Models
{
    /// <summary>
    /// 应用程序设置类
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// 主题设置（light/dark）
        /// </summary>
        public string Theme { get; set; } = "light";
        
        /// <summary>
        /// 语言设置
        /// </summary>
        public string Language { get; set; } = "zh-CN";
        
        /// <summary>
        /// 大语言模型配置
        /// </summary>
        public LLMConfig LLMConfig { get; set; } = new();
    }

    /// <summary>
    /// 大语言模型配置类
    /// </summary>
    public class LLMConfig
    {
        /// <summary>
        /// 模型名称
        /// </summary>
        public string Model { get; set; } = "gemma-3-12b-it-qat";
        
        /// <summary>
        /// API基础URL
        /// </summary>
        public string BaseUrl { get; set; } = "http://localhost:1234/v1/";
        
        /// <summary>
        /// API密钥
        /// </summary>
        public string ApiKey { get; set; } = "sk-xxxxxxxxxxxxxxxxxxxxxxx";
        
        /// <summary>
        /// 最大令牌数
        /// </summary>
        public int MaxTokens { get; set; } = 4096;
        
        /// <summary>
        /// 温度参数，控制生成文本的随机性
        /// </summary>
        public double Temperature { get; set; } = 0.6;
    }
}