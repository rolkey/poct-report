using Newtonsoft.Json.Linq;

namespace ExamplePlugin;

public class HelloPlugin : IPlugin
{
    public string Name => "HelloPlugin";
    public string Version => "1.0.0";

    public object? Execute(string method, object?[] parameters)
    {
        return method.ToLower() switch
        {
            "greet" => $"Hello, {parameters.FirstOrDefault() ?? "World"}!",
            "echo" => string.Join(" ", parameters.Select(p => p?.ToString() ?? "")),
            "add" => ((parameters[0] as JToken)?.Value<int>() ?? 0) + ((parameters[1] as JToken)?.Value<int>() ?? 0),
            _ => null
        };
    }
}