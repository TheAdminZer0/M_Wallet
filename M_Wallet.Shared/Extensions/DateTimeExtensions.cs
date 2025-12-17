namespace M_Wallet.Shared.Extensions;

/// <summary>
/// Extension methods for DateTime formatting throughout the application.
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Standard date and time format used across the application.
    /// </summary>
    public const string StandardDateTimeFormat = "yyyy/MM/dd hh:mm tt";
    
    /// <summary>
    /// Standard date-only format used across the application.
    /// </summary>
    public const string StandardDateFormat = "yyyy/MM/dd";

    /// <summary>
    /// Converts a DateTime to the standard display format (yyyy/MM/dd hh:mm tt).
    /// Automatically converts to local time.
    /// </summary>
    public static string ToDisplayString(this DateTime dateTime)
    {
        return dateTime.ToLocalTime().ToString(StandardDateTimeFormat);
    }

    /// <summary>
    /// Converts a nullable DateTime to the standard display format.
    /// Returns "-" if null.
    /// </summary>
    public static string ToDisplayString(this DateTime? dateTime)
    {
        return dateTime?.ToLocalTime().ToString(StandardDateTimeFormat) ?? "-";
    }

    /// <summary>
    /// Converts a DateTime to date-only display format (yyyy/MM/dd).
    /// Automatically converts to local time.
    /// </summary>
    public static string ToDateOnlyString(this DateTime dateTime)
    {
        return dateTime.ToLocalTime().ToString(StandardDateFormat);
    }

    /// <summary>
    /// Converts a nullable DateTime to date-only display format.
    /// Returns "-" if null.
    /// </summary>
    public static string ToDateOnlyString(this DateTime? dateTime)
    {
        return dateTime?.ToLocalTime().ToString(StandardDateFormat) ?? "-";
    }
}
