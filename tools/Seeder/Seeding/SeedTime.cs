namespace Seeder.Seeding;

/// <summary>All generated timestamps are whole UTC seconds inside [2024-01-01, 2025-12-31 23:59:59].</summary>
internal static class SeedTime
{
    public const long Day = 86_400;

    public static readonly long Start = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
    public static readonly long End = new DateTimeOffset(2025, 12, 31, 23, 59, 59, TimeSpan.Zero).ToUnixTimeSeconds();

    public static DateTime ToUtc(long unixSeconds) => DateTime.UnixEpoch.AddSeconds(unixSeconds);

    /// <summary>Uniform whole second in [lo, hi] (hi clamped to End, lo to Start).</summary>
    public static long Between(Random rng, long lo, long hi)
    {
        lo = Math.Max(lo, Start);
        hi = Math.Min(hi, End);
        return hi <= lo ? lo : lo + rng.NextInt64(hi - lo + 1);
    }

    /// <summary>Fills <paramref name="target"/> with sorted uniform seconds in [lo, hi].</summary>
    public static void SortedBetween(Random rng, long lo, long hi, Span<long> target)
    {
        for (int i = 0; i < target.Length; i++)
        {
            target[i] = Between(rng, lo, hi);
        }

        target.Sort();
    }
}
