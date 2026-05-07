namespace RealTimeDashboard.API.DTOs;

/// <summary>
/// Standardized API error response for all error cases.
/// </summary>
public class ApiErrorResponse
{
    /// <summary>
    /// Error type/code for programmatic handling.
    /// </summary>
    public string Code { get; set; } = null!;

    /// <summary>
    /// Human-friendly error message.
    /// </summary>
    public string Message { get; set; } = null!;

    /// <summary>
    /// HTTP status code.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Field-level validation errors (if applicable).
    /// Key: field name, Value: list of error messages.
    /// </summary>
    public Dictionary<string, List<string>>? Errors { get; set; }

    /// <summary>
    /// Timestamp when the error occurred (UTC).
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
