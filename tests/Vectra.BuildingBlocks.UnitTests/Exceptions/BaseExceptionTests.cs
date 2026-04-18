using FluentAssertions;
using Vectra.BuildingBlocks.Errors;
using Vectra.BuildingBlocks.Exceptions;
using Xunit;

namespace Vectra.BuildingBlocks.UnitTests.Exceptions;

file sealed class TestException : BaseException
{
    public TestException(ErrorCode errorCode, string message, Exception? innerException = null)
        : base(errorCode, message, innerException) { }

    public TestException(Error error, Exception? innerException = null)
        : base(error, innerException) { }
}

public class BaseExceptionTests
{
    [Fact]
    public void Constructor_WithErrorCodeAndMessage_ShouldSetProperties()
    {
        var errorCode = VectraErrors.SystemFailure;

        var ex = new TestException(errorCode, "System failed");

        ex.ErrorCode.Should().Be(errorCode);
        ex.Message.Should().Be($"{errorCode}: System failed");
        ex.InnerException.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithInnerException_ShouldSetInnerException()
    {
        var inner = new InvalidOperationException("inner");

        var ex = new TestException(VectraErrors.SystemFailure, "outer", inner);

        ex.InnerException.Should().Be(inner);
    }

    [Fact]
    public void Constructor_WithError_ShouldDeriveMessageFromError()
    {
        var error = Error.Failure(VectraErrors.SystemFailure, "Derived message");

        var ex = new TestException(error);

        ex.ErrorCode.Should().Be(VectraErrors.SystemFailure);
        ex.Message.Should().Contain("Derived message");
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        var errorCode = VectraErrors.SystemFailure;

        var ex = new TestException(errorCode, "System failed");

        ex.ToString().Should().StartWith($"{errorCode}: {errorCode}: System failed");
    }
}
