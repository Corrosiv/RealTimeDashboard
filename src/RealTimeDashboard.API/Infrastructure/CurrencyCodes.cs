namespace RealTimeDashboard.API.Infrastructure;

/// <summary>
/// ISO 4217 currency codes.
/// Reference: https://en.wikipedia.org/wiki/ISO_4217
/// 
/// This list includes all official ISO 4217 three-letter codes for currencies in use.
/// Updates should be made as new currencies are added or deprecated.
/// </summary>
public static class CurrencyCodes
{
    private static readonly HashSet<string> ValidCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        // Major Currencies
        "USD", // US Dollar
        "EUR", // Euro
        "GBP", // British Pound
        "JPY", // Japanese Yen
        "CHF", // Swiss Franc
        "CAD", // Canadian Dollar
        "AUD", // Australian Dollar
        "NZD", // New Zealand Dollar
        "CNY", // Chinese Yuan
        "INR", // Indian Rupee
        "KRW", // South Korean Won
        "SGD", // Singapore Dollar
        "HKD", // Hong Kong Dollar
        "SEK", // Swedish Krona
        "NOK", // Norwegian Krone
        "DKK", // Danish Krone
        "MXN", // Mexican Peso
        "BRL", // Brazilian Real
        "RUB", // Russian Ruble
        "ZAR", // South African Rand
        "TRY", // Turkish Lira

        // Additional Major Currencies
        "THB", // Thai Baht
        "MYR", // Malaysian Ringgit
        "PHP", // Philippine Peso
        "IDR", // Indonesian Rupiah
        "VND", // Vietnamese Dong
        "PLN", // Polish Zloty
        "CZK", // Czech Koruna
        "HUF", // Hungarian Forint
        "RON", // Romanian Leu
        "BGN", // Bulgarian Lev
        "HRK", // Croatian Kuna
        "ISK", // Icelandic Króna
        "ILS", // Israeli New Shekel
        "SAR", // Saudi Arabian Riyal
        "AED", // United Arab Emirates Dirham
        "QAR", // Qatari Riyal
        "KWD", // Kuwaiti Dinar
        "BHD", // Bahraini Dinar
        "OMR", // Omani Rial
        "JOD", // Jordanian Dinar
        "LBP", // Lebanese Pound
        "EGP", // Egyptian Pound
        "PKR", // Pakistani Rupee
        "BDT", // Bangladeshi Taka
        "LKR", // Sri Lankan Rupee
        "TND", // Tunisian Dinar
        "NGN", // Nigerian Naira
        "GHS", // Ghanaian Cedi
        "KES", // Kenyan Shilling
        "UGX", // Ugandan Shilling
        "ETB", // Ethiopian Birr
        "CLP", // Chilean Peso
        "ARS", // Argentine Peso
        "UYU", // Uruguayan Peso
        "PEN", // Peruvian Sol
        "COP", // Colombian Peso
        "VEF", // Venezuelan Bolívar

        // Additional currencies for completeness
        "TWD", // New Taiwan Dollar
        "VEB", // Venezuelan Bolívar (legacy)
        "MAD", // Moroccan Dirham
        "AZN", // Azerbaijani Manat
        "GEL", // Georgian Lari
        "KZT", // Kazakhstani Tenge
        "KGS", // Kyrgyzstani Som
        "UZS", // Uzbekistani So'm
        "TJS", // Tajikistani Somoni
        "TMT", // Turkmenistani Manat
        "AFN", // Afghan Afghani
        "XAF", // CFA Franc BEAC
        "XOF", // CFA Franc BCEAO
        "XPF", // CFP Franc
        "ALL", // Albanian Lek
        "BAM", // Bosnia and Herzegovina Convertible Mark
        "MKD", // Macedonian Denar
        "SRD", // Surinamese Dollar
        "TTD", // Trinidad and Tobago Dollar
        "BBD", // Barbadian Dollar
        "BSD", // Bahamian Dollar
        "BZD", // Belize Dollar
        "BMD", // Bermudian Dollar
        "JMD", // Jamaican Dollar
        "KYD", // Cayman Islands Dollar
        "FJD", // Fijian Dollar
        "PGK", // Papua New Guinean Kina
        "SBD", // Solomon Islands Dollar
        "TOP", // Tongan Paʻanga
        "WST", // Samoan Tālā
        "VUV", // Vanuatu Vatu
        "XCD", // East Caribbean Dollar
        "ANG", // Netherlands Antillean Guilder
        "AWG", // Aruban Florin
        "BND", // Brunei Dollar
        "CUR", // Currently undefined/reserved codes
        "XXX", // No currency (reserved)
    };

    /// <summary>
    /// Validates if a currency code is valid according to ISO 4217.
    /// </summary>
    /// <param name="code">The currency code to validate (case-insensitive)</param>
    /// <returns>True if the code is a valid ISO 4217 currency; otherwise false</returns>
    public static bool IsValid(string? code)
    {
        return !string.IsNullOrWhiteSpace(code) && ValidCodes.Contains(code.Trim().ToUpperInvariant());
    }

    /// <summary>
    /// Normalizes a currency code to uppercase ISO 4217 format.
    /// </summary>
    /// <param name="code">The currency code to normalize</param>
    /// <returns>The normalized code in uppercase, or the original if null/empty</returns>
    public static string Normalize(string? code)
    {
        return string.IsNullOrWhiteSpace(code) ? code ?? string.Empty : code.Trim().ToUpperInvariant();
    }

    /// <summary>
    /// Gets all valid currency codes.
    /// </summary>
    public static IEnumerable<string> GetAllCodes => ValidCodes.OrderBy(c => c);
}
