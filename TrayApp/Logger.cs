using System.IO;

namespace TrayApp;

public static class Logger
{
    private static string GetLogFile() => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"TrayApp_{DateTime.Now:yyyyMMdd}.log");

    public static void Info(string message) => Log("INFO", message);
    public static void Warn(string message) => Log("WARN", message);
    public static void Error(string message) => Log("ERROR", message);
    public static void Error(string message, Exception ex) => Log("ERROR", $"{message}: {ex}");

    private static void Log(string level, string message)
    {
        try
        {
            var log = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
            File.AppendAllText(GetLogFile(), log + Environment.NewLine);
        }
        catch { }
    }
}