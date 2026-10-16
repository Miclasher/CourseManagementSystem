using SharedKernel;

namespace Domain.Users;

public sealed class User : Entity
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string[] Roles { get; set; } = [];
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }

    public bool IsAdmin => Roles.Contains("admin", StringComparer.Ordinal);

    public static User Register(string email, string passwordHash, string firstName, string lastName, UserRole role)
    {
        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Email = email,
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            Role = role
        };

        user.Raise(new UserRegisteredDomainEvent(user.Id));

        return user;
    }

    public void Deactivate() => Raise(new UserDeactivatedDomainEvent(Id));
}
