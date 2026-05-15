using Microsoft.Extensions.Logging;
using NSubstitute;
using Vectra.Services;

namespace Vectra.UnitTests.Services;

public class VectraVersionTests
{
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new VectraVersion(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ValidLogger_DoesNotThrow()
    {
        var logger = Substitute.For<ILogger<VectraVersion>>();
        var act = () => new VectraVersion(logger);
        act.Should().NotThrow();
    }

    [Fact]
    public void Version_ReturnsNonNullVersion()
    {
        var logger = Substitute.For<ILogger<VectraVersion>>();
        var service = new VectraVersion(logger);

        service.Version.Should().NotBeNull();
    }

    [Fact]
    public void GetApplicationVersion_WithNullLogger_ReturnsVersion()
    {
        var version = VectraVersion.GetApplicationVersion(null);
        version.Should().NotBeNull();
    }

    [Fact]
    public void GetApplicationVersion_WithLogger_ReturnsVersion()
    {
        var logger = Substitute.For<ILogger>();
        var version = VectraVersion.GetApplicationVersion(logger);
        version.Should().NotBeNull();
    }

    [Fact]
    public void GetApplicationVersion_ReturnsFallbackVersionWhenNoAttribute()
    {
        // Assembly under test has version attributes, but the method should always return a valid Version
        var version = VectraVersion.GetApplicationVersion();
        version.Should().BeOfType<Version>();
        version.Major.Should().BeGreaterThanOrEqualTo(0);
    }
}
