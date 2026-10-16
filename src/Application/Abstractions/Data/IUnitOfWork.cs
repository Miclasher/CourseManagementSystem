using SharedKernel;

namespace Application.Abstractions.Data;

public interface IUnitOfWork
{
    /// <summary>
    /// Queues an operation. Nothing is sent to the database until <see cref="CommitAsync" />.
    /// </summary>
    /// <param name="operation">The deferred database call.</param>
    /// <param name="source">Entity whose domain events are published once the commit succeeds.</param>
    void Enqueue(DbOperation operation, Entity? source = null);

    /// <summary>
    /// Runs all queued operations in a single transaction, then publishes the collected domain events.
    /// </summary>
    Task CommitAsync(CancellationToken cancellationToken = default);
}
