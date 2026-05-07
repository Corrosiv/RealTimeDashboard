using System.Text.RegularExpressions;
using RealTimeDashboard.API.DTOs;
using RealTimeDashboard.API.Infrastructure;

namespace RealTimeDashboard.API.Services;

/// <summary>
/// Service responsible for canonicalizing and normalizing API request data.
/// Runs AFTER validation to ensure requests are already structurally valid.
/// 
/// Canonicalization includes:
/// - DateTime normalization to UTC
/// - Currency code normalization (uppercase)
/// - Text field trimming and normalization
/// - Decimal precision rounding
/// - Setting server-controlled defaults
/// </summary>
public class RequestCanonicalizationService
{
    /// <summary>
    /// Canonicalizes a CreateTransactionRequest after validation.
    /// Modifies the request in-place to normalize all fields.
    /// </summary>
    public void CanonicalizeCreateTransactionRequest(CreateTransactionRequest request)
    {
        if (request == null)
            return;

        // 1. Normalize DateTime to UTC
        request.Timestamp = NormalizeDateTime(request.Timestamp);

        // 2. Normalize currency to uppercase ISO 4217
        request.Currency = CurrencyCodes.Normalize(request.Currency);

        // 3. Trim and sanitize text fields
        // Note: CreatedBy is guaranteed non-null by validator, so the ! is safe
        request.CreatedBy = SanitizeTextField(request.CreatedBy, "CreatedBy", 100) ?? request.CreatedBy;
        request.Description = SanitizeTextField(request.Description, "Description", 500);
        request.Source = SanitizeTextField(request.Source, "Source", 50);

        // 4. Round amount to 2 decimal places (currency precision)
        request.Amount = RoundToCurrencyPrecision(request.Amount);
    }

    /// <summary>
    /// Canonicalizes a TransactionQueryRequest after validation.
    /// Normalizes query parameters to semantic canonical form.
    /// </summary>
    public void CanonicalizeTransactionQueryRequest(TransactionQueryRequest request)
    {
        if (request == null)
            return;

        // 1. Normalize dates to UTC if they have timezone info
        if (request.DateFrom.HasValue)
            request.DateFrom = NormalizeDateTime(request.DateFrom.Value);

        if (request.DateTo.HasValue)
            request.DateTo = NormalizeDateTime(request.DateTo.Value);

        // 2. Trim and sanitize search text
        request.Search = SanitizeTextField(request.Search, "Search", 200);

        // 3. Normalize sort parameter (handled by validator, but ensure consistency)
        // Sort is already validated to be in format "timestamp:asc" or "timestamp:desc"
        if (!string.IsNullOrWhiteSpace(request.Sort))
            request.Sort = request.Sort.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Normalizes a DateTime to UTC.
    /// If the DateTime is in a different timezone, converts to UTC.
    /// If it's already UTC or has no timezone info, ensures it's marked as UTC.
    /// </summary>
    private DateTime NormalizeDateTime(DateTime dateTime)
    {
        // Convert to UTC if needed
        if (dateTime.Kind == DateTimeKind.Local)
            return dateTime.ToUniversalTime();

        if (dateTime.Kind == DateTimeKind.Unspecified)
            return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);

        // Already UTC
        return dateTime;
    }

    /// <summary>
    /// Sanitizes a text field by:
    /// - Trimming leading/trailing whitespace
    /// - Normalizing internal whitespace (multiple spaces → single space, preserve newlines in Description)
    /// - Removing control characters (except newlines in some fields)
    /// - Enforcing length limit
    /// </summary>
    private string? SanitizeTextField(string? input, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        // Trim whitespace (but preserve internal structure)
        var trimmed = input.Trim();

        // For Description, preserve newlines but normalize other whitespace
        // For other fields, collapse all whitespace to single spaces
        string normalized;
        if (fieldName == "Description")
        {
            // Normalize whitespace within lines, but preserve line breaks
            var lines = trimmed.Split('\n');
            var normalizedLines = lines.Select(line => Regex.Replace(line, @"\s+", " ").Trim()).ToArray();
            normalized = string.Join("\n", normalizedLines);
        }
        else
        {
            // Collapse all whitespace to single spaces (no newlines)
            normalized = Regex.Replace(trimmed, @"\s+", " ");
        }

        // Remove control characters (except newlines for Description field)
        if (fieldName == "Description")
        {
            // Allow newlines in description
            normalized = Regex.Replace(normalized, @"[\x00-\x08\x0B\x0C\x0E-\x1F]", string.Empty);
        }
        else
        {
            // Remove all control characters
            normalized = Regex.Replace(normalized, @"[\x00-\x1F]", string.Empty);
        }

        // Enforce length limit
        if (normalized.Length > maxLength)
            normalized = normalized.Substring(0, maxLength);

        return normalized.Length == 0 ? null : normalized;
    }

    /// <summary>
    /// Rounds a decimal amount to 2 decimal places (currency precision).
    /// Uses banker's rounding (round to nearest, ties to even).
    /// </summary>
    private decimal RoundToCurrencyPrecision(decimal amount)
    {
        return Math.Round(amount, 2, MidpointRounding.ToEven);
    }
}
