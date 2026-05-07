using System.Text;
using FinanceTracker.Core.Domain;

namespace RealTimeDashboard.API.Services;

/// <summary>
/// Service for encoding/decoding cursors used in cursor-based pagination.
/// A cursor represents a position (timestamp + id) for stable, efficient pagination.
/// </summary>
public class CursorService
{
    private const char Separator = '|';

    /// <summary>
    /// Encodes a transaction's position into a base64 cursor string.
    /// Format: base64(timestamp|id)
    /// </summary>
    public string EncodeCursor(DateTime timestamp, int id)
    {
        var cursorValue = $"{timestamp:O}{Separator}{id}";
        var bytes = Encoding.UTF8.GetBytes(cursorValue);
        return Convert.ToBase64String(bytes);
    }

    /// <summary>
    /// Decodes a base64 cursor string back into timestamp and id.
    /// Returns null if cursor is invalid.
    /// </summary>
    public (DateTime Timestamp, int Id)? DecodeCursor(string cursor)
    {
        try
        {
            var bytes = Convert.FromBase64String(cursor);
            var cursorValue = Encoding.UTF8.GetString(bytes);
            var parts = cursorValue.Split(Separator);

            if (parts.Length != 2)
                return null;

            if (!DateTime.TryParse(parts[0], null, System.Globalization.DateTimeStyles.RoundtripKind, out var timestamp) || !int.TryParse(parts[1], out var id))
                return null;

            return (timestamp, id);
        }
        catch
        {
            return null;
        }
    }
}
