using System.Data;

namespace Application.Abstractions.Data;

/// <summary>
/// A deferred database call (usually a stored procedure invocation) that a repository hands to the unit of work.
/// </summary>
public delegate Task DbOperation(IDbConnection connection, IDbTransaction transaction, CancellationToken cancellationToken);
