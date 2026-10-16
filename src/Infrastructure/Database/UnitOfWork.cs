using Application.Abstractions.Data;
using Infrastructure.DomainEvents;
using Npgsql;
using SharedKernel;
using System.Data.Common;

namespace Infrastructure.Database;

/// <summary>
/// Registered as transient: every consumer gets its own cheap instance, all of them forwarding to the scoped
/// <see cref="OperationQueue" />, so repositories and the request-end commit see the same pending work.
/// </summary>
internal sealed class UnitOfWork(
    OperationQueue queue,
    ISqlConnectionFactory connectionFactory,
    IDomainEventsDispatcher domainEventsDispatcher) : IUnitOfWork
{
    public void Enqueue(DbOperation operation, Entity? source = null) => queue.Add(operation, source);

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (queue.IsEmpty)
        {
            return;
        }

        IReadOnlyList<(DbOperation Operation, Entity? Source)> items = queue.Drain();

        await using DbConnection connection = await connectionFactory.OpenAsync(cancellationToken);
        await using DbTransaction transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach ((DbOperation operation, _) in items)
            {
                await operation(connection, transaction, cancellationToken);
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (PostgresException exception)
        {
            await transaction.RollbackAsync(CancellationToken.None);

            Error? error = PostgresErrorMapper.Map(exception);
            if (error is null)
            {
                throw;
            }

            throw new DatabaseOperationException(error);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }

        await PublishDomainEventsAsync(items, cancellationToken);
    }

    private async Task PublishDomainEventsAsync(
        IReadOnlyList<(DbOperation Operation, Entity? Source)> items,
        CancellationToken cancellationToken)
    {
        var domainEvents = items
            .Select(item => item.Source)
            .OfType<Entity>()
            .Distinct()
            .SelectMany(entity =>
            {
                IReadOnlyCollection<IDomainEvent> events = entity.DomainEvents;
                entity.ClearDomainEvents();
                return events;
            })
            .ToList();

        await domainEventsDispatcher.DispatchAsync(domainEvents, cancellationToken);
    }
}
