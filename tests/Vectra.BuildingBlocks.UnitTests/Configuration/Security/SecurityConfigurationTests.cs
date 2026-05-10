using FluentAssertions;
using Vectra.BuildingBlocks.Configuration.Security;
using Vectra.BuildingBlocks.Configuration.Security.AgentAuth;
using Xunit;

namespace Vectra.BuildingBlocks.UnitTests.Configuration.Security;

public class SecurityConfigurationTests
{
    [Fact]
    public void DefaultValues_ShouldInitializeAgentAuth()
    {
        var config = new SecurityConfiguration();

        config.AgentAuth.Should().NotBeNull();
    }

    [Fact]
    public void AgentAuthConfiguration_DefaultValues_ShouldBeCorrect()
    {
        var config = new AgentAuthConfiguration();

        config.Provider.Should().Be(AgentAuthProviderType.SelfSigned);
        config.SelfSigned.Should().NotBeNull();
        config.Jwt.Should().NotBeNull();
    }

    [Fact]
    public void SelfSignedProvider_DefaultValues_ShouldBeCorrect()
    {
        var config = new SelfSignedProvider();

        config.Secret.Should().BeEmpty();
        config.Issuer.Should().BeEmpty();
        config.Audience.Should().BeEmpty();
        config.Expiration.Should().Be(TimeSpan.FromMinutes(15));
    }

    [Fact]
    public void SelfSignedProvider_ShouldAllowCustomValues()
    {
        var config = new SelfSignedProvider
        {
            Secret = "my-secret",
            Issuer = "my-issuer",
            Audience = "my-audience",
            Expiration = TimeSpan.FromHours(1)
        };

        config.Secret.Should().Be("my-secret");
        config.Issuer.Should().Be("my-issuer");
        config.Audience.Should().Be("my-audience");
        config.Expiration.Should().Be(TimeSpan.FromHours(1));
    }

    [Fact]
    public void JwtProvider_DefaultValues_ShouldBeCorrect()
    {
        var config = new JwtProvider();

        config.Authority.Should().BeEmpty();
        config.Audience.Should().BeEmpty();
        config.MetadataUrl.Should().BeEmpty();
        config.ValidateIssuer.Should().BeTrue();
        config.ValidateAudience.Should().BeTrue();
    }

    [Fact]
    public void JwtProvider_ShouldAllowCustomValues()
    {
        var config = new JwtProvider
        {
            Authority = "https://auth.example.com",
            Audience = "api",
            MetadataUrl = "https://auth.example.com/.well-known/openid-configuration",
            ValidateIssuer = false,
            ValidateAudience = false
        };

        config.Authority.Should().Be("https://auth.example.com");
        config.Audience.Should().Be("api");
        config.ValidateIssuer.Should().BeFalse();
        config.ValidateAudience.Should().BeFalse();
    }

    [Theory]
    [InlineData(AgentAuthProviderType.SelfSigned)]
    [InlineData(AgentAuthProviderType.Jwt)]
    public void AgentAuthProviderType_AllValues_ShouldBeDefined(AgentAuthProviderType providerType)
    {
        Enum.IsDefined(providerType).Should().BeTrue();
    }
}
