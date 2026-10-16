using System.Data.Common;

namespace Application.Abstractions.Data;

public interface ISqlConnectionFactory
{
    ValueTask<DbConnection> OpenAsync(CancellationToken cancellationToken = default);
}
