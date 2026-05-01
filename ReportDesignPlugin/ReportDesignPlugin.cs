using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using FastReport;
using Newtonsoft.Json;

namespace ReportDesignPlugin;

public class ReportDesignPlugin
{
    private static string? _templateDirectoryOverride;

    public string GetInfo() => "ReportDesignPlugin v1.0 - FastReport 报表设计器 (仅 Windows)";

    public bool IsDesignerAvailable() => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public static string TemplateDirectory { get; set; } = GetDefaultTemplateDirectory();

    private static string GetDefaultTemplateDirectory()
    {
        var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var baseDir = string.IsNullOrEmpty(assemblyLocation)
            ? Directory.GetCurrentDirectory()
            : Path.GetDirectoryName(assemblyLocation)!;
        return Path.Combine(baseDir, "templates");
    }

    public string DesignReport(string reportFilePath)
    {
        var filePath = Path.Combine(TemplateDirectory, reportFilePath);
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return ErrorResult("设计器仅在 Windows 上可用");

        if (!File.Exists(filePath))
            return ErrorResult($"报表文件不存在: {filePath}");

        try
        {
            var report = new Report();
            report.Load(filePath);
            var result = ShowDesignerDialog(report, filePath);
            return SuccessResult(result);
        }
        catch (Exception ex)
        {
            return ErrorResult($"设计器打开失败: {ex.Message}");
        }
    }

    public string CreateNewReport()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return ErrorResult("设计器仅在 Windows 上可用");

        try
        {
            var report = new Report();
            var result = ShowDesignerDialog(report, null);
            return SuccessResult(result);
        }
        catch (Exception ex)
        {
            return ErrorResult($"设计器打开失败: {ex.Message}");
        }
    }

    public string[] GetReportTemplates()
    {
        var templateDir = GetTemplateDirectory();
        if (!Directory.Exists(templateDir))
            return [];

        return Directory.GetFiles(templateDir, "*.frx")
            .Select(Path.GetFileName)
            .ToArray()!;
    }

    public string GetTemplateDirectoryPath() => GetTemplateDirectory();

    public void SetTemplateDirectoryPath(string path) => _templateDirectoryOverride = path;

    private static string GetTemplateDirectory()
    {
        if (_templateDirectoryOverride != null)
            return _templateDirectoryOverride;

        var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var baseDir = string.IsNullOrEmpty(assemblyLocation)
            ? Directory.GetCurrentDirectory()
            : Path.GetDirectoryName(assemblyLocation)!;
        return Path.Combine(baseDir, "templates");
    }

    private static string SuccessResult((bool saved, string? filePath, string? reportXml) result)
    {
        return JsonConvert.SerializeObject(new
        {
            saved = result.saved,
            filePath = result.filePath,
            reportXml = result.reportXml,
            message = result.saved ? "报表已保存" : "设计器已关闭，未保存"
        });
    }

    private static string ErrorResult(string message)
    {
        return JsonConvert.SerializeObject(new
        {
            saved = false,
            filePath = (string?)null,
            reportXml = (string?)null,
            message
        });
    }

    private static (bool saved, string? filePath, string? reportXml) ShowDesignerDialog(Report report, string? initialFilePath)
    {
        // TrayApp 是 WPF 应用，WebSocket 在 ThreadPool 线程上处理命令。
        // WPF 的 UI 操作（包括 DesignerControl 这个 WinForms 控件）
        // 必须在 STA 线程上运行。
        //
        // 方案：在新 STA 线程上启动设计器，用 thread.Join() 同步等待。
        // 这会在设计器打开期间阻塞 WebSocket 的线程池线程，
        // 但这是必要的——设计器是模态的，同一连接的其他命令不应并发执行。
        //
        // 注意：不要在 WebSocket 线程上直接 new Window()，
        // 因为 ThreadPool 线程是 MTA，WPF/WinForms 窗口需要 STA。

        bool saved = false;
        string? filePath = null;
        string? reportXml = null;

        var thread = new Thread(() =>
        {
            try
            {
                var window = new DesignerWindow();
                window.LoadReport(report, initialFilePath);
                window.Closed += (_, _) =>
                {
                    saved = window.SavedFilePath != null;
                    filePath = window.SavedFilePath;
                    reportXml = window.SavedReportXml;
                };
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"设计器异常: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        return (saved, filePath, reportXml);
    }
}
