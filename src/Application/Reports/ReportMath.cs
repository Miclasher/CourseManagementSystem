using System.Globalization;

namespace Application.Reports;

/// <summary>
/// The only place where derived numbers and dates are produced, so that every source yields byte-identical JSON:
/// sources return raw integer aggregates, this class turns them into rates, averages and strings.
/// </summary>
internal static class ReportMath
{
    /// <summary>100 * part / total rounded to 2 decimals (0 when total is 0).</summary>
    public static decimal Percent(long part, long total) =>
        total == 0 ? 0m : decimal.Round(100m * part / total, 2, MidpointRounding.AwayFromZero);

    /// <summary>sum / count rounded to 2 decimals (0 when count is 0).</summary>
    public static decimal Average(long sum, long count) =>
        count == 0 ? 0m : decimal.Round((decimal)sum / count, 2, MidpointRounding.AwayFromZero);

    /// <summary>Average of values stored in hundredths (e.g. score 87.25 stored as 8725).</summary>
    public static decimal AverageHundredths(long sumHundredths, long count) =>
        count == 0 ? 0m : decimal.Round(sumHundredths / 100m / count, 2, MidpointRounding.AwayFromZero);

    public static decimal FromHundredths(long hundredths) => decimal.Round(hundredths / 100m, 2, MidpointRounding.AwayFromZero);

    /// <summary>UTC, whole seconds: PostgreSQL keeps microseconds, MongoDB milliseconds, ClickHouse seconds.</summary>
    public static string Timestamp(DateTime value) =>
        value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);

    public static string? Timestamp(DateTime? value) => value is null ? null : Timestamp(value.Value);

    /// <summary>202503 -> "2025-03".</summary>
    public static string Month(int yearMonth) =>
        string.Create(CultureInfo.InvariantCulture, $"{yearMonth / 100:D4}-{yearMonth % 100:D2}");

    public static string Key(Guid id) => id.ToString("D", CultureInfo.InvariantCulture);
}
