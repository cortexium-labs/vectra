using FluentAssertions;
using Vectra.BuildingBlocks.Configuration.SecretManagement;
using Xunit;

namespace Vectra.BuildingBlocks.UnitTests.Configuration.SecretManagement;

public class SecretManagementConfigurationTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var config = new SecretManagementConfiguration();

        config.DefaultProvider.Should().Be(SecretManagementProviderType.None);
        config.Providers.Should().NotBeNull();
    }

    [Fact]
    public void Providers_ShouldInitializeAllProviders()
    {
        var providers = new SecretManagementProviders();

        providers.EnvironmentVariables.Should().NotBeNull();
        providers.AzureKeyVault.Should().NotBeNull();
    }

    [Fact]
    public void EnvironmentVariablesSecretConfiguration_DefaultPrefix_ShouldBeNull()
    {
        var config = new EnvironmentVariablesSecretConfiguration();

        config.Prefix.Should().BeNull();
    }

    [Fact]
    public void EnvironmentVariablesSecretConfiguration_ShouldAllowSettingPrefix()
    {
        var config = new EnvironmentVariablesSecretConfiguration { Prefix = "MYAPP_" };

        config.Prefix.Should().Be("MYAPP_");
    }

    [Fact]
    public void AzureKeyVaultSecretConfiguration_DefaultValues_ShouldBeCorrect()
    {
        var config = new AzureKeyVaultSecretConfiguration();

        config.VaultUri.Should().BeNull();
        config.Optional.Should().BeFalse();
        config.SecretPrefix.Should().BeNull();
        config.ReloadOnChange.Should().BeFalse();
        config.ReloadInterval.Should().Be(TimeSpan.FromMinutes(30));
    }

    [Fact]
    public void AzureKeyVaultSecretConfiguration_ShouldAllowCustomValues()
    {
        var config = new AzureKeyVaultSecretConfiguration
        {
            VaultUri = "https://myvault.vault.azure.net/",
            Optional = true,
            SecretPrefix = "vectra-",
            ReloadOnChange = true,
            ReloadInterval = TimeSpan.FromMinutes(10)
        };

        config.VaultUri.Should().Be("https://myvault.vault.azure.net/");
        config.Optional.Should().BeTrue();
        config.SecretPrefix.Should().Be("vectra-");
        config.ReloadOnChange.Should().BeTrue();
        config.ReloadInterval.Should().Be(TimeSpan.FromMinutes(10));
    }

    [Theory]
    [InlineData(SecretManagementProviderType.None)]
    [InlineData(SecretManagementProviderType.EnvironmentVariables)]
    [InlineData(SecretManagementProviderType.AzureKeyVault)]
    [InlineData(SecretManagementProviderType.UserSecrets)]
    public void SecretManagementProviderType_AllValues_ShouldBeDefined(SecretManagementProviderType providerType)
    {
        Enum.IsDefined(providerType).Should().BeTrue();
    }
}
