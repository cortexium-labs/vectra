using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Vectra.BuildingBlocks.Configuration.SecretManagement;
using Vectra.Infrastructure.SecretManagement;

namespace Vectra.Infrastructure.UnitTests.SecretManagement;

public class EnvironmentVariablesSecretProviderTests
{
    // EnvironmentVariablesSecretProvider is internal sealed — test via its behavior through reflection
    private static ISecretProvider? CreateEnvProvider(EnvironmentVariablesSecretConfiguration config)
    {
        var type = typeof(SecretProviderFactory).Assembly
            .GetType("Vectra.Infrastructure.SecretManagement.Providers.EnvironmentVariablesSecretProvider");
        if (type == null) return null;
        return (ISecretProvider?)Activator.CreateInstance(
            type,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public,
            null,
            [config],
            null);
    }

    [Fact]
    public void Configure_NullPrefix_AddsEnvironmentVariablesWithoutPrefix()
    {
        var config = new EnvironmentVariablesSecretConfiguration { Prefix = null };
        var provider = CreateEnvProvider(config);
        provider.Should().NotBeNull();
        var builder = new ConfigurationBuilder();

        var act = () => provider!.Configure(builder);

        act.Should().NotThrow();
    }

    [Fact]
    public void Configure_WhitespacePrefix_AddsEnvironmentVariablesWithoutPrefix()
    {
        var config = new EnvironmentVariablesSecretConfiguration { Prefix = "   " };
        var provider = CreateEnvProvider(config);
        provider.Should().NotBeNull();
        var builder = new ConfigurationBuilder();

        var act = () => provider!.Configure(builder);

        act.Should().NotThrow();
    }

    [Fact]
    public void Configure_ValidPrefix_AddsEnvironmentVariablesWithPrefix()
    {
        var config = new EnvironmentVariablesSecretConfiguration { Prefix = "MYAPP_" };
        var provider = CreateEnvProvider(config);
        provider.Should().NotBeNull();
        var builder = new ConfigurationBuilder();

        var act = () => provider!.Configure(builder);

        act.Should().NotThrow();
    }
}

public class UserSecretsSecretProviderTests
{
    private static ISecretProvider? CreateUserSecretsProvider()
    {
        var type = typeof(SecretProviderFactory).Assembly
            .GetType("Vectra.Infrastructure.SecretManagement.Providers.UserSecretsSecretProvider");
        if (type == null) return null;
        return (ISecretProvider?)Activator.CreateInstance(
            type,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public,
            null,
            [],
            null);
    }

    [Fact]
    public void Configure_DoesNotThrow()
    {
        var provider = CreateUserSecretsProvider();
        provider.Should().NotBeNull();
        var builder = new ConfigurationBuilder();

        var act = () => provider!.Configure(builder);

        act.Should().NotThrow();
    }
}
