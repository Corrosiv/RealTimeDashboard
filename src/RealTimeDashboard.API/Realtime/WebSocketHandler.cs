namespace RealTimeDashboard.API.Realtime;

using System.Net.WebSockets;
using System.Text;

public class WebSocketHandler
{
    private readonly WebSocketConnectionManager _manager;

    public WebSocketHandler(WebSocketConnectionManager manager)
    {
        _manager = manager;
    }

    public async Task HandleAsync(string id, WebSocket socket)
    {
        _manager.AddSocket(id, socket);

        var buffer = new byte[1024 * 4];
        while (socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(buffer: new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by client", CancellationToken.None);
                _manager.RemoveSocket(id);
            }
            else
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                // TODO: implement message routing and handling
                // - Deserialize incoming message envelope (type + payload)
                // - Validate message and actor (username)
                // - Route to appropriate handlers (e.g., CreateTransaction, Subscribe)
                // - Publish server events back to clients via WebSocketConnectionManager
                // Current placeholder avoids throwing to keep the connection stable.
            }
        }
    }
}
