namespace OpenManus.WebUI.Models
{

    /// <summary>
    /// 文件预览信息类
    /// </summary>
    public class FilePreviewInfo
    {
        /// <summary>
        /// 文件名
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 文件路径
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// 文件内容
        /// </summary>
        public string Content { get; set; } = string.Empty;
    }

}
