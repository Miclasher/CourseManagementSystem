using Application.Reports.MonthlyActivity;
using Dapper;
using Npgsql;

namespace Infrastructure.Reports.Postgres;

internal sealed class PostgresMonthlyActivitySource(NpgsqlDataSource dataSource) : IMonthlyActivitySource
{
    private const string Sql = $"""
        WITH {PostgresActivityEvents.Cte}
        SELECT (extract(year FROM ev.event_at AT TIME ZONE 'UTC') * 100
                + extract(month FROM ev.event_at AT TIME ZONE 'UTC'))::int AS year_month,
               cat.id AS category_id,
               cat.name AS category_name,
               {PostgresActivityEvents.Aggregates}
        FROM events ev
        JOIN categories cat ON cat.id = ev.category_id
        GROUP BY 1, cat.id, cat.name
        """;

    public async Task<IReadOnlyList<MonthlyActivityRaw>> GetAsync(int year, CancellationToken cancellationToken)
    {
        var from = new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime to = from.AddYears(1);

        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

        IEnumerable<MonthlyActivityRaw> rows = await connection.QueryAsync<MonthlyActivityRaw>(
            new CommandDefinition(Sql, new { from, to }, cancellationToken: cancellationToken));

        return rows.AsList();
    }
}
