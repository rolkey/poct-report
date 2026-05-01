using System.IO;
using System.Reflection;

namespace TrayApp.Plugins;

public class PluginManager
{
    private readonly Dictionary<string, object> _pluginInstances = new();
    private readonly string[] _pluginDirs = ["designPlugs", "plugins"];

    public object? Invoke(string pluginName, string method, object?[] parameters)
    {
        if (!_pluginInstances.TryGetValue(pluginName, out var instance))
        {
            Logger.Info($"插件 {pluginName} 未加载，尝试动态加载...");
            instance = TryLoadPlugin(pluginName);

            if (instance == null)
            {
                Logger.Warn($"插件 {pluginName} 加载失败");
                throw new Exception($"Plugin not loaded: {pluginName}");
            }

            _pluginInstances[pluginName] = instance;
        }

        try
        {
            var type = instance.GetType();
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                .Where(m => m.Name == method)
                .ToList();

            if (methods.Count == 0)
            {
                Logger.Warn($"插件 {pluginName} 中未找到方法: {method}");
                throw new Exception($"Method not found: {pluginName}.{method}");
            }

            MethodInfo? targetMethod = null;
            object?[]? convertedParams = null;

            foreach (var mi in methods)
            {
                var pis = mi.GetParameters();
                int paramCount = parameters?.Length ?? 0;
                int requiredCount = pis.Count(p => !p.IsOptional);
                if (paramCount < requiredCount || paramCount > pis.Length)
                    continue;

                var converted = new object?[pis.Length];
                bool match = true;

                for (int i = 0; i < paramCount; i++)
                {
                    if (parameters[i] == null)
                    {
                        if (pis[i].ParameterType.IsValueType && Nullable.GetUnderlyingType(pis[i].ParameterType) == null)
                        {
                            match = false;
                            break;
                        }
                        converted[i] = null;
                        continue;
                    }

                    try
                    {
                        var raw = parameters[i];
                        if (raw is Newtonsoft.Json.Linq.JToken jt)
                            raw = ((Newtonsoft.Json.Linq.JToken)raw).ToString();

                        converted[i] = Convert.ChangeType(raw, pis[i].ParameterType);
                    }
                    catch
                    {
                        match = false;
                        break;
                    }
                }

                // 填充可选参数的默认值
                for (int i = paramCount; i < pis.Length; i++)
                {
                    var defaultVal = pis[i].DefaultValue;
                    converted[i] = defaultVal == DBNull.Value ? null : defaultVal;
                }

                if (match)
                {
                    targetMethod = mi;
                    convertedParams = converted;
                    break;
                }
            }

            if (targetMethod == null)
            {
                Logger.Warn($"插件 {pluginName}.{method} 参数不匹配");
                throw new Exception($"Parameter mismatch: {pluginName}.{method}");
            }

            var result = targetMethod.Invoke(instance, convertedParams);
            Logger.Info($"插件 {pluginName}.{method} 执行成功");
            return result;
        }
        catch (Exception ex)
        {
            Logger.Error($"插件 {pluginName}.{method} 执行失败", ex);
            throw;
        }
    }

    private object? TryLoadPlugin(string pluginName)
    {
        Logger.Info($"CurrentDirectory: {Environment.CurrentDirectory}");
        Logger.Info($"BaseDirectory: {AppDomain.CurrentDomain.BaseDirectory}");

        // 按优先级依次搜索 designPlugs → plugins
        foreach (var dir in _pluginDirs)
        {
            if (!Directory.Exists(dir))
            {
                Logger.Info($"插件目录不存在: {dir}，跳过");
                continue;
            }

            var dllPath = Path.Combine(dir, $"{pluginName}.dll");
            if (!File.Exists(dllPath))
            {
                Logger.Info($"插件 DLL 不存在: {dllPath}，跳过");
                continue;
            }

            Logger.Info($"从目录加载插件: {dir}");

            try
            {
                var assembly = Assembly.LoadFrom(dllPath);
                var expectedTypeName = $"{pluginName}.{pluginName}";
                var type = assembly.GetTypes()
                    .FirstOrDefault(t => t.FullName == expectedTypeName && t.IsClass && !t.IsAbstract);

                if (type == null)
                {
                    Logger.Warn($"DLL 中未找到类: {expectedTypeName}");
                    continue;
                }

                var instance = Activator.CreateInstance(type);
                Logger.Info($"动态加载插件成功: {pluginName} (来自 {dir})");
                return instance;
            }
            catch (Exception ex)
            {
                Logger.Error($"加载 DLL 失败: {dllPath}", ex);
                continue;
            }
        }

        Logger.Warn($"所有目录均未找到插件: {pluginName}");
        return null;
    }

    public List<string> GetLoadedPlugins() => _pluginInstances.Keys.ToList();
}