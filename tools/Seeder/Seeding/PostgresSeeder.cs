using System.Globalization;
using System.Text;
using Infrastructure.Database;
using Npgsql;

namespace Seeder.Seeding;

internal sealed class PostgresSeeder(SeederOptions options)
{
    private static readonly string[] KeptTables = ["roles", "schema_versions"];

    public async Task RunAsync(CancellationToken ct)
    {
        Log.Info("== seed (PostgreSQL)");
        await Log.StepAsync("schema scripts", () => ScriptRunner.RunAsync(options.Postgres, ct));

        await using var connection =
            new NpgsqlConnection(new NpgsqlConnectionStringBuilder(options.Postgres) { CommandTimeout = 0 }
                .ConnectionString);
        await connection.OpenAsync(ct);
        await ExecAsync(connection, """
                                    SET session_replication_role = replica;
                                    SET synchronous_commit = off;
                                    SET maintenance_work_mem = '1GB';
                                    SET max_parallel_maintenance_workers = 4;
                                    SET work_mem = '256MB';
                                    """, ct);

        await using (NpgsqlTransaction tx = await connection.BeginTransactionAsync(ct))
        {
            Ddl ddl = await Log.StepAsync("drop keys and indexes", () => DropKeysAndIndexesAsync(connection, ct));
            await Log.StepAsync("truncate", () => TruncateAsync(connection, ct));

            var generator = new DataGenerator(connection, options.Scale);
            await Log.StepAsync("generate + copy", () => generator.RunAsync(ct));

            await Log.StepAsync("rebuild primary keys / unique constraints",
                () => ExecEachAsync(connection, ddl.Keys, ct));
            await Log.StepAsync("rebuild indexes", () => ExecEachAsync(connection, ddl.Indexes, ct));
            await Log.StepAsync("rebuild (validate) foreign keys",
                () => ExecEachAsync(connection, ddl.ForeignKeys, ct));
            await Log.StepAsync("recompute courses.enrolled_count / rating_avg / rating_count", () =>
                ExecAsync(connection, """
                                      WITH e AS (SELECT course_id, count(*)::int AS cnt FROM enrollments WHERE status <> 'dropped' GROUP BY course_id),
                                           r AS (SELECT course_id, round(avg(rating), 2) AS avg_rating, count(*)::int AS cnt
                                                 FROM reviews WHERE NOT is_deleted GROUP BY course_id)
                                      UPDATE courses c
                                      SET enrolled_count = coalesce(e.cnt, 0),
                                          rating_avg     = coalesce(r.avg_rating, 0),
                                          rating_count   = coalesce(r.cnt, 0)
                                      FROM courses c2
                                      LEFT JOIN e ON e.course_id = c2.id
                                      LEFT JOIN r ON r.course_id = c2.id
                                      WHERE c.id = c2.id
                                      """, ct));

            await Log.StepAsync("commit", () => tx.CommitAsync(ct));
        }

        await Log.StepAsync("vacuum analyze", () => ExecAsync(connection, "VACUUM (ANALYZE)", ct));

        IReadOnlyList<(string Table, long Rows, long Bytes)> stats = await TableStatsAsync(connection, ct);
        foreach (var (table, rows, bytes) in stats)
        {
            Log.Info(string.Create(CultureInfo.InvariantCulture,
                $"  {table,-20} {rows,12:N0} rows {bytes / 1024.0 / 1024.0,10:F1} MB"));
        }

        long dbBytes = (long)(await ScalarAsync(connection, "SELECT pg_database_size(current_database())", ct))!;
        Log.Info(string.Create(CultureInfo.InvariantCulture, $"  database size {dbBytes / 1024.0 / 1024.0:F1} MB"));

        await Log.StepAsync("bench files", () => WriteBenchFilesAsync(connection, stats, ct));
    }

    private sealed record Ddl(List<string> Keys, List<string> Indexes, List<string> ForeignKeys);

