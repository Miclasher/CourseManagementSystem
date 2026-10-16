namespace Infrastructure.Database;

internal static class EnumExtensions
{
    /// <summary>
    /// Database check constraints store enum members as lower-case text.
    /// </summary>
#pragma warning disable CA1308 // Lower case is the storage format, not a normalisation for comparison
    public static string ToDbValue(this Enum value) => value.ToString().ToLowerInvariant();
#pragma warning restore CA1308
}
