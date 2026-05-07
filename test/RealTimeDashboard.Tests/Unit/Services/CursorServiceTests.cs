using System;
using Xunit;
using RealTimeDashboard.API.Services;

namespace RealTimeDashboard.Tests.Unit.Services;

/// <summary>
/// Unit tests for CursorService (cursor encoding/decoding).
/// </summary>
public class CursorServiceTests
{
    private readonly CursorService _service = new();

    [Fact]
    public void EncodeCursor_ValidTimestampAndId_EncodesSuccessfully()
    {
        var timestamp = new DateTime(2026, 4, 7, 15, 0, 0, DateTimeKind.Utc);
        var id = 123;

        var cursor = _service.EncodeCursor(timestamp, id);

        Assert.NotNull(cursor);
        Assert.NotEmpty(cursor);
    }

    [Fact]
    public void DecodeCursor_ValidCursor_DecodesSuccessfully()
    {
        var originalTimestamp = new DateTime(2026, 4, 7, 15, 0, 0, DateTimeKind.Utc);
        var originalId = 123;
        var cursor = _service.EncodeCursor(originalTimestamp, originalId);

        var decoded = _service.DecodeCursor(cursor);

        Assert.True(decoded.HasValue);
        Assert.Equal(originalTimestamp, decoded.Value.Timestamp);
        Assert.Equal(originalId, decoded.Value.Id);
    }

    [Fact]
    public void DecodeCursor_InvalidBase64_ReturnsNull()
    {
        var cursor = "not-valid-base64!!!";

        var decoded = _service.DecodeCursor(cursor);

        Assert.Null(decoded);
    }

    [Fact]
    public void DecodeCursor_InvalidFormat_ReturnsNull()
    {
        var validBase64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("invalid:format:structure"));

        var decoded = _service.DecodeCursor(validBase64);

        Assert.Null(decoded);
    }

    [Fact]
    public void EncodeDecode_RoundTrip_MaintainsValue()
    {
        var originalTimestamp = DateTime.UtcNow;
        var originalId = 456;

        var encoded = _service.EncodeCursor(originalTimestamp, originalId);
        var decoded = _service.DecodeCursor(encoded);

        Assert.True(decoded.HasValue);
        Assert.Equal(originalTimestamp, decoded.Value.Timestamp);
        Assert.Equal(originalId, decoded.Value.Id);
    }
}
