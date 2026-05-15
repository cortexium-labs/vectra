using Vectra.BuildingBlocks.Errors;
using Vectra.BuildingBlocks.Exceptions;

namespace Vectra.UnitTests.Exceptions;

public class SystemExceptionTests
{
    // Concrete subclass for testing the abstract base
    private sealed class ConcreteSystemException : Vectra.Exceptions.SystemException
    {
        public ConcreteSystemException(ErrorCode errorCode, string message, Exception? inner = null)
            : base(errorCode, message, inner) { }
    }

    private static readonly ErrorCode TestCode = new(900_002, ErrorCategory.System);

    [Fact]
    public void Constructor_SetsErrorCode()
    {
        var ex = new ConcreteSystemException(TestCode, "test");
        ex.ErrorCode.Should().Be(TestCode);
    }

    [Fact]
    public void Constructor_MessageContainsErrorCodePrefix()
    {
        var ex = new ConcreteSystemException(TestCode, "test message");
        ex.Message.Should().Contain(ErrorCode.Prefix);
    }

    [Fact]
    public void Constructor_WithInnerException_SetsInnerException()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new ConcreteSystemException(TestCode, "outer", inner);
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void Constructor_WithoutInnerException_InnerExceptionIsNull()
    {
        var ex = new ConcreteSystemException(TestCode, "message");
        ex.InnerException.Should().BeNull();
    }

    [Fact]
    public void IsBaseException_Subclass()
    {
        var ex = new ConcreteSystemException(TestCode, "message");
        ex.Should().BeAssignableTo<BaseException>();
    }

    [Fact]
    public void ToString_ContainsErrorCode()
    {
        var ex = new ConcreteSystemException(TestCode, "message");
        ex.ToString().Should().Contain(TestCode.ToString());
    }
}
