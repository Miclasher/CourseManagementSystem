using SharedKernel;

namespace Domain.Users;

public static class UserErrors
{
    public static Error NotFound(Guid userId) => Error.NotFound(
        "Users.NotFound",
        $"The user with the Id = '{userId}' was not found");

    public static Error EmailNotUnique => Error.Conflict(
        "Users.EmailNotUnique",
        "The provided email is already in use");

    public static Error Unauthorized() => Error.Problem(
        "Users.Unauthorized",
        "You are not allowed to perform this action");
}
