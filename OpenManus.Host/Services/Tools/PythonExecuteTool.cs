using System.Diagnostics;
using System.Text;

namespace OpenManus.Host.Services.Tools;

public class PythonExecuteTool : BaseAgentTool
{
    public override string Name => "python_execute";
    public override string Description => "Execute Python code and return the output";
    
    public override async Task<string> ExecuteAsync(Dictionary<string, object> arguments)
    {
        var code = GetArgument<string>(arguments, "code", "");
        var timeout = GetArgument<int>(arguments, "timeout", 30000); // 30 seconds default
        
        if (string.IsNullOrWhiteSpace(code))
        {
            return "Error: No Python code provided";
        }
        
        try
        {
            // Create a temporary Python file
            var tempFile = Path.GetTempFileName() + ".py";
            await File.WriteAllTextAsync(tempFile, code);
            
            var processInfo = new ProcessStartInfo
            {
                FileName = "python3",
                Arguments = tempFile,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            using var process = new Process { StartInfo = processInfo };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            
            process.OutputDataReceived += (sender, e) => {
                if (e.Data != null) outputBuilder.AppendLine(e.Data);
            };
            
            process.ErrorDataReceived += (sender, e) => {
                if (e.Data != null) errorBuilder.AppendLine(e.Data);
            };
            
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            
            var completed = await Task.Run(() => process.WaitForExit(timeout));
            
            if (!completed)
            {
                process.Kill();
                return "Error: Python execution timed out";
            }
            
            // Clean up temp file
            try { File.Delete(tempFile); } catch { }
            
            var output = outputBuilder.ToString().Trim();
            var error = errorBuilder.ToString().Trim();
            
            if (process.ExitCode != 0)
            {
                return $"Python execution failed (exit code {process.ExitCode}):\n{error}";
            }
            
            if (!string.IsNullOrEmpty(error))
            {
                return $"Python output with warnings:\n{output}\n\nWarnings:\n{error}";
            }
            
            return string.IsNullOrEmpty(output) ? "Python code executed successfully (no output)" : $"Python output:\n{output}";
        }
        catch (Exception ex)
        {
            return $"Error executing Python code: {ex.Message}";
        }
    }
    
    public override Dictionary<string, object> GetSchema()
    {
        return new Dictionary<string, object>
        {
            ["type"] = "object",
            ["properties"] = new Dictionary<string, object>
            {
                ["code"] = new Dictionary<string, object>
                {
                    ["type"] = "string",
                    ["description"] = "Python code to execute"
                },
                ["timeout"] = new Dictionary<string, object>
                {
                    ["type"] = "integer",
                    ["description"] = "Execution timeout in milliseconds (default: 30000)",
                    ["default"] = 30000
                }
            },
            ["required"] = new[] { "code" }
        };
    }
}