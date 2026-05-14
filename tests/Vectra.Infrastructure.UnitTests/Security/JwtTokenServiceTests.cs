using FluentAssertions;
using Microsoft.Extensions.Options;
using Vectra.BuildingBlocks.Configuration.Security;
using Vectra.BuildingBlocks.Configuration.Security.AgentAuth;
using Vectra.Domain.Agents;
using Vectra.Infrastructure.Security;

namespace Vectra.Infrastructure.UnitTests.Security;

public class JwtTokenServiceTests
{
    private static JwtTokenService CreateSut(
        string secret = "super-secret-key-for-testing-1234567890",
        string issuer = "vectra-issuer",
        string audience = "vectra-audience",
        TimeSpan? expiration = null)
    {
        var config = new SecurityConfiguration
        {
            AgentAuth = new AgentAuthConfiguration
            {
                SelfSigned = new SelfSignedProvider
                {
                    Secret = secret,
                    Issuer = issuer,
                    Audience = audience,
                    Expiration = expiration ?? TimeSpan.FromMinutes(15)
                }
            }
        };
        return new JwtTokenService(Options.Create(config));
    }

    [Fact]
    public void GenerateToken_ValidAgent_ReturnsNonEmptyToken()
    {
        var sut = CreateSut();
        var agent = new Agent("TestAgent", "owner-1", "hash");

        var token = sut.GenerateToken(agent);

        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateToken_EmptySecret_ThrowsInvalidOperationException()
    {
        var sut = CreateSut(secret: string.Empty);
        var agent = new Agent("TestAgent", "owner-1", "hash");

        var act = () => sut.GenerateToken(agent);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ValidateToken_ValidToken_ReturnsPrincipalWithClaims()
    {
        var sut = CreateSut();
        var agent = new Agent("TestAgent", "owner-1", "hash");
        var token = sut.GenerateToken(agent);

        var principal = sut.ValidateToken(token);

        principal.Should().NotBeNull();
        principal!.FindFirst("agent_name")?.Value.Should().Be("TestAgent");
        principal.FindFirst("trust_score")?.Value.Should().Be(agent.TrustScore.ToString());
    }

    [Fact]
    public void ValidateToken_InvalidToken_ReturnsNull()
    {
        var sut = CreateSut();

        var principal = sut.ValidateToken("this.is.not.a.valid.jwt");

        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_TokenSignedWithDifferentSecret_ReturnsNull()
    {
        var generator = CreateSut(secret: "super-secret-key-for-testing-1234567890");
        var validator = CreateSut(secret: "different-secret-key-for-testing-9999999");
        var agent = new Agent("TestAgent", "owner-1", "hash");
        var token = generator.GenerateToken(agent);

        var principal = validator.ValidateToken(token);

        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_ExpiredToken_ReturnsNull()
    {
        var sut = CreateSut(expiration: TimeSpan.FromSeconds(-1));
        var agent = new Agent("TestAgent", "owner-1", "hash");
        var token = sut.GenerateToken(agent);

        var principal = sut.ValidateToken(token);

        principal.Should().BeNull();
    }

    [Fact]
    public void ValidateToken_EmptySecret_ThrowsInvalidOperationException()
    {
        var sut = CreateSut(secret: string.Empty);

        var act = () => sut.ValidateToken("some.token.value");

        act.Should().Throw<InvalidOperationException>();
    }
}
