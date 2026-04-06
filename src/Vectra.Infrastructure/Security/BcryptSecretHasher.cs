using Vectra.Application.Abstractions.Security;

namespace Vectra.Infrastructure.Security;

public class BcryptSecretHasher : ISecretHasher
{
    public string HashPassword(string secret)
    {
        return BCrypt.Net.BCrypt.HashPassword(secret);
    }

    public bool Verify(string secret, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(secret, hash);
    }
}