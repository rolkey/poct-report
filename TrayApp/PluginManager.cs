using System.IO;
using System.Reflection;

namespace TrayApp.Plugins;

public class PluginManager
{
    private readonly Dictionary<string, IPlugin> _plugins = new();
    private readonly string _pluginDir = "plugins";

    public void LoadPlugins()
    {
        if (!Directory.Exists(_pluginDir))
        {
            Directory.CreateDirectory(_pluginDir);
            return;
        }

        foreach (var file in Directory.GetFiles(_pluginDir, "*.dll"))
        {
            try
            {
                var assembly = Assembly.LoadFrom(file);
                var type = assembly.GetTypes()
                    .FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface);

                if (type != null)
                {
                    var plugin = (IPlugin)Activator.CreateInstance(type)!;
                    _plugins[plugin.Name] = plugin;
                }
            }
            catch { }
        }
    }

    public object? Invoke(string pluginName, string method, object?[] parameters)
    {
        if (_plugins.TryGetValue(pluginName, out var plugin))
        {
            return plugin.Execute(method, parameters);
        }
        return null;
    }

    public List<string> GetLoadedPlugins() => _plugins.Keys.ToList();
}