namespace OpenManus.WebUI.Models;

/// <summary>
/// 文件信息类
/// </summary>
public class FileInfo
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
    /// 文件扩展名
    /// </summary>
    public string Extension { get; set; } = string.Empty;
    
    /// <summary>
    /// 文件大小（字节）
    /// </summary>
    public long Size { get; set; }
    
    /// <summary>
    /// 最后修改时间
    /// </summary>
    public DateTime LastModified { get; set; }
    
    /// <summary>
    /// 是否为目录
    /// </summary>
    public bool IsDirectory { get; set; }
    
    /// <summary>
    /// MIME类型
    /// </summary>
    public string MimeType { get; set; } = string.Empty;
}

