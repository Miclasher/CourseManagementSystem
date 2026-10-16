using System.Reflection;
using Npgsql;

namespace Infrastructure.Database;

/// <summary>
/// Applies the embedded numbered SQL scripts once each and records them in <c>schema_versions</c>.
/// </summary>
public static class ScriptRunner
{
    public static async Task RunAsync(string connectionString, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using (var create = new NpgsqlCommand(
            "CREATE TABLE IF NOT EXISTS schema_versions (script varchar(128) PRIMARY KEY, applied_at timestamptz NOT NULL DEFAULT now())",
            connection))
        {
            await create.ExecuteNonQueryAsync(cancellationToken);
        }

        Assembly assembly = typeof(ScriptRunner).Assembly;
        IEnumerable<string> scripts = assembly.GetManifestResourceNames()
            .Where(name => name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            .OrderBy(name => name, StringComparer.Ordinal);

        foreach (string resource in scripts)
        {
            string name = resource[(resource.LastIndexOf('.', resource.Length - 5) + 1)..];

            await using (var check = new NpgsqlCommand("SELECT 1 FROM schema_versions WHERE script = @name", connection))
            {
                check.Parameters.AddWithValue("name", name);
                if (await check.ExecuteScalarAsync(cancellationToken) is not null)
                {
                    continue;
                }
            }

            await using Stream stream = assembly.GetManifestResourceStream(resource)!;
            using var reader = new StreamReader(stream);
            string sql = await reader.ReadToEndAsync(cancellationToken);

            await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

#pragma warning disable CA2100 // Script text comes from embedded resources, not user input
            await using (var run = new NpgsqlCommand(sql, connection, transaction))
#pragma warning restore CA2100
            {
                await run.ExecuteNonQueryAsync(cancellationToken);
            }

            await using (var record = new NpgsqlCommand("INSERT INTO schema_versions (script) VALUES (@name)", connection, transaction))
            {
                record.Parameters.AddWithValue("name", name);
                await record.ExecuteNonQueryAsync(cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
    }
}
