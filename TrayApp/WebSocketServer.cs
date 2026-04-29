using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<WebSocket, Task> _activeConnections = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();

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
            _ = Task.Run(() => AcceptConnections(_cancellationTokenSource.Token));
        }
        catch (Exception ex)
        {
            Logger.Error($"启动HttpListener失败: {ex}");
            throw;
        }
    }

    // public void Stop()
    // {
    //     _isRunning = false;
    //     _listener.Stop();
    //     Logger.Info("HttpListener 已停止");
    // }
    public async Task StopAsync()
    {
        _isRunning = false;
        
        // 关闭所有活动的WebSocket连接
        var closeTasks = new List<Task>();
        foreach (var connection in _activeConnections.Keys.ToList())
        {
            try
            {
                if (connection.State == WebSocketState.Open)
                {
                    closeTasks.Add(connection.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutting down", CancellationToken.None));
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"关闭WebSocket连接时出错: {ex.Message}");
            }
        }
        
        // 等待所有连接关闭
        if (closeTasks.Count > 0)
        {
            await Task.WhenAll(closeTasks);
        }
        
        // 取消所有待处理的任务
        _cancellationTokenSource.Cancel();
        
        // 停止监听器
        _listener.Stop();
        Logger.Info("HttpListener 已停止");
    }

    private async Task AcceptConnections(CancellationToken cancellationToken)
    {
        while (_isRunning && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                var context = await _listener.GetContextAsync().ConfigureAwait(false);
                if (context.Request.IsWebSocketRequest)
                {
                    Logger.Info("收到 WebSocket 连接请求");
                    _ = Task.Run(() => HandleWebSocket(context, cancellationToken), cancellationToken);
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

     private async Task HandleWebSocket(HttpListenerContext context, CancellationToken cancellationToken)
    {
        var wsContext = await context.AcceptWebSocketAsync(null).ConfigureAwait(false);
        var ws = wsContext.WebSocket;
        Logger.Info("WebSocket 连接已建立");
        
        // 添加到活动连接集合
        _activeConnections.TryAdd(ws, Task.CompletedTask);

        try
        {
            while (ws.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var buffer = new byte[4096];
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).ConfigureAwait(false);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Logger.Info("收到 WebSocket 关闭请求");
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).ConfigureAwait(false);
                    return;
                }

                var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Logger.Info($"收到命令: {json}");
                ProcessCommand(json, ws, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // 正常的取消操作，不需要记录错误
            Logger.Info("WebSocket 连接被取消");
        }
        catch (Exception ex)
        {
            Logger.Error("HandleWebSocket 异常", ex);
        }
        finally
        {
            // 从活动连接集合中移除
            _activeConnections.TryRemove(ws, out _);
            
            // 确保WebSocket被关闭
            if (ws.State == WebSocketState.Open)
            {
                try
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.Error($"关闭WebSocket连接时出错: {ex.Message}");
                }
            }
            
            Logger.Info("WebSocket 连接已关闭");
        }
    }

     private async void ProcessCommand(string json, WebSocket ws, CancellationToken cancellationToken)
    {
        try
        {
            var cmd = JObject.Parse(json);
            var pluginName = cmd["plugin"]?.ToString();
            var method = cmd["method"]?.ToString();
            var parameters = cmd["params"]?.ToArray<object>() ?? Array.Empty<object>();

            if (pluginName == "system" && method == "shutdown")
            {
                Logger.Info("收到 shutdown 命令");
                var resp = new { success = true, data = "shutdown" };
                var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(resp));
                await ws.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).ConfigureAwait(false);
                Task.Delay(100).ContinueWith(_ => App.RequestShutdown(), cancellationToken);
                return;
            }

            Logger.Info($"调用插件: {pluginName}, 方法: {method}");
            var result = _pluginManager.Invoke(pluginName ?? "", method ?? "", parameters);

            var response = new { success = true, data = result };
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response));
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
            Logger.Info($"返回结果: {JsonConvert.SerializeObject(response)}");
        }
        catch (OperationCanceledException)
        {
            // 正常的取消操作，不需要记录错误
            Logger.Info("ProcessCommand 被取消");
        }
        catch (Exception ex)
        {
            Logger.Error("处理命令异常", ex);
            var errorMsg = ex.Message ?? "Unknown error";
            var response = new { success = false, error = errorMsg };
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response));
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
            Logger.Info($"返回错误: {JsonConvert.SerializeObject(response)}");
        }
    }
}