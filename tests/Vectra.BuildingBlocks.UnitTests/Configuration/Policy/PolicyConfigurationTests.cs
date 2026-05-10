using FluentAssertions;
using Vectra.BuildingBlocks.Configuration.Policy;
using Xunit;

namespace Vectra.BuildingBlocks.UnitTests.Configuration.Policy;

public class PolicyConfigurationTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var config = new PolicyConfiguration();

        config.Enabled.Should().BeTrue();
        config.DefaultProvider.Should().Be("Internal");
        config.Providers.Should().NotBeNull();
    }

    [Fact]
    public void Providers_ShouldHaveDefaultInternalAndOpaConfigurations()
    {
        var config = new PolicyConfiguration();

        config.Providers.Internal.Should().NotBeNull();
        config.Providers.Opa.Should().NotBeNull();
    }

    [Fact]
    public void InternalPolicyConfiguration_DefaultDirectory_ShouldBeEmpty()
    {
        var config = new InternalPolicyConfiguration();

        config.Directory.Should().BeEmpty();
    }

    [Fact]
    public void InternalPolicyConfiguration_ShouldAllowSettingDirectory()
    {
        var config = new InternalPolicyConfiguration { Directory = "/policies" };

        config.Directory.Should().Be("/policies");
    }

    [Fact]
    public void OpaPolicyConfiguration_DefaultValues_ShouldBeCorrect()
    {
        var config = new OpaPolicyConfiguration();

        config.BaseUrl.Should().BeEmpty();
        config.Path.Should().Be("/v1/data/vectra/authz");
        config.Timeout.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void OpaPolicyConfiguration_ShouldAllowCustomValues()
    {
        var config = new OpaPolicyConfiguration
        {
            BaseUrl = "http://opa:8181",
            Path = "/v1/data/custom",
            Timeout = TimeSpan.FromSeconds(10)
        };

        config.BaseUrl.Should().Be("http://opa:8181");
        config.Path.Should().Be("/v1/data/custom");
        config.Timeout.Should().Be(TimeSpan.FromSeconds(10));
    }
}
