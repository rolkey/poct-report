using System.Net;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TrayApp.Plugins;

namespace TrayApp;

public class WebSocketServer
{
    private readonly HttpListener _listener;
    private readonly PluginManager _pluginManager;
    private bool _isRunning;
    private readonly int _port;

    public WebSocketServer(int port, PluginManager pluginManager)
    {
        _port = port;
        _pluginManager = pluginManager;
        _listener = new HttpListener();

        // Windows 用 localhost 或 127.0.0.1 (无需管理员)，Linux 用 0.0.0.0
        var host = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "localhost" : "0.0.0.0";
        var prefix = $"http://{host}:{port}/";

        _listener.Prefixes.Add(prefix);
        Logger.Info($"HttpListener 前缀: {prefix}");
    }

    public void Start()
    {
        try
        {
            _isRunning = true;
            _listener.Start();
            Logger.Info($"HttpListener 已启动，监听端口 {_port}");
            _ = Task.Run(() => AcceptConnections());
        }
        catch (Exception ex)
        {
            Logger.Error($"启动HttpListener失败: {ex}");
            throw;
        }
    }

    public void Stop()
    {
        _isRunning = false;
        _listener.Stop();
        Logger.Info("HttpListener 已停止");
    }

    private async Task AcceptConnections()
    {
        while (_isRunning)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                if (context.Request.IsWebSocketRequest)
                {
                    Logger.Info("收到 WebSocket 连接请求");
                    _ = Task.Run(() => HandleWebSocket(context));
                }
                else
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                if (_isRunning)
                {
                    Logger.Error("AcceptConnections 异常", ex);
                }
                break;
            }
        }
    }

    private async Task HandleWebSocket(HttpListenerContext context)
    {
        var wsContext = await context.AcceptWebSocketAsync(null);
        var ws = wsContext.WebSocket;
        Logger.Info("WebSocket 连接已建立");

        try
        {
            while (ws.State == WebSocketState.Open)
            {
                var buffer = new byte[4096];
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Logger.Info("收到 WebSocket 关闭请求");
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    return;
                }

                var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Logger.Info($"收到命令: {json}");
                ProcessCommand(json, ws);
            }
        }
        catch (Exception ex)
        {
            Logger.Error("HandleWebSocket 异常", ex);
        }

        Logger.Info("WebSocket 连接已关闭");
    }

    private async void ProcessCommand(string json, WebSocket ws)
    {
        try
        {
            var cmd = JObject.Parse(json);
            var pluginName = cmd["plugin"]?.ToString();
            var method = cmd["method"]?.ToString();
            var parameters = cmd["parameters"]?.ToArray<object>() ?? Array.Empty<object>();

            Logger.Info($"调用插件: {pluginName}, 方法: {method}");
            var result = _pluginManager.Invoke(pluginName ?? "", method ?? "", parameters);

            var response = new { success = true, data = result };
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response));
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
            Logger.Info($"返回结果: {JsonConvert.SerializeObject(response)}");
        }
        catch (Exception ex)
        {
            Logger.Error("处理命令异常", ex);
            var response = new { success = false, error = ex.Message };
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response));
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}