namespace ExamplePlugin;

public interface IPlugin
{
    string Name { get; }
    string Version { get; }
    object? Execute(string method, object?[] parameters);
}