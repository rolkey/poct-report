namespace ExamplePlugin;

public class ExamplePlugin
{
    public string greet(string name)
    {
        return $"Hello, {name}!";
    }

    public string echo(string message)
    {
        return message;
    }

    public int add(int a, int b)
    {
        return a + b;
    }
}