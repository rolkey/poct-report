using System.IO;
using System.Reflection;

namespace TrayApp.Plugins;

public class PluginManager
{
    private readonly Dictionary<string, IPlugin> _plugins = new();
    private readonly string _pluginDir = "plugins";

    public void LoadPlugins()
    {
        Logger.Info($"开始加载插件目录: {_pluginDir}");

        if (!Directory.Exists(_pluginDir))
        {
            Logger.Info($"插件目录不存在，创建: {_pluginDir}");
            Directory.CreateDirectory(_pluginDir);
            return;
        }

        var files = Directory.GetFiles(_pluginDir, "*.dll");
        Logger.Info($"找到 {files.Length} 个 DLL 文件");

        foreach (var file in files)
        {
            try
            {
                Logger.Info($"尝试加载: {file}");
                var assembly = Assembly.LoadFrom(file);
                var type = assembly.GetTypes()
                    .FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface);

                if (type != null)
                {
                    var plugin = (IPlugin)Activator.CreateInstance(type)!;
                    _plugins[plugin.Name] = plugin;
                    Logger.Info($"成功加载插件: {plugin.Name} v{plugin.Version}");
                }
                else
                {
                    Logger.Info($"跳过: {file} (未找到 IPlugin 实现)");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"加载 DLL 失败: {file}", ex);
            }
        }

        Logger.Info($"插件加载完成，共 {_plugins.Count} 个");
    }

    public object? Invoke(string pluginName, string method, object?[] parameters)
    {
        if (_plugins.TryGetValue(pluginName, out var plugin))
        {
            try
            {
                var result = plugin.Execute(method, parameters);
                Logger.Info($"插件 {pluginName}.{method} 执行成功");
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error($"插件 {pluginName}.{method} 执行失败", ex);
                throw;
            }
        }
        Logger.Warn($"未找到插件: {pluginName}");
        return null;
    }

    public List<string> GetLoadedPlugins() => _plugins.Keys.ToList();
}