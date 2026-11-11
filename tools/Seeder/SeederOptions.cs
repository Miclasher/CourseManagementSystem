using System.Globalization;

namespace Seeder;

internal sealed class SeederOptions
{
    public const string Usage = """
                                Usage: Seeder <seed|project|all> [options]
                                  seed      recreate PostgreSQL data (deterministic, Bogus seed 42) and write bench id files
                                  project   project PostgreSQL into ClickHouse (activity_events) and MongoDB (report documents)
                                  all       seed, then project
                                Options:
                                  --scale <double>       data volume multiplier (default 1.0 = ~100k users, 2k courses, 1M enrollments)
                                  --pg <conn>            PostgreSQL connection string
                                  --mongo <conn>         MongoDB connection string
                                  --clickhouse <conn>    ClickHouse (ClickHouse.Client) connection string
                                  --ch-pg-host <h:p>     PostgreSQL host:port as seen from the ClickHouse server (default postgres:5432)
                                  --out <dir>            output directory for bench id files (default bench/data)
                                """;

    public string? Command { get; private set; }

    public double Scale { get; private set; } = 1.0;

    public string Postgres { get; private set; } =
        "Host=localhost;Port=5432;Database=clean-architecture;Username=postgres;Password=postgres;Include Error Detail=true";

    public string Mongo { get; private set; } = "mongodb://localhost:27017/cms";

    public string ClickHouse { get; private set; } =
        "Host=localhost;Port=8123;Username=cms;Password=cms;Database=cms;Timeout=1800";

    /// <summary>How the ClickHouse server reaches PostgreSQL for the postgresql() table function.</summary>
    public string ClickHousePostgresHost { get; private set; } = "postgres:5432";

    public string OutDir { get; private set; } = Path.Combine("bench", "data");

    public static SeederOptions Parse(string[] args)
    {
        var options = new SeederOptions();
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            switch (arg)
            {
                case "seed" or "project" or "all":
                    options.Command = arg;
                    break;
                case "-h" or "--help":
                    options.Command = null;
                    return options;
                case "--scale":
                    options.Scale = double.Parse(Next(args, ref i, arg), CultureInfo.InvariantCulture);
                    if (options.Scale <= 0)
                    {
                        throw new ArgumentException("--scale must be positive");
                    }

                    break;
                case "--pg":
                    options.Postgres = Next(args, ref i, arg);
                    break;
                case "--mongo":
                    options.Mongo = Next(args, ref i, arg);
                    break;
                case "--clickhouse":
                    options.ClickHouse = Next(args, ref i, arg);
                    break;
                case "--ch-pg-host":
                    options.ClickHousePostgresHost = Next(args, ref i, arg);
                    break;
                case "--out":
                    options.OutDir = Next(args, ref i, arg);
                    break;
                default:
                    throw new ArgumentException($"Unknown argument '{arg}'");
            }
        }

        return options;
    }

    private static string Next(string[] args, ref int i, string name)
    {
        if (i + 1 >= args.Length)
        {
            throw new ArgumentException($"{name} requires a value");
        }

        i++;
        return args[i];
    }
}
