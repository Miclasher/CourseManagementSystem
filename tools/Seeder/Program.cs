using System.Diagnostics;
using Seeder;
using Seeder.Projection;
using Seeder.Seeding;

SeederOptions options;
try
{
    options = SeederOptions.Parse(args);
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine(SeederOptions.Usage);
    return 2;
}

if (options.Command is null)
{
    Console.WriteLine(SeederOptions.Usage);
    return 0;
}

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var total = Stopwatch.StartNew();
Log.Info($"command={options.Command} scale={options.Scale} out={Path.GetFullPath(options.OutDir)}");

if (options.Command is "seed" or "all")
{
    await new PostgresSeeder(options).RunAsync(cts.Token);
}

if (options.Command is "project" or "all")
{
    await new ClickHouseProjector(options).RunAsync(cts.Token);
    await new MongoProjector(options).RunAsync(cts.Token);
}

Log.Info($"done in {total.Elapsed.TotalSeconds:F1}s");
return 0;
