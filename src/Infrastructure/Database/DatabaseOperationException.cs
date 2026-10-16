using SharedKernel;

namespace Infrastructure.Database;

/// <summary>
/// Raised when the database rejects a queued operation for a business reason (RAISE EXCEPTION in a procedure,
/// constraint violation). Carries the mapped <see cref="SharedKernel.Error" /> so the API can answer with the right status.
/// </summary>
public sealed class DatabaseOperationException(Error error) : Exception(error.Description)
{
    public Error Error { get; } = error;
}
