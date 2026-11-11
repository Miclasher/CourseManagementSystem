using Application.Reports.TopCourses;
using Dapper;
using Npgsql;

namespace Infrastructure.Reports.Postgres;

internal sealed class PostgresTopCoursesSource(NpgsqlDataSource dataSource) : ITopCoursesSource
{
    private const string Sql = $"""
        WITH {PostgresActivityEvents.Cte}
        SELECT c.id AS course_id,
               c.title AS course_title,
               cat.name AS category_name,
               {PostgresActivityEvents.Aggregates}
        FROM events ev
        JOIN courses c ON c.id = ev.course_id
        JOIN categories cat ON cat.id = c.category_id
        GROUP BY c.id, c.title, cat.name
        ORDER BY lessons_completed DESC, c.id::text ASC
        LIMIT @limit
        """;

    public async Task<IReadOnlyList<TopCourseRaw>> GetAsync(
        DateTime fromUtc,
        DateTime toUtcExclusive,
        int limit,
        CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

        IEnumerable<TopCourseRaw> rows = await connection.QueryAsync<TopCourseRaw>(
            new CommandDefinition(Sql, new { from = fromUtc, to = toUtcExclusive, limit }, cancellationToken: cancellationToken));

        return rows.AsList();
    }
}
