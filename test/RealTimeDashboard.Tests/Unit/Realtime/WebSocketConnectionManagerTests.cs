using Xunit;
using Moq;
using System.Net.WebSockets;
using RealTimeDashboard.API.Realtime;

namespace RealTimeDashboard.Tests.Unit;

/// <summary>
/// Light unit tests for WebSocketConnectionManager.
/// Tests connection lifecycle and socket management.
/// </summary>
public class WebSocketConnectionManagerTests
{
    private readonly WebSocketConnectionManager _manager = new();

    [Fact]
    public void AddSocket_StoresSocket()
    {
        // Arrange
        var mockSocket = new Mock<WebSocket>();
        var connectionId = "client-1";

        // Act
        _manager.AddSocket(connectionId, mockSocket.Object);
        var sockets = _manager.GetAll().ToList();

        // Assert
        Assert.Single(sockets);
        Assert.Equal(connectionId, sockets[0].Key);
        Assert.Same(mockSocket.Object, sockets[0].Value);
    }

    [Fact]
    public void AddSocket_WithMultipleConnections_StoresAll()
    {
        // Arrange
        var socket1 = new Mock<WebSocket>().Object;
        var socket2 = new Mock<WebSocket>().Object;
        var socket3 = new Mock<WebSocket>().Object;

        // Act
        _manager.AddSocket("client-1", socket1);
        _manager.AddSocket("client-2", socket2);
        _manager.AddSocket("client-3", socket3);

        // Assert
        var sockets = _manager.GetAll().ToList();
        Assert.Equal(3, sockets.Count);
    }

    [Fact]
    public void RemoveSocket_RemovesConnection()
    {
        // Arrange
        var mockSocket = new Mock<WebSocket>();
        _manager.AddSocket("client-1", mockSocket.Object);

        // Act
        _manager.RemoveSocket("client-1");

        // Assert
        Assert.Empty(_manager.GetAll());
    }

    [Fact]
    public void RemoveSocket_WithMultipleConnections_RemovesOnlyTarget()
    {
        // Arrange
        var socket1 = new Mock<WebSocket>().Object;
        var socket2 = new Mock<WebSocket>().Object;
        _manager.AddSocket("client-1", socket1);
        _manager.AddSocket("client-2", socket2);

        // Act
        _manager.RemoveSocket("client-1");

        // Assert
        var remaining = _manager.GetAll().ToList();
        Assert.Single(remaining);
        Assert.Equal("client-2", remaining[0].Key);
    }

    [Fact]
    public void RemoveSocket_WithNonExistentId_DoesNotThrow()
    {
        // Act & Assert: Should not throw
        _manager.RemoveSocket("non-existent");
    }

    [Fact]
    public void GetAll_ReturnsEmptyWhenNoConnections()
    {
        // Act
        var sockets = _manager.GetAll();

        // Assert
        Assert.Empty(sockets);
    }

    [Fact]
    public void AddSocket_OverwritesPreviousSocket()
    {
        // Arrange
        var socket1 = new Mock<WebSocket>().Object;
        var socket2 = new Mock<WebSocket>().Object;
        _manager.AddSocket("client-1", socket1);

        // Act
        _manager.AddSocket("client-1", socket2);

        // Assert
        var sockets = _manager.GetAll().ToList();
        Assert.Single(sockets);
        Assert.Same(socket2, sockets[0].Value);
    }

    [Fact]
    public void MultipleAddRemove_MaintainsConsistentState()
    {
        // Arrange & Act
        var sockets = new List<WebSocket>
        {
            new Mock<WebSocket>().Object,
            new Mock<WebSocket>().Object,
            new Mock<WebSocket>().Object
        };

        for (int i = 0; i < sockets.Count; i++)
        {
            _manager.AddSocket($"client-{i}", sockets[i]);
        }

        _manager.RemoveSocket("client-1");
        _manager.AddSocket("client-new", new Mock<WebSocket>().Object);

        // Assert
        var remaining = _manager.GetAll().ToList();
        Assert.Equal(3, remaining.Count);
        Assert.DoesNotContain(remaining, s => s.Key == "client-1");
        Assert.Contains(remaining, s => s.Key == "client-new");
    }
}
