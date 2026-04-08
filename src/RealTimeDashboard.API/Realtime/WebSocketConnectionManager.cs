namespace RealTimeDashboard.API.Realtime;

using System.Collections.Concurrent;
using System.Net.WebSockets;

public class WebSocketConnectionManager
{
    private readonly ConcurrentDictionary<string, WebSocket> _sockets = new();

    public void AddSocket(string id, WebSocket socket) => _sockets[id] = socket;
    public void RemoveSocket(string id) => _sockets.TryRemove(id, out _);
    public IEnumerable<KeyValuePair<string, WebSocket>> GetAll() => _sockets;
}
