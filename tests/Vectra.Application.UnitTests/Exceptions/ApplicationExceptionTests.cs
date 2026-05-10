using FluentAssertions;
using Vectra.Application.Errors;
using Vectra.Application.Exceptions;
using Vectra.BuildingBlocks.Errors;
using AppException = Vectra.Application.Exceptions.ApplicationException;

namespace Vectra.Application.UnitTests.Exceptions;

file sealed class ConcreteApplicationException : AppException
{
    public ConcreteApplicationException(ErrorCode errorCode, string message)
        : base(errorCode, message) { }
}

public class ApplicationExceptionTests
{
    [Fact]
    public void Constructor_ShouldSetErrorCodeAndMessage()
    {
        var exception = new ConcreteApplicationException(
            ApplicationErrorCodes.AgentNotFound, "Agent was not found");

        exception.ErrorCode.Should().Be(ApplicationErrorCodes.AgentNotFound);
        exception.Message.Should().Contain("Agent was not found");
    }

    [Fact]
    public void IsException_ShouldInheritFromSystemException()
    {
        var exception = new ConcreteApplicationException(
            ApplicationErrorCodes.PolicyNotFound, "Policy not found");

        exception.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void ToString_ShouldIncludeErrorCode()
    {
        var exception = new ConcreteApplicationException(
            ApplicationErrorCodes.AgentNotFound, "Agent missing");

        exception.ToString().Should().Contain("VEC");
    }
}
