namespace RealTimeDashboard.API.DTOs;

/// <summary>
/// Request model for creating a new transaction.
/// 
/// IMPORTANT: All timestamps are interpreted as UTC. If a client sends a local time or 
/// timezone-aware timestamp, it will be converted to UTC during canonicalization.
/// 
/// All values are validated and canonicalized (trimmed, normalized, rounded) after binding.
/// CreatedAt is always set by the server and cannot be provided by the client.
/// </summary>
public class CreateTransactionRequest
{
    /// <summary>
    /// When the transaction occurred (UTC). Format: ISO 8601 (e.g., "2026-04-07T15:00:00Z").
    /// Will be converted to UTC during canonicalization if provided with timezone info.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Transaction amount. Will be rounded to 2 decimal places (currency precision).
    /// Positive for expenses, negative for income per convention.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency code (ISO 4217 format, case-insensitive, e.g., "USD", "EUR").
    /// Will be normalized to uppercase during canonicalization.
    /// Must be a valid ISO 4217 code.
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// User-friendly transaction description (max 500 characters).
    /// Will be trimmed and have whitespace normalized during canonicalization.
    /// Control characters will be removed.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Category ID for grouping this transaction.
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Username of the user creating this transaction (max 100 characters).
    /// Will be trimmed and have whitespace normalized during canonicalization.
    /// </summary>
    public string CreatedBy { get; set; } = null!;

    /// <summary>
    /// Optional: Source/type of transaction (e.g., "Manual", "API", "CSV"). Max 50 characters.
    /// Defaults to "Manual" if not provided (set during domain entity creation).
    /// Will be trimmed and have whitespace normalized during canonicalization.
    /// </summary>
    public string? Source { get; set; }

    // NOTE: CreatedAt is deliberately NOT included in this DTO.
    // The server always sets CreatedAt to DateTime.UtcNow when persisting.
    // This prevents clients from manipulating transaction creation timestamps.
}

