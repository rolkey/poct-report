using System.Windows;
using SystemTray = Hardcodet.Wpf.TaskbarNotification;
using TrayApp.Plugins;

namespace TrayApp;

public partial class App : Application
{
    private SystemTray.TaskbarIcon? _notifyIcon;
    private WebSocketServer? _wsServer;
    private PluginManager? _pluginManager;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _pluginManager = new PluginManager();
        _pluginManager.LoadPlugins();

        _notifyIcon = new SystemTray.TaskbarIcon
        {
            ToolTipText = "TrayApp - 托盘程序"
        };

        var menu = new System.Windows.Controls.ContextMenu();
        menu.Items.Add(new System.Windows.Controls.MenuItem { Header = "显示状态", Command = new RelayCommand(_ => ShowStatus()) });
        menu.Items.Add(new System.Windows.Controls.Separator());
        menu.Items.Add(new System.Windows.Controls.MenuItem { Header = "退出", Command = new RelayCommand(_ => ExitApplication()) });
        _notifyIcon.ContextMenu = menu;
        _notifyIcon.TrayMouseDoubleClick += (s, ev) => ShowStatus();

        _wsServer = new WebSocketServer(8761, _pluginManager);
        _wsServer.Start();
    }

    private void ShowStatus()
    {
        var plugins = _pluginManager?.GetLoadedPlugins() ?? new List<string>();
        System.Windows.MessageBox.Show(
            $"WebSocket服务: 端口 8761\n已加载插件: {plugins.Count}\n{string.Join("\n", plugins)}",
            "TrayApp 状态",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void ExitApplication()
    {
        _wsServer?.Stop();
        _notifyIcon?.Dispose();
        Shutdown();
    }
}

public class RelayCommand : System.Windows.Input.ICommand
{
    private readonly Action<object?> _execute;
    public RelayCommand(Action<object?> execute) => _execute = execute;
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => _execute(parameter);
}