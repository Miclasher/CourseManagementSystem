using Application.Reports.MonthlyActivity;
using ClickHouse.Client.ADO;
using Dapper;

namespace Infrastructure.Reports.ClickHouse;

internal sealed class ClickHouseMonthlyActivitySource(ClickHouseConnectionFactory connectionFactory) : IMonthlyActivitySource
{
    private const string Sql = $"""
        SELECT toInt32(toYYYYMM(event_at, 'UTC')) AS year_month,
               category_id,
               category_name,
               {ClickHouseActivityEvents.Aggregates}
        FROM activity_events
        WHERE {ClickHouseActivityEvents.PeriodFilter}
        GROUP BY year_month, category_id, category_name
        """;

    public async Task<IReadOnlyList<MonthlyActivityRaw>> GetAsync(int year, CancellationToken cancellationToken)
    {
        var parameters = new DynamicParameters();
        parameters.Add("from", new DateTime(year, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        parameters.Add("to", new DateTime(year + 1, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        await using ClickHouseConnection connection = await connectionFactory.OpenAsync(cancellationToken);

        IEnumerable<MonthlyActivityRaw> rows = await connection.QueryAsync<MonthlyActivityRaw>(
            new CommandDefinition(Sql, parameters, cancellationToken: cancellationToken));

        return rows.AsList();
    }
}
