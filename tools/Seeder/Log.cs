using System.Diagnostics;
using System.Globalization;

namespace Seeder;

internal static class Log
{
    private static readonly Stopwatch Clock = Stopwatch.StartNew();

    public static void Info(string message) =>
        Console.WriteLine(
            string.Create(CultureInfo.InvariantCulture, $"[{Clock.Elapsed.TotalSeconds,7:F1}s] {message}"));

    /// <summary>Runs a step and prints its duration (and optional row count).</summary>
    public static async Task<T> StepAsync<T>(string name, Func<Task<T>> step)
    {
        var sw = Stopwatch.StartNew();
        T result = await step();
        Info(string.Create(CultureInfo.InvariantCulture, $"{name}: {Describe(result)}{sw.Elapsed.TotalSeconds:F1}s"));
        return result;
    }

    public static async Task StepAsync(string name, Func<Task> step)
    {
        var sw = Stopwatch.StartNew();
        await step();
        Info(string.Create(CultureInfo.InvariantCulture, $"{name}: {sw.Elapsed.TotalSeconds:F1}s"));
    }

    private static string Describe<T>(T result) => result switch
    {
        long n => string.Create(CultureInfo.InvariantCulture, $"{n:N0} rows in "),
        int n => string.Create(CultureInfo.InvariantCulture, $"{n:N0} rows in "),
        _ => string.Empty,
    };
}
