using Application.Reports.TopCourses;
using ClickHouse.Client.ADO;
using Dapper;

namespace Infrastructure.Reports.ClickHouse;

internal sealed class ClickHouseTopCoursesSource(ClickHouseConnectionFactory connectionFactory) : ITopCoursesSource
{
    private const string Sql = $"""
        SELECT course_id,
               course_title,
               category_name,
               {ClickHouseActivityEvents.Aggregates}
        FROM activity_events
        WHERE {ClickHouseActivityEvents.PeriodFilter}
        GROUP BY course_id, course_title, category_name
        ORDER BY lessons_completed DESC, toString(course_id) ASC
        LIMIT {"{limit:Int32}"}
        """;

    public async Task<IReadOnlyList<TopCourseRaw>> GetAsync(
        DateTime fromUtc,
        DateTime toUtcExclusive,
        int limit,
        CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("from", fromUtc);
        parameters.Add("to", toUtcExclusive);
        parameters.Add("limit", limit);

        await using ClickHouseConnection connection = await connectionFactory.OpenAsync(cancellationToken);

        IEnumerable<TopCourseRaw> rows = await connection.QueryAsync<TopCourseRaw>(
            new CommandDefinition(Sql, parameters, cancellationToken: cancellationToken));

        return rows.AsList();
    }
}
