using ClickHouse.Client.ADO;
using Dapper;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Infrastructure.Reports.ClickHouse;

internal sealed class ClickHouseHealthCheck(ClickHouseConnectionFactory connectionFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using ClickHouseConnection connection = await connectionFactory.OpenAsync(cancellationToken);
            await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT 1", cancellationToken: cancellationToken));
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, exception: exception);
        }
    }
}
