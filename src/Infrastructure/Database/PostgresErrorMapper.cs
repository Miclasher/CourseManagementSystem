using Npgsql;
using SharedKernel;

namespace Infrastructure.Database;

internal static class PostgresErrorMapper
{
    private const string UniqueViolation = "23505";
    private const string ForeignKeyViolation = "23503";
    private const string CheckViolation = "23514";

    // Procedures raise custom SQLSTATEs with the error code in HINT: CM400 problem, CM404 not found, CM409 conflict.
    public static Error? Map(PostgresException exception)
    {
        string code = string.IsNullOrWhiteSpace(exception.Hint) ? "Database.Error" : exception.Hint;
        string description = exception.MessageText;

        return exception.SqlState switch
        {
            "CM400" => Error.Problem(code, description),
            "CM404" => Error.NotFound(code, description),
            "CM409" => Error.Conflict(code, description),
            UniqueViolation => Error.Conflict("Database.UniqueViolation", "A record with the same unique value already exists."),
            ForeignKeyViolation => Error.Problem("Database.ForeignKeyViolation", "A referenced record does not exist."),
            CheckViolation => Error.Problem("Database.CheckViolation", "The data violates a database constraint."),
            _ => null
        };
    }
}
