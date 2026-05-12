using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RealTimeDashboard.Tests.Integration.Helpers;
using RealTimeDashboard.API.Realtime;
using FinanceTracker.Core.Domain;
using FinanceTracker.Core.Interfaces;

namespace RealTimeDashboard.Tests.Integration;

/// <summary>
/// Integration tests for WebSocket replay functionality.
/// Tests connection state management, replay result validation, and ordering guarantees.
/// </summary>
public class WebSocketReplayIntegrationTests : IAsyncLifetime
{
    private CustomWebApplicationFactory<Program> _factory = null!;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerOptions.Default)
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task InitializeAsync()
    {
        _factory = new CustomWebApplicationFactory<Program>();
        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
    }

    #region Replay Result & Error Handling

    [Fact]
    public void ReplayResult_WithSuccess_HasEvents()
    {
        // Arrange
        var events = new List<ActivityEvent>
        {
            new ActivityEvent
            {
                Id = 1,
                EventId = "event-1",
                SequenceId = 1,
                Type = "TestEvent",
                EntityId = 0,
                Scope = "test",
                Actor = new ActorInfo { Username = "user1", JoinedAt = DateTime.UtcNow },
                Message = "Test",
                CreatedAt = DateTime.UtcNow
            }
        };

        // Act
        var result = new ReplayResult { IsSuccess = true, Events = events };

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Events);
        Assert.Null(result.Error);
    }

    [Fact]
    public void ReplayResult_WithError_HasErrorMessage()
    {
        // Arrange
        var error = new WebSocketErrorMessage
        {
            Code = WebSocketErrorCodes.ReplayWindowExceeded,
            Message = "Replay window exceeded"
        };

        // Act
        var result = new ReplayResult { IsSuccess = false, Error = error };

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Empty(result.Events);
        Assert.NotNull(result.Error);
        Assert.Equal(WebSocketErrorCodes.ReplayWindowExceeded, result.Error.Code);
    }

    #endregion

    #region Per-Client Cursor Management

    [Fact]
    public void ClientConnection_TracksLastSeenEventId()
    {
        // Arrange
        var connectionManager = new WebSocketConnectionManager();
        var mockSocket = new Mock<WebSocket>();

        connectionManager.AddSocket("client-1", mockSocket.Object);

        // Act
        var connection = connectionManager.GetConnection("client-1");
        Assert.NotNull(connection);
        Assert.Equal(0, connection.LastSeenEventId); // Initially 0

        connectionManager.UpdateLastSeenEventId("client-1", 42);
        var lastSeen = connectionManager.GetLastSeenEventId("client-1");

        // Assert
        Assert.Equal(42, lastSeen);
    }

    [Fact]
    public void MultipleConnections_TrackIndependentCursors()
    {
        // Arrange
        var connectionManager = new WebSocketConnectionManager();
        var socket1 = new Mock<WebSocket>();
        var socket2 = new Mock<WebSocket>();

        connectionManager.AddSocket("client-1", socket1.Object);
        connectionManager.AddSocket("client-2", socket2.Object);

        // Act
        connectionManager.UpdateLastSeenEventId("client-1", 10);
        connectionManager.UpdateLastSeenEventId("client-2", 20);

        var cursor1 = connectionManager.GetLastSeenEventId("client-1");
        var cursor2 = connectionManager.GetLastSeenEventId("client-2");

        // Assert
        Assert.Equal(10, cursor1);
        Assert.Equal(20, cursor2);
    }

    [Fact]
    public void ClientConnection_TracksConnectionTime()
    {
        // Arrange
        var connectionManager = new WebSocketConnectionManager();
        var mockSocket = new Mock<WebSocket>();
        var beforeAdd = DateTime.UtcNow;

        // Act
        connectionManager.AddSocket("client-1", mockSocket.Object);
        var connection = connectionManager.GetConnection("client-1");
        var afterAdd = DateTime.UtcNow;

        // Assert
        Assert.NotNull(connection);
        Assert.True(connection.ConnectedAtUtc >= beforeAdd);
        Assert.True(connection.ConnectedAtUtc <= afterAdd);
    }

    #endregion

    #region Subscription State Management

    [Fact]
    public void ClientConnection_StartsUnsubscribed()
    {
        // Arrange
        var connectionManager = new WebSocketConnectionManager();
        var mockSocket = new Mock<WebSocket>();

        // Act
        connectionManager.AddSocket("client-1", mockSocket.Object);
        var connection = connectionManager.GetConnection("client-1");

        // Assert
        Assert.NotNull(connection);
        Assert.False(connection.IsSubscribed);
    }

    [Fact]
    public void ClientConnection_CanBeMarkedAsSubscribed()
    {
        // Arrange
        var connectionManager = new WebSocketConnectionManager();
        var mockSocket = new Mock<WebSocket>();

        connectionManager.AddSocket("client-1", mockSocket.Object);
        var connection = connectionManager.GetConnection("client-1");

        // Act
        connection!.IsSubscribed = true;

        // Assert
        Assert.True(connection.IsSubscribed);
    }

    #endregion

    #region Connection Lifecycle

    [Fact]
    public void AddSocket_StoresSocketInManager()
    {
        // Arrange
        var connectionManager = new WebSocketConnectionManager();
        var mockSocket = new Mock<WebSocket>();

        // Act
        connectionManager.AddSocket("client-1", mockSocket.Object);
        var allConnections = connectionManager.GetAll().ToList();

        // Assert
        Assert.Single(allConnections);
        Assert.Equal("client-1", allConnections[0].Key);
        Assert.Same(mockSocket.Object, allConnections[0].Value.Socket);
    }

    [Fact]
    public void RemoveSocket_DeletesConnection()
    {
        // Arrange
        var connectionManager = new WebSocketConnectionManager();
        var mockSocket = new Mock<WebSocket>();
        connectionManager.AddSocket("client-1", mockSocket.Object);

        // Act
        connectionManager.RemoveSocket("client-1");
        var allConnections = connectionManager.GetAll().ToList();

        // Assert
        Assert.Empty(allConnections);
    }

    [Fact]
    public void ReplaceSocket_OverwritesPreviousConnection()
    {
        // Arrange
        var connectionManager = new WebSocketConnectionManager();
        var socket1 = new Mock<WebSocket>();
        var socket2 = new Mock<WebSocket>();

        connectionManager.AddSocket("client-1", socket1.Object);
        connectionManager.UpdateLastSeenEventId("client-1", 100);

        // Act
        connectionManager.AddSocket("client-1", socket2.Object);
        var connection = connectionManager.GetConnection("client-1");

        // Assert
        Assert.NotNull(connection);
        Assert.Same(socket2.Object, connection.Socket);
        // Note: cursor is reset when socket is replaced
        Assert.Equal(0, connection.LastSeenEventId);
    }

    #endregion

    #region WebSocket Message Contracts

    [Fact]
    public void WebSocketMessage_SerializesCorrectly()
    {
        // Arrange
        var message = new WebSocketMessage
        {
            Type = "ActivityEvent",
            Payload = new ActivityEventMessage
            {
                EventId = 1,
                SequenceId = 1,
                Type = "TransactionCreated",
                OccurredAtUtc = DateTime.UtcNow,
                Actor = new ActorData { Username = "user1", DisplayName = "User One" },
                Message = "Created transaction",
                Metadata = null
            }
        };

        // Act
        var json = JsonSerializer.Serialize(message, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var deserialized = JsonSerializer.Deserialize<WebSocketMessage>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal("ActivityEvent", deserialized.Type);
        Assert.NotNull(deserialized.Payload);
    }

    [Fact]
    public void ReplayStartedMessage_ContainsFromEventId()
    {
        // Arrange & Act
        var message = new ReplayStartedMessage { FromEventId = 10 };

        // Assert
        Assert.Equal(10, message.FromEventId);
    }

    [Fact]
    public void ReplayCompletedMessage_ContainsLastEventId()
    {
        // Arrange & Act
        var message = new ReplayCompletedMessage { LastEventId = 20 };

        // Assert
        Assert.Equal(20, message.LastEventId);
    }

    [Fact]
    public void WebSocketErrorMessage_ContainsCode()
    {
        // Arrange & Act
        var error = new WebSocketErrorMessage
        {
            Code = WebSocketErrorCodes.ReplayWindowExceeded,
            Message = "Window exceeded",
            Details = new Dictionary<string, object> { { "age", 120 } }
        };

        // Assert
        Assert.Equal(WebSocketErrorCodes.ReplayWindowExceeded, error.Code);
        Assert.Equal("Window exceeded", error.Message);
        Assert.Single(error.Details);
    }

    #endregion

    #region Replay Options Configuration

    [Fact]
    public void WebSocketReplayOptions_HasDefaults()
    {
        // Arrange & Act
        var options = new WebSocketReplayOptions();

        // Assert
        Assert.Equal(10000, options.MaxReplayEvents);
        Assert.Equal(60, options.MaxReplayAgeMinutes);
        Assert.Equal(TimeSpan.FromMinutes(60), options.MaxReplayAge);
    }

    [Fact]
    public void WebSocketReplayOptions_CanBeConfigured()
    {
        // Arrange & Act
        var options = new WebSocketReplayOptions
        {
            MaxReplayEvents = 500,
            MaxReplayAgeMinutes = 30
        };

        // Assert
        Assert.Equal(500, options.MaxReplayEvents);
        Assert.Equal(30, options.MaxReplayAgeMinutes);
        Assert.Equal(TimeSpan.FromMinutes(30), options.MaxReplayAge);
    }

    #endregion
}

