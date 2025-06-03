namespace OpenManus.WebUI.Services.Tools;

/// <summary>
/// 代理工具接口，定义所有代理工具必须实现的基本功能
/// </summary>
public interface IAgentTool
{
    /// <summary>
    /// 工具名称
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// 工具描述
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// 执行工具功能
    /// </summary>
    /// <param name="arguments">工具参数</param>
    /// <returns>执行结果</returns>
    Task<string> ExecuteAsync(Dictionary<string, object> arguments);
    
    /// <summary>
    /// 获取工具的JSON Schema定义
    /// </summary>
    /// <returns>工具的Schema</returns>
    Dictionary<string, object> GetSchema();
}

/// <summary>
/// 代理工具基类，提供通用的参数处理功能
/// </summary>
public abstract class BaseAgentTool : IAgentTool
{
    /// <summary>
    /// 工具名称
    /// </summary>
    public abstract string Name { get; }
    
    /// <summary>
    /// 工具描述
    /// </summary>
    public abstract string Description { get; }
    
    /// <summary>
    /// 执行工具功能
    /// </summary>
    /// <param name="arguments">工具参数</param>
    /// <returns>执行结果</returns>
    public abstract Task<string> ExecuteAsync(Dictionary<string, object> arguments);
    
    /// <summary>
    /// 获取工具的JSON Schema定义
    /// </summary>
    /// <returns>工具的Schema</returns>
    public abstract Dictionary<string, object> GetSchema();
    
    /// <summary>
    /// 从参数字典中获取指定类型的参数值
    /// </summary>
    /// <typeparam name="T">参数类型</typeparam>
    /// <param name="arguments">参数字典</param>
    /// <param name="key">参数键名</param>
    /// <param name="defaultValue">默认值</param>
    /// <returns>参数值</returns>
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