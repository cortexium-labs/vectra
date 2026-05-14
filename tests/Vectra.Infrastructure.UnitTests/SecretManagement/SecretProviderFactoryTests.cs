using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Vectra.BuildingBlocks.Configuration.SecretManagement;
using Vectra.Infrastructure.Caches;
using Vectra.Infrastructure.SecretManagement;
using Vectra.Infrastructure.SecretManagement.Providers;

namespace Vectra.Infrastructure.UnitTests.SecretManagement;

public class SecretProviderFactoryTests
{
    private static IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        return services.BuildServiceProvider();
    }

    private static SecretProviderFactory CreateSut(
        SecretManagementProviderType provider,
        IServiceProvider? sp = null)
    {
        var config = new SecretManagementConfiguration
        {
            DefaultProvider = provider,
            Providers = new SecretManagementProviders
            {
                EnvironmentVariables = new EnvironmentVariablesSecretConfiguration { Prefix = null },
                AzureKeyVault = new AzureKeyVaultSecretConfiguration { VaultUri = null }
            }
        };
        sp ??= BuildServiceProvider();
        var logger = NullLogger<SecretProviderFactory>.Instance;
        return new SecretProviderFactory(Options.Create(config), sp, logger);
    }

    [Fact]
    public void Create_NoneProvider_ReturnsNull()
    {
        var sut = CreateSut(SecretManagementProviderType.None);

        var result = sut.Create();

        result.Should().BeNull();
    }

    [Fact]
    public void Create_EnvironmentVariables_ReturnsProvider()
    {
        var sut = CreateSut(SecretManagementProviderType.EnvironmentVariables);

        var result = sut.Create();

        result.Should().NotBeNull();
    }

    [Fact]
    public void Create_UserSecrets_ReturnsProvider()
    {
        var sut = CreateSut(SecretManagementProviderType.UserSecrets);

        var result = sut.Create();

        result.Should().NotBeNull();
    }

    [Fact]
    public void Create_AzureKeyVault_ReturnsProvider()
    {
        var sut = CreateSut(SecretManagementProviderType.AzureKeyVault);

        // AzureKeyVaultSecretProvider is constructed successfully (Configure is not called here)
        var result = sut.Create();

        result.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_NullServiceProvider_ThrowsArgumentNullException()
    {
        var config = new SecretManagementConfiguration();
        var logger = NullLogger<SecretProviderFactory>.Instance;

        var act = () => new SecretProviderFactory(Options.Create(config), null!, logger);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var config = new SecretManagementConfiguration();
        var sp = BuildServiceProvider();

        var act = () => new SecretProviderFactory(Options.Create(config), sp, null!);

        act.Should().Throw<ArgumentNullException>();
    }
}

public class SecretManagementServiceTests
{
    [Fact]
    public void Constructor_UsesFactoryToCreateCurrentProvider()
    {
        var factory = Substitute.For<ISecretProviderFactory>();
        var provider = Substitute.For<ISecretProvider>();
        factory.Create().Returns(provider);

        var sut = new SecretManagementService(factory);

        sut.Current.Should().Be(provider);
        factory.Received(1).Create();
    }

    [Fact]
    public void Constructor_FactoryReturnsNull_CurrentIsNull()
    {
        var factory = Substitute.For<ISecretProviderFactory>();
        factory.Create().Returns((ISecretProvider?)null);

        var sut = new SecretManagementService(factory);

        sut.Current.Should().BeNull();
    }
}
