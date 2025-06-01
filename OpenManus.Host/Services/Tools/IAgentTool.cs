namespace OpenManus.Host.Services.Tools;

public interface IAgentTool
{
    string Name { get; }
    string Description { get; }
    Task<string> ExecuteAsync(Dictionary<string, object> arguments);
    Dictionary<string, object> GetSchema();
}

public abstract class BaseAgentTool : IAgentTool
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    
    public abstract Task<string> ExecuteAsync(Dictionary<string, object> arguments);
    public abstract Dictionary<string, object> GetSchema();
    
    protected T GetArgument<T>(Dictionary<string, object> arguments, string key, T defaultValue = default!)
    {
        if (arguments.TryGetValue(key, out var value))
        {
            try
            {
                if (value is T directValue)
                    return directValue;
                    
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }
}