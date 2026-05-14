using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Abstractions.Serializations;
using Vectra.BuildingBlocks.Configuration.Policy;
using Vectra.Domain.Policies;
using Vectra.Infrastructure.Policy;

namespace Vectra.Infrastructure.UnitTests.Policy;

public class FileSystemPolicyLoaderTests
{
    private readonly ILogger<FileSystemPolicyLoader> _logger = Substitute.For<ILogger<FileSystemPolicyLoader>>();
    private readonly IDeserializer _deserializer = Substitute.For<IDeserializer>();

    private FileSystemPolicyLoader CreateSut(string? directory)
    {
        var config = new PolicyConfiguration
        {
            Providers = new PolicyProviders
            {
                Internal = new InternalPolicyConfiguration { Directory = directory ?? string.Empty }
            }
        };
        return new FileSystemPolicyLoader(Options.Create(config), _logger, _deserializer);
    }

    [Fact]
    public async Task LoadAllAsync_EmptyDirectory_ReturnsEmptyDictionary()
    {
        var sut = CreateSut(null);

        var result = await sut.LoadAllAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAllAsync_NonExistentDirectory_ReturnsEmptyDictionary()
    {
        var sut = CreateSut("/non/existent/path");

        var result = await sut.LoadAllAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAllAsync_ValidDirectory_LoadsPoliciesFromJsonFiles()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var policyFile = Path.Combine(tempDir, "test-policy.json");
        await File.WriteAllTextAsync(policyFile, "{\"name\":\"test-policy\"}");

        var expectedPolicy = new PolicyDefinition { Name = "test-policy", Default = PolicyType.Allow };
        _deserializer.Deserialize<PolicyDefinition>(Arg.Any<string>()).Returns(expectedPolicy);

        var sut = CreateSut(tempDir);

        try
        {
            var result = await sut.LoadAllAsync(CancellationToken.None);

            result.Should().ContainKey("test-policy");
            result["test-policy"].Should().Be(expectedPolicy);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task LoadAllAsync_DeserializationFails_SkipsFileAndContinues()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        await File.WriteAllTextAsync(Path.Combine(tempDir, "bad-policy.json"), "invalid json");
        await File.WriteAllTextAsync(Path.Combine(tempDir, "good-policy.json"), "{\"name\":\"good-policy\"}");

        var goodPolicy = new PolicyDefinition { Name = "good-policy", Default = PolicyType.Allow };
        _deserializer.Deserialize<PolicyDefinition>(Arg.Is<string>(s => s.Contains("invalid")))
            .Returns(_ => throw new Exception("parse error"));
        _deserializer.Deserialize<PolicyDefinition>(Arg.Is<string>(s => s.Contains("good-policy")))
            .Returns(goodPolicy);

        var sut = CreateSut(tempDir);

        try
        {
            var result = await sut.LoadAllAsync(CancellationToken.None);

            result.Should().ContainKey("good-policy");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task LoadAllAsync_PolicyWithEmptyName_IsSkipped()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        await File.WriteAllTextAsync(Path.Combine(tempDir, "unnamed.json"), "{\"name\":\"\"}");

        var namelessPolicy = new PolicyDefinition { Name = string.Empty };
        _deserializer.Deserialize<PolicyDefinition>(Arg.Any<string>()).Returns(namelessPolicy);

        var sut = CreateSut(tempDir);

        try
        {
            var result = await sut.LoadAllAsync(CancellationToken.None);

            result.Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task GetPolicyAsync_ExistingPolicy_ReturnsPolicy()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        await File.WriteAllTextAsync(Path.Combine(tempDir, "my-policy.json"), "{\"name\":\"my-policy\"}");

        var policy = new PolicyDefinition { Name = "my-policy", Default = PolicyType.Allow };
        _deserializer.Deserialize<PolicyDefinition>(Arg.Any<string>()).Returns(policy);

        var sut = CreateSut(tempDir);

        try
        {
            var result = await sut.GetPolicyAsync("my-policy", CancellationToken.None);

            result.Should().NotBeNull();
            result!.Name.Should().Be("my-policy");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task GetPolicyAsync_NonExistentPolicy_ReturnsNull()
    {
        var sut = CreateSut(null);

        var result = await sut.GetPolicyAsync("missing", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new FileSystemPolicyLoader(null!, _logger, _deserializer);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var config = new PolicyConfiguration { Providers = new PolicyProviders { Internal = new InternalPolicyConfiguration() } };
        var act = () => new FileSystemPolicyLoader(Options.Create(config), null!, _deserializer);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullDeserializer_ThrowsArgumentNullException()
    {
        var config = new PolicyConfiguration { Providers = new PolicyProviders { Internal = new InternalPolicyConfiguration() } };
        var act = () => new FileSystemPolicyLoader(Options.Create(config), _logger, null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
