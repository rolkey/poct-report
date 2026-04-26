using System.Drawing;
using System.Windows;
using SystemTray = Hardcodet.Wpf.TaskbarNotification;
using TrayApp.Plugins;

namespace TrayApp;

public partial class App : Application
{
    private SystemTray.TaskbarIcon? _notifyIcon;
    private WebSocketServer? _wsServer;
    private PluginManager? _pluginManager;
    private Window? _mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Logger.Info("应用启动");

        AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
        {
            Logger.Error("UnhandledException", ex.ExceptionObject as Exception ?? new Exception(ex.ExceptionObject.ToString()));
        };
        DispatcherUnhandledException += (s, ex) =>
        {
            Logger.Error("DispatcherUnhandledException", ex.Exception);
            ex.Handled = true;
        };

        try
        {
            _mainWindow = new Window
            {
                Width = 0,
                Height = 0,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false,
                ShowActivated = false,
                Visibility = Visibility.Hidden
            };
            _mainWindow.Show();
            Logger.Info("主窗口创建成功");

            _pluginManager = new PluginManager();
            Logger.Info("PluginManager 初始化完成，按需动态加载");

            using var icon = SystemIcons.Application;
            _notifyIcon = new SystemTray.TaskbarIcon
            {
                Icon = icon,
                ToolTipText = "TrayApp - 托盘程序"
            };
            Logger.Info("托盘图标创建成功");

            var menu = new System.Windows.Controls.ContextMenu();

            var showStatusItem = new System.Windows.Controls.MenuItem { Header = "显示状态" };
            showStatusItem.Click += ShowStatusItem_Click;
            menu.Items.Add(showStatusItem);

            menu.Items.Add(new System.Windows.Controls.Separator());

            var exitItem = new System.Windows.Controls.MenuItem { Header = "退出" };
            exitItem.Click += ExitItem_Click;
            menu.Items.Add(exitItem);

            _notifyIcon.ContextMenu = menu;
            _notifyIcon.TrayMouseDoubleClick += (s, ev) => ShowStatusItem_Click(s, new RoutedEventArgs());
            Logger.Info("菜单创建成功");

            _wsServer = new WebSocketServer(8761, _pluginManager);
            _wsServer.Start();
            Logger.Info("WebSocket服务启动，端口 8761");
        }
        catch (Exception ex)
        {
            Logger.Error("启动失败", ex);
            MessageBox.Show($"启动失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            Environment.Exit(1);
        }
    }

    private void ShowStatusItem_Click(object sender, RoutedEventArgs e)
    {
        Logger.Info("点击显示状态");
        var plugins = _pluginManager?.GetLoadedPlugins() ?? new List<string>();
        MessageBox.Show(
            _mainWindow,
            $"WebSocket服务: 端口 8761\n已加载插件: {plugins.Count}\n{string.Join("\n", plugins)}",
            "TrayApp 状态",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void ExitItem_Click(object sender, RoutedEventArgs e)
    {
        Logger.Info("点击退出菜单");
        ExitApplication();
    }

    private void ExitApplication()
    {
        Logger.Info("开始退出应用");
        try
        {
            _wsServer?.Stop();
            Logger.Info("WebSocket服务已停止");
        }
        catch (Exception ex)
        {
            Logger.Error("停止WebSocket服务失败", ex);
        }

        if (_notifyIcon != null)
        {
            _notifyIcon.Dispose();
            _notifyIcon = null;
            Logger.Info("托盘图标已释放");
        }

        _mainWindow?.Close();
        Logger.Info("主窗口已关闭");
        Environment.Exit(0);
    }
}