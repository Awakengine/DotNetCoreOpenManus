using OpenManus.Host.Models;

namespace OpenManus.Host.Services;

public class FileManagementService
{
    private readonly string _workspaceRoot;

    public FileManagementService(IConfiguration configuration)
    {
        _workspaceRoot = Path.Combine(Directory.GetCurrentDirectory(), "workspace");
        if (!Directory.Exists(_workspaceRoot))
        {
            Directory.CreateDirectory(_workspaceRoot);
        }
    }

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

    public async Task<string> ReadFileContentAsync(string relativePath)
    {
        var fullPath = Path.Combine(_workspaceRoot, relativePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"文件不存在: {relativePath}");
        }

        return await File.ReadAllTextAsync(fullPath);
    }

    public async Task<byte[]> ReadFileBytesAsync(string relativePath)
    {
        var fullPath = Path.Combine(_workspaceRoot, relativePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"文件不存在: {relativePath}");
        }

        return await File.ReadAllBytesAsync(fullPath);
    }

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
    
    public async Task<bool> FileExistsAsync(string relativePath)
    {
        await Task.CompletedTask; // 避免async警告
        var fullPath = Path.Combine(_workspaceRoot, relativePath);
        return File.Exists(fullPath);
    }

    public string GetFullPath(string relativePath)
    {
        return Path.Combine(_workspaceRoot, relativePath);
    }

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