    private static async Task<Ddl> DropKeysAndIndexesAsync(NpgsqlConnection connection, CancellationToken ct)
    {
        var keys = new List<string>();
        var foreignKeys = new List<string>();
        var dropKeys = new List<string>();
        var dropForeignKeys = new List<string>();
        var indexes = new List<string>();
        var dropIndexes = new List<string>();
        int pinned = 0;

        await using (var cmd = new NpgsqlCommand("""
                                                 SELECT c.conrelid::regclass::text, quote_ident(c.conname), c.contype::text, pg_get_constraintdef(c.oid),
                                                        -- views relying on a primary key (GROUP BY functional dependency) pin it: keep those in place
                                                        EXISTS (SELECT 1 FROM pg_depend d
                                                                WHERE d.refclassid = 'pg_constraint'::regclass AND d.refobjid = c.oid
                                                                  AND d.classid = 'pg_rewrite'::regclass) AS pinned
                                                 FROM pg_constraint c
                                                 JOIN pg_class r ON r.oid = c.conrelid
                                                 JOIN pg_namespace n ON n.oid = r.relnamespace
                                                 WHERE n.nspname = 'public' AND c.contype IN ('p', 'u', 'f') AND r.relname <> ALL(@kept)
                                                 ORDER BY 1, 2
                                                 """, connection))
        {
            cmd.Parameters.AddWithValue("kept", KeptTables);
            await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                string table = reader.GetString(0);
                string name = reader.GetString(1);
                string add = $"ALTER TABLE {table} ADD CONSTRAINT {name} {reader.GetString(3)}";
                string drop = $"ALTER TABLE {table} DROP CONSTRAINT {name}";
                if (reader.GetString(2) == "f")
                {
                    foreignKeys.Add(add);
                    dropForeignKeys.Add(drop);
                }
                else if (reader.GetBoolean(4))
                {
                    pinned++;
                }
                else
                {
                    keys.Add(add);
                    dropKeys.Add(drop);
                }
            }
        }

        await using (var cmd = new NpgsqlCommand("""
                                                 SELECT pg_get_indexdef(i.indexrelid), quote_ident(ic.relname)
                                                 FROM pg_index i
                                                 JOIN pg_class ic ON ic.oid = i.indexrelid
                                                 JOIN pg_class r ON r.oid = i.indrelid
                                                 JOIN pg_namespace n ON n.oid = r.relnamespace
                                                 WHERE n.nspname = 'public' AND r.relname <> ALL(@kept)
                                                   AND NOT EXISTS (SELECT 1 FROM pg_constraint k WHERE k.conindid = i.indexrelid AND k.contype IN ('p', 'u', 'x'))
                                                 ORDER BY 2
                                                 """, connection))
        {
            cmd.Parameters.AddWithValue("kept", KeptTables);
            await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                indexes.Add(reader.GetString(0));
                dropIndexes.Add($"DROP INDEX {reader.GetString(1)}");
            }
        }

        await ExecEachAsync(connection, dropForeignKeys, ct);
        await ExecEachAsync(connection, dropIndexes, ct);
        await ExecEachAsync(connection, dropKeys, ct);
        Log.Info(
            $"  dropped {dropForeignKeys.Count} foreign keys, {dropIndexes.Count} indexes, {dropKeys.Count} primary/unique keys (kept {pinned} referenced by views)");
        return new Ddl(keys, indexes, foreignKeys);
    }

    private static async Task TruncateAsync(NpgsqlConnection connection, CancellationToken ct)
    {
        var tables = new List<string>();
        await using (var cmd = new NpgsqlCommand(
                         "SELECT quote_ident(tablename) FROM pg_tables WHERE schemaname = 'public' AND tablename <> ALL(@kept) ORDER BY 1",
                         connection))
        {
            cmd.Parameters.AddWithValue("kept", KeptTables);
            await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                tables.Add(reader.GetString(0));
            }
        }

        await ExecAsync(connection, $"TRUNCATE {string.Join(", ", tables)} RESTART IDENTITY", ct);
    }

    private static async Task<IReadOnlyList<(string Table, long Rows, long Bytes)>> TableStatsAsync(
        NpgsqlConnection connection, CancellationToken ct)
    {
        var tables = new List<string>();
        await using (var cmd = new NpgsqlCommand(
                         "SELECT tablename FROM pg_tables WHERE schemaname = 'public' AND tablename <> 'schema_versions' ORDER BY 1",
                         connection))
        await using (NpgsqlDataReader reader = await cmd.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                tables.Add(reader.GetString(0));
            }
        }

        var result = new List<(string, long, long)>();
        foreach (string table in tables)
        {
            long rows = (long)(await ScalarAsync(connection, $"SELECT count(*) FROM \"{table}\"", ct))!;
            long bytes = (long)(await ScalarAsync(connection, $"SELECT pg_total_relation_size('\"{table}\"')", ct))!;
            result.Add((table, rows, bytes));
        }

        return result;
    }

    private async Task WriteBenchFilesAsync(NpgsqlConnection connection,
        IReadOnlyList<(string Table, long Rows, long Bytes)> stats, CancellationToken ct)
    {
        Directory.CreateDirectory(options.OutDir);

        List<Guid> courseIds =
            await ReadIdsAsync(connection, "SELECT id FROM courses WHERE NOT is_deleted ORDER BY id", ct);
        await File.WriteAllLinesAsync(Path.Combine(options.OutDir, "course_ids.txt"), courseIds.Select(Key), ct);

        List<Guid> studentPool = await ReadIdsAsync(connection, """
                                                                SELECT u.id FROM users u
                                                                WHERE NOT u.is_deleted
                                                                  AND EXISTS (SELECT 1 FROM user_roles ur JOIN roles r ON r.id = ur.role_id WHERE ur.user_id = u.id AND r.name = 'student')
                                                                  AND EXISTS (SELECT 1 FROM enrollments e WHERE e.user_id = u.id)
                                                                ORDER BY u.id
                                                                """, ct);
        Guid[] shuffled = studentPool.ToArray();
        new Random(42).Shuffle(shuffled);
        Guid[] students = shuffled.Take(20_000).ToArray();
        await File.WriteAllLinesAsync(Path.Combine(options.OutDir, "student_ids.txt"), students.Select(Key), ct);

        long liveStudents = (long)(await ScalarAsync(connection, """
                                                                 SELECT count(*) FROM users u
                                                                 WHERE NOT u.is_deleted
                                                                   AND EXISTS (SELECT 1 FROM user_roles ur JOIN roles r ON r.id = ur.role_id WHERE ur.user_id = u.id AND r.name = 'student')
                                                                 """, ct))!;

        var meta = new StringBuilder();
        meta.AppendLine(CultureInfo.InvariantCulture, $"scale={options.Scale.ToString(CultureInfo.InvariantCulture)}");
        meta.AppendLine(CultureInfo.InvariantCulture, $"generated_at={DateTime.UtcNow:O}");
        foreach (var (table, rows, bytes) in stats)
        {
            meta.AppendLine(CultureInfo.InvariantCulture, $"rows.{table}={rows}");
            meta.AppendLine(CultureInfo.InvariantCulture, $"bytes.{table}={bytes}");
        }

        meta.AppendLine(CultureInfo.InvariantCulture, $"live_courses={courseIds.Count}");
        meta.AppendLine(CultureInfo.InvariantCulture, $"live_students={liveStudents}");
        meta.AppendLine(CultureInfo.InvariantCulture, $"bench_course_ids={courseIds.Count}");
        meta.AppendLine(CultureInfo.InvariantCulture, $"bench_student_ids={students.Length}");
        await File.WriteAllTextAsync(Path.Combine(options.OutDir, "meta.txt"), meta.ToString(), ct);

        Log.Info(
            $"  {courseIds.Count:N0} course ids, {students.Length:N0} student ids -> {Path.GetFullPath(options.OutDir)}");

        static string Key(Guid id) => id.ToString("D", CultureInfo.InvariantCulture);
    }

    internal static async Task<List<Guid>> ReadIdsAsync(NpgsqlConnection connection, string sql, CancellationToken ct)
    {
        var ids = new List<Guid>();
        await using var cmd = new NpgsqlCommand(sql, connection);
        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            ids.Add(reader.GetGuid(0));
        }

        return ids;
    }

    internal static async Task ExecAsync(NpgsqlConnection connection, string sql, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(sql, connection);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    internal static async Task<object?> ScalarAsync(NpgsqlConnection connection, string sql, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(sql, connection);
        return await cmd.ExecuteScalarAsync(ct);
    }

    private static async Task ExecEachAsync(NpgsqlConnection connection, IEnumerable<string> statements,
        CancellationToken ct)
    {
        foreach (string sql in statements)
        {
            await ExecAsync(connection, sql, ct);
        }
    }
}
