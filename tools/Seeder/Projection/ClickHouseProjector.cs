using System.Globalization;
using ClickHouse.Client.ADO;
using Npgsql;
using Seeder.Seeding;

namespace Seeder.Projection;

/// <summary>
/// Rebuilds <c>activity_events</c> by letting ClickHouse pull straight from PostgreSQL with the postgresql() table
/// function. The joins mirror <c>Infrastructure.Reports.Postgres.PostgresActivityEvents</c>: lesson_progress /
/// quiz_attempts -> enrollments -> courses (NOT is_deleted) -> categories.
/// </summary>
internal sealed class ClickHouseProjector(SeederOptions options)
{
    private const string PgLessonEvents = """
                                          FROM lesson_progress lp
                                          JOIN enrollments e ON e.id = lp.enrollment_id
                                          JOIN courses c ON c.id = e.course_id AND NOT c.is_deleted
                                          JOIN categories cat ON cat.id = c.category_id
                                          """;

    private const string PgQuizEvents = """
                                        FROM quiz_attempts qa
                                        JOIN enrollments e ON e.id = qa.enrollment_id
                                        JOIN courses c ON c.id = e.course_id AND NOT c.is_deleted
                                        JOIN categories cat ON cat.id = c.category_id
                                        """;

    public async Task RunAsync(CancellationToken ct)
    {
        Log.Info("== project (ClickHouse)");
        var pg = new NpgsqlConnectionStringBuilder(options.Postgres);

        string Source(string table) => string.Create(CultureInfo.InvariantCulture,
            $"postgresql('{Esc(options.ClickHousePostgresHost)}', '{Esc(pg.Database!)}', '{table}', '{Esc(pg.Username!)}', '{Esc(pg.Password ?? string.Empty)}')");

        string enrollments = $"(SELECT id, user_id, course_id FROM {Source("enrollments")}) AS e";
        string courses = $"(SELECT id, title, category_id FROM {Source("courses")} WHERE NOT is_deleted) AS c";
        string categories = $"(SELECT id, name FROM {Source("categories")}) AS cat";

        string insertLessons = $"""
                                INSERT INTO activity_events
                                    (event_type, event_at, user_id, enrollment_id, course_id, course_title, category_id, category_name, score_hundredths, passed)
                                SELECT 'lesson_completed', toDateTime(lp.completed_at, 'UTC'), e.user_id, e.id, c.id, c.title, c.category_id, cat.name, 0, 0
                                FROM (SELECT enrollment_id, completed_at FROM {Source("lesson_progress")}) AS lp
                                INNER JOIN {enrollments} ON e.id = lp.enrollment_id
                                INNER JOIN {courses} ON c.id = e.course_id
                                INNER JOIN {categories} ON cat.id = c.category_id
                                """;

        string insertQuizzes = $"""
                                INSERT INTO activity_events
                                    (event_type, event_at, user_id, enrollment_id, course_id, course_title, category_id, category_name, score_hundredths, passed)
                                SELECT 'quiz_attempt', toDateTime(qa.attempted_at, 'UTC'), e.user_id, e.id, c.id, c.title, c.category_id, cat.name,
                                       toUInt32(qa.score_percent * 100), toUInt8(qa.passed)
                                FROM (SELECT enrollment_id, attempted_at, score_percent, passed FROM {Source("quiz_attempts")}) AS qa
                                INNER JOIN {enrollments} ON e.id = qa.enrollment_id
                                INNER JOIN {courses} ON c.id = e.course_id
                                INNER JOIN {categories} ON cat.id = c.category_id
                                """;

        await using var ch = new ClickHouseConnection(options.ClickHouse);
        await ch.OpenAsync(ct);

        await Log.StepAsync("truncate activity_events", () => ExecAsync(ch, "TRUNCATE TABLE activity_events", ct));
        await Log.StepAsync("insert lesson_completed events (postgresql())", () => ExecAsync(ch, insertLessons, ct));
        await Log.StepAsync("insert quiz_attempt events (postgresql())", () => ExecAsync(ch, insertQuizzes, ct));
        await Log.StepAsync("optimize final", () => ExecAsync(ch, "OPTIMIZE TABLE activity_events FINAL", ct));

        // verification against PostgreSQL
        await using var pgConnection = new NpgsqlConnection(options.Postgres);
        await pgConnection.OpenAsync(ct);
        long pgLessons =
            (long)(await PostgresSeeder.ScalarAsync(pgConnection, $"SELECT count(*) {PgLessonEvents}", ct))!;
        long pgQuizzes = (long)(await PostgresSeeder.ScalarAsync(pgConnection, $"SELECT count(*) {PgQuizEvents}", ct))!;
        long pgScore = (long)(await PostgresSeeder.ScalarAsync(pgConnection,
            $"SELECT coalesce(sum(round(qa.score_percent * 100)), 0)::bigint {PgQuizEvents}", ct))!;
        long pgPassed =
            (long)(await PostgresSeeder.ScalarAsync(pgConnection, $"SELECT count(*) {PgQuizEvents} WHERE qa.passed",
                ct))!;

        long chLessons = await ScalarLongAsync(ch,
            "SELECT count() FROM activity_events WHERE event_type = 'lesson_completed'", ct);
        long chQuizzes = await ScalarLongAsync(ch,
            "SELECT count() FROM activity_events WHERE event_type = 'quiz_attempt'", ct);
        long chScore = await ScalarLongAsync(ch,
            "SELECT toInt64(sum(score_hundredths)) FROM activity_events WHERE event_type = 'quiz_attempt'", ct);
        long chPassed = await ScalarLongAsync(ch,
            "SELECT count() FROM activity_events WHERE event_type = 'quiz_attempt' AND passed = 1", ct);

        Report("lesson_completed rows", pgLessons, chLessons);
        Report("quiz_attempt rows", pgQuizzes, chQuizzes);
        Report("quiz passed rows", pgPassed, chPassed);
        Report("sum score_hundredths", pgScore, chScore);

        long bytes = await ScalarLongAsync(ch,
            "SELECT toInt64(sum(bytes_on_disk)) FROM system.parts WHERE active AND database = currentDatabase() AND table = 'activity_events'",
            ct);
        long parts = await ScalarLongAsync(ch,
            "SELECT count() FROM system.parts WHERE active AND database = currentDatabase() AND table = 'activity_events'",
            ct);
        Log.Info(string.Create(CultureInfo.InvariantCulture,
            $"  activity_events on disk: {bytes / 1024.0 / 1024.0:F1} MB in {parts} active parts"));

        if (pgLessons != chLessons || pgQuizzes != chQuizzes || pgScore != chScore || pgPassed != chPassed)
        {
            throw new InvalidOperationException("ClickHouse projection does not match PostgreSQL");
        }
    }

    private static void Report(string what, long pg, long ch) =>
        Log.Info(string.Create(CultureInfo.InvariantCulture,
            $"  {what,-24} postgres={pg,12:N0} clickhouse={ch,12:N0} {(pg == ch ? "OK" : "MISMATCH")}"));

    private static string Esc(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("'", "\\'", StringComparison.Ordinal);

    private static async Task ExecAsync(ClickHouseConnection connection, string sql, CancellationToken ct)
    {
        using ClickHouseCommand cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task<long> ScalarLongAsync(ClickHouseConnection connection, string sql, CancellationToken ct)
    {
        using ClickHouseCommand cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        return Convert.ToInt64(await cmd.ExecuteScalarAsync(ct), CultureInfo.InvariantCulture);
    }
}
