using System.Data.Common;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Dapper;
using Domain.Users;

namespace Infrastructure.Users;

internal sealed class UserRepository(
    ISqlConnectionFactory connectionFactory,
    IUnitOfWork unitOfWork,
    IUserContext userContext) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using DbConnection connection = await connectionFactory.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<User>(new CommandDefinition(
            "SELECT id, email, first_name, last_name, roles FROM v_active_users WHERE id = @id",
            new { id },
            cancellationToken: cancellationToken));
    }

    // Registration is anonymous, so the new user is its own actor.
    public void Add(User user) =>
        unitOfWork.Enqueue(
            (connection, transaction, ct) => connection.ExecuteAsync(new CommandDefinition(
                "CALL sp_register_user(@Id::uuid, @Email::varchar, @PasswordHash::varchar, @FirstName::varchar, " +
                "@LastName::varchar, @RoleId::smallint, @Id::uuid)",
                new
                {
                    user.Id,
                    user.Email,
                    user.PasswordHash,
                    user.FirstName,
                    user.LastName,
                    RoleId = (short)user.Role
                },
                transaction,
                cancellationToken: ct)),
            user);

    public void Deactivate(User user) =>
        unitOfWork.Enqueue(
            (connection, transaction, ct) => connection.ExecuteAsync(new CommandDefinition(
                "CALL sp_deactivate_user(@id::uuid, @actor::uuid)",
                new { id = user.Id, actor = userContext.UserId },
                transaction,
                cancellationToken: ct)),
            user);
}
