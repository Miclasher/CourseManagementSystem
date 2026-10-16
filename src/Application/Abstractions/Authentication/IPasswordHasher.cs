namespace Application.Abstractions.Authentication;

public interface IPasswordHasher
{
    string Hash(string password);
}
