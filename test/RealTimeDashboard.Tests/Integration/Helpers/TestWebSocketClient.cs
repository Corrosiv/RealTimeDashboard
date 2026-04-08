using System;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RealTimeDashboard.Tests.Integration.Helpers;

public class TestWebSocketClient : IDisposable
{
    private readonly ClientWebSocket _socket = new();
    private readonly Uri _uri;

    public TestWebSocketClient(string uri)
    {
        _uri = new Uri(uri);
    }

    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        await _socket.ConnectAsync(_uri, cancellationToken);
    }

    public async Task SendAsync(string message, CancellationToken cancellationToken = default)
    {
        var buffer = Encoding.UTF8.GetBytes(message);
        await _socket.SendAsync(buffer: new ArraySegment<byte>(buffer), messageType: WebSocketMessageType.Text, endOfMessage: true, cancellationToken: cancellationToken);
    }

    public async Task<string?> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        var buffer = new byte[4096];
        var result = await _socket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
        if (result.MessageType == WebSocketMessageType.Close)
        {
            await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", cancellationToken);
            return null;
        }

        return Encoding.UTF8.GetString(buffer, 0, result.Count);
    }

    public void Dispose()
    {
        _socket?.Dispose();
    }
}
