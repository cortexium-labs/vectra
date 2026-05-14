using FluentAssertions;
using Vectra.Infrastructure.Security;

namespace Vectra.Infrastructure.UnitTests.Security;

public class BcryptSecretHasherTests
{
    private readonly BcryptSecretHasher _sut = new();

    [Fact]
    public void HashPassword_ReturnsNonEmptyHash()
    {
        var hash = _sut.HashPassword("mySecret123");

        hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void HashPassword_SameInput_ReturnsDifferentHashes()
    {
        var hash1 = _sut.HashPassword("mySecret123");
        var hash2 = _sut.HashPassword("mySecret123");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Verify_CorrectSecret_ReturnsTrue()
    {
        var secret = "correctSecret!";
        var hash = _sut.HashPassword(secret);

        var result = _sut.Verify(secret, hash);

        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_WrongSecret_ReturnsFalse()
    {
        var hash = _sut.HashPassword("correctSecret");

        var result = _sut.Verify("wrongSecret", hash);

        result.Should().BeFalse();
    }

    [Fact]
    public void Verify_EmptySecretAgainstHash_ReturnsFalse()
    {
        var hash = _sut.HashPassword("someSecret");

        var result = _sut.Verify(string.Empty, hash);

        result.Should().BeFalse();
    }

    [Fact]
    public void HashPassword_EmptyString_ReturnsHash()
    {
        var hash = _sut.HashPassword(string.Empty);

        hash.Should().NotBeNullOrEmpty();
    }
}
