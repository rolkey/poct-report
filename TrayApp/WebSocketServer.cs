using System.Net;
using System.Net.WebSockets;
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

    public WebSocketServer(int port, PluginManager pluginManager)
    {
        _pluginManager = pluginManager;
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://+:{port}/");
    }

    public void Start()
    {
        _isRunning = true;
        _listener.Start();
        _ = Task.Run(() => AcceptConnections());
    }

    public void Stop()
    {
        _isRunning = false;
        _listener.Stop();
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
                    _ = Task.Run(() => HandleWebSocket(context));
                }
                else
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
            catch { break; }
        }
    }

    private async Task HandleWebSocket(HttpListenerContext context)
    {
        var wsContext = await context.AcceptWebSocketAsync(null);
        var ws = wsContext.WebSocket;

        while (ws.State == WebSocketState.Open)
        {
            var buffer = new byte[4096];
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                return;
            }

            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            ProcessCommand(json, ws);
        }
    }

    private async void ProcessCommand(string json, WebSocket ws)
    {
        try
        {
            var cmd = JObject.Parse(json);
            var pluginName = cmd["plugin"]?.ToString();
            var method = cmd["method"]?.ToString();
            var parameters = cmd["parameters"]?.ToArray<object>() ?? Array.Empty<object>();

            var result = _pluginManager.Invoke(pluginName ?? "", method ?? "", parameters);

            var response = new { success = true, data = result };
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response));
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (Exception ex)
        {
            var response = new { success = false, error = ex.Message };
            var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(response));
            await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }
}