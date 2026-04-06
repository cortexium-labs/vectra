namespace Vectra.Application.Abstractions.Security;

public interface ISecretHasher
{
    string HashPassword(string secret);
    bool Verify(string secret, string hash);
}