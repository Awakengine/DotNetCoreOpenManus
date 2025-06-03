using OpenManus.WebUI.Models;

namespace OpenManus.WebUI.Services;

/// <summary>
/// 文件管理服务类，负责工作区文件的管理和操作
/// </summary>
public class FileManagementService
{
    /// <summary>
    /// 工作区根目录路径
    /// </summary>
    private readonly string _workspaceRoot;

    /// <summary>
    /// 构造函数，初始化文件管理服务
    /// </summary>
    /// <param name="configuration">配置对象</param>
    public FileManagementService(IConfiguration configuration)
    {
        _workspaceRoot = Path.Combine(Directory.GetCurrentDirectory(), "workspace");
        if (!Directory.Exists(_workspaceRoot))
        {
            Directory.CreateDirectory(_workspaceRoot);
        }
    }

    /// <summary>
    /// 获取指定路径下的文件和目录列表
    /// </summary>
    /// <param name="relativePath">相对路径，默认为根目录</param>
    /// <returns>文件信息列表</returns>
    public async Task<List<Models.FileInfo>> GetFilesAsync(string relativePath = "")
    {
        await Task.CompletedTask; // 避免async警告
        
        var fullPath = Path.Combine(_workspaceRoot, relativePath);
        if (!Directory.Exists(fullPath))
        {
            return new List<Models.FileInfo>();
        }

        var files = new List<Models.FileInfo>();

        // 添加目录
        foreach (var dir in Directory.GetDirectories(fullPath))
        {
            var dirInfo = new DirectoryInfo(dir);
            files.Add(new Models.FileInfo
            {
                Name = dirInfo.Name,
                Path = Path.GetRelativePath(_workspaceRoot, dir).Replace('\\', '/'),
                IsDirectory = true,
                LastModified = dirInfo.LastWriteTime,
                Size = 0
            });
        }

        // 添加文件
        foreach (var file in Directory.GetFiles(fullPath))
        {
            var fileInfo = new System.IO.FileInfo(file);
            files.Add(new Models.FileInfo
            {
                Name = fileInfo.Name,
                Path = Path.GetRelativePath(_workspaceRoot, file).Replace('\\', '/'),
                Extension = fileInfo.Extension,
                IsDirectory = false,
                LastModified = fileInfo.LastWriteTime,
                Size = fileInfo.Length,
                MimeType = GetMimeType(fileInfo.Extension)
            });
        }

        return files.OrderBy(f => !f.IsDirectory).ThenBy(f => f.Name).ToList();
    }

    /// <summary>
    /// 读取文件内容
    /// </summary>
    /// <param name="relativePath">文件的相对路径</param>
    /// <returns>文件内容</returns>
    /// <exception cref="FileNotFoundException">文件不存在时抛出</exception>
    public async Task<string> ReadFileContentAsync(string relativePath)
    {
        var fullPath = Path.Combine(_workspaceRoot, relativePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"文件不存在: {relativePath}");
        }

        return await File.ReadAllTextAsync(fullPath);
    }

    /// <summary>
    /// 读取文件的字节数组
    /// </summary>
    /// <param name="relativePath">文件的相对路径</param>
    /// <returns>文件的字节数组</returns>
    /// <exception cref="FileNotFoundException">文件不存在时抛出</exception>
    public async Task<byte[]> ReadFileBytesAsync(string relativePath)
    {
        var fullPath = Path.Combine(_workspaceRoot, relativePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"文件不存在: {relativePath}");
        }

        return await File.ReadAllBytesAsync(fullPath);
    }

    /// <summary>
    /// 写入文件内容
    /// </summary>
    /// <param name="relativePath">文件的相对路径</param>
    /// <param name="content">要写入的内容</param>
    public async Task WriteFileAsync(string relativePath, string content)
    {
        var fullPath = Path.Combine(_workspaceRoot, relativePath);
        var directory = Path.GetDirectoryName(fullPath);
        
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        await File.WriteAllTextAsync(fullPath, content);
    }
    
    /// <summary>
    /// 检查文件是否存在
    /// </summary>
    /// <param name="relativePath">文件的相对路径</param>
    /// <returns>文件是否存在</returns>
    public async Task<bool> FileExistsAsync(string relativePath)
    {
        await Task.CompletedTask; // 避免async警告
        var fullPath = Path.Combine(_workspaceRoot, relativePath);
        return File.Exists(fullPath);
    }

    /// <summary>
    /// 获取文件的完整路径
    /// </summary>
    /// <param name="relativePath">文件的相对路径</param>
    /// <returns>文件的完整路径</returns>
    public string GetFullPath(string relativePath)
    {
        return Path.Combine(_workspaceRoot, relativePath);
    }

    /// <summary>
    /// 根据文件扩展名获取MIME类型
    /// </summary>
    /// <param name="extension">文件扩展名</param>
    /// <returns>MIME类型</returns>
    private string GetMimeType(string extension)
    {
        return extension.ToLower() switch
        {
            ".txt" => "text/plain",
            ".md" => "text/markdown",
            ".json" => "application/json",
            ".xml" => "application/xml",
            ".html" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".py" => "text/x-python",
            ".cs" => "text/x-csharp",
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };
    }
}

