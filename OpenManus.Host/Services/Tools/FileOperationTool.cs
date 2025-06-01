using OpenManus.Host.Services;
using System.Text.Json;

namespace OpenManus.Host.Services.Tools;

public class FileOperationTool : BaseAgentTool
{
    private readonly FileManagementService _fileService;
    
    public FileOperationTool(FileManagementService fileService)
    {
        _fileService = fileService;
    }
    
    public override string Name => "file_operation";
    public override string Description => "Read, write, and manage files in the workspace";
    
    public override async Task<string> ExecuteAsync(Dictionary<string, object> arguments)
    {
        var operation = GetArgument<string>(arguments, "operation", "");
        var filePath = GetArgument<string>(arguments, "file_path", "");
        
        try
        {
            switch (operation.ToLower())
            {
                case "read":
                    var content = await _fileService.ReadFileContentAsync(filePath);
                    return $"File content:\n{content}";
                    
                case "write":
                    var writeContent = GetArgument<string>(arguments, "content", "");
                    await _fileService.WriteFileAsync(filePath, writeContent);
                    return $"Successfully wrote to file: {filePath}";
                    
                case "list":
                    var directory = GetArgument<string>(arguments, "directory", "");
                    var files = await _fileService.GetFilesAsync(directory);
                    var fileList = string.Join("\n", files.Select(f => $"{(f.IsDirectory ? "[DIR]" : "[FILE]")} {f.Name}"));
                    return $"Files in {directory}:\n{fileList}";
                    
                case "exists":
                    var exists = await _fileService.FileExistsAsync(filePath);
                    return $"File {filePath} exists: {exists}";
                    
                default:
                    return $"Unknown operation: {operation}. Supported operations: read, write, list, exists";
            }
        }
        catch (Exception ex)
        {
            return $"Error executing file operation: {ex.Message}";
        }
    }
    
    public override Dictionary<string, object> GetSchema()
    {
        return new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["operation"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["enum"] = new[] { "read", "write", "list", "exists" },
                    ["description"] = "The file operation to perform"
                },
                ["file_path"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "Path to the file"
                },
                ["content"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "Content to write (for write operation)"
                },
                ["directory"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "Directory to list (for list operation)"
                }
            },
            ["required"] = new[] { "operation" }
        };
    }
}