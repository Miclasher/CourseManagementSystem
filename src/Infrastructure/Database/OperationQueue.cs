using Application.Abstractions.Data;
using SharedKernel;

namespace Infrastructure.Database;

/// <summary>
/// Per-request buffer shared by every <see cref="UnitOfWork" /> instance resolved inside the same scope.
/// </summary>
internal sealed class OperationQueue
{
    private readonly List<(DbOperation Operation, Entity? Source)> _items = [];

    public bool IsEmpty => _items.Count == 0;

    public void Add(DbOperation operation, Entity? source) => _items.Add((operation, source));

    public IReadOnlyList<(DbOperation Operation, Entity? Source)> Drain()
    {
        List<(DbOperation Operation, Entity? Source)> snapshot = [.. _items];
        _items.Clear();
        return snapshot;
    }
}
