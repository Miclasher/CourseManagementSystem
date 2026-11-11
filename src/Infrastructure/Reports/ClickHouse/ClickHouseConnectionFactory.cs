using System.Net;
using ClickHouse.Client.ADO;

namespace Infrastructure.Reports.ClickHouse;

/// <summary>
/// Creates a new <see cref="ClickHouseConnection" /> per query. Connections are thin wrappers over HTTP, so all of
/// them share one <see cref="HttpClient" /> (and its socket pool) to avoid socket exhaustion under load.
/// </summary>
internal sealed class ClickHouseConnectionFactory : IDisposable
{
    private readonly string _connectionString;
    private readonly SocketsHttpHandler _handler;
    private readonly HttpClient _httpClient;

    public ClickHouseConnectionFactory(string connectionString)
    {
        _connectionString = connectionString;
        _handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            MaxConnectionsPerServer = 256,
            AutomaticDecompression = DecompressionMethods.All
        };
        _httpClient = new HttpClient(_handler, disposeHandler: false);
    }

    public async Task<ClickHouseConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new ClickHouseConnection(_connectionString, _httpClient);
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _handler.Dispose();
    }
}
