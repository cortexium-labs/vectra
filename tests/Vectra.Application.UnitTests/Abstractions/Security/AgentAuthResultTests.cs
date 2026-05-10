using FluentAssertions;
using Vectra.Application.Abstractions.Security;

namespace Vectra.Application.UnitTests.Abstractions.Security;

public class AgentAuthResultTests
{
    [Fact]
    public void Success_WithToken_ShouldSetSucceededAndToken()
    {
        var result = AgentAuthResult.Success("my-jwt-token");

        result.Succeeded.Should().BeTrue();
        result.Token.Should().Be("my-jwt-token");
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Success_WithoutToken_ShouldSetSucceededWithNullToken()
    {
        var result = AgentAuthResult.Success();

        result.Succeeded.Should().BeTrue();
        result.Token.Should().BeNull();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Failure_ShouldSetSucceededFalseAndError()
    {
        var result = AgentAuthResult.Failure("invalid credentials");

        result.Succeeded.Should().BeFalse();
        result.Error.Should().Be("invalid credentials");
        result.Token.Should().BeNull();
    }
}
