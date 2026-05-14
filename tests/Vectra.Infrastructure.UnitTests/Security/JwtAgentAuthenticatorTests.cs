using FluentAssertions;
using NSubstitute;
using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Abstractions.Security;
using Vectra.Domain.Agents;
using Vectra.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Vectra.BuildingBlocks.Configuration.Security;
using Vectra.BuildingBlocks.Configuration.Security.AgentAuth;

namespace Vectra.Infrastructure.UnitTests.Security;

public class JwtAgentAuthenticatorTests
{
    private static JwtAgentAuthenticator CreateSut(
        AgentAuthProviderType provider = AgentAuthProviderType.SelfSigned,
        ITokenService? tokenService = null)
    {
        var config = new SecurityConfiguration
        {
            AgentAuth = new AgentAuthConfiguration
            {
                Provider = provider,
                SelfSigned = new SelfSignedProvider
                {
                    Secret = "super-secret-key-for-tests-1234567890",
                    Issuer = "vectra-issuer",
                    Audience = "vectra-audience",
                    Expiration = TimeSpan.FromMinutes(15)
                },
                Jwt = new JwtProvider()
            }
        };
        tokenService ??= Substitute.For<ITokenService>();
        return new JwtAgentAuthenticator(Options.Create(config), tokenService);
    }

    [Fact]
    public void Authenticate_SelfSignedProvider_GeneratesToken()
    {
        var tokenService = Substitute.For<ITokenService>();
        tokenService.GenerateToken(Arg.Any<Agent>()).Returns("generated-token");
        var sut = CreateSut(AgentAuthProviderType.SelfSigned, tokenService);
        var agent = new Agent("TestAgent", "owner-1", "hash");

        var result = sut.Authenticate(agent);

        result.Succeeded.Should().BeTrue();
        result.Token.Should().Be("generated-token");
        tokenService.Received(1).GenerateToken(agent);
    }

    [Fact]
    public void Authenticate_ExternalProvider_ReturnsFailure()
    {
        var sut = CreateSut(AgentAuthProviderType.Jwt);
        var agent = new Agent("TestAgent", "owner-1", "hash");

        var result = sut.Authenticate(agent);

        result.Succeeded.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ValidateAsync_SelfSigned_DelegatesToTokenService()
    {
        var tokenService = Substitute.For<ITokenService>();
        var expectedPrincipal = new System.Security.Claims.ClaimsPrincipal();
        tokenService.ValidateToken(Arg.Any<string>()).Returns(expectedPrincipal);
        var sut = CreateSut(AgentAuthProviderType.SelfSigned, tokenService);

        var result = await sut.ValidateAsync("some-token");

        result.Should().Be(expectedPrincipal);
        tokenService.Received(1).ValidateToken("some-token");
    }

    [Fact]
    public async Task ValidateAsync_SelfSigned_InvalidToken_ReturnsNull()
    {
        var tokenService = Substitute.For<ITokenService>();
        tokenService.ValidateToken(Arg.Any<string>()).Returns((System.Security.Claims.ClaimsPrincipal?)null);
        var sut = CreateSut(AgentAuthProviderType.SelfSigned, tokenService);

        var result = await sut.ValidateAsync("bad-token");

        result.Should().BeNull();
    }
}
