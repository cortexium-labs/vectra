using FluentAssertions;
using Vectra.Infrastructure.Exceptions;
using Vectra.Infrastructure.Errors;

namespace Vectra.Infrastructure.UnitTests.Exceptions;

public class JsonSerializationExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_StoresMessage()
    {
        var ex = new JsonSerializationException("Something went wrong");

        ex.Message.Should().Contain("Something went wrong");
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_StoresInnerException()
    {
        var inner = new Exception("root cause");

        var ex = new JsonSerializationException("Outer message", inner);

        ex.InnerException.Should().Be(inner);
        ex.Message.Should().Contain("Outer message");
    }

    [Fact]
    public void IsSerializationException_ByInheritance()
    {
        var ex = new JsonSerializationException("test");

        ex.Should().BeAssignableTo<SerializationException>();
    }
}

public class JsonSerializationInputRequiredExceptionTests
{
    [Fact]
    public void Constructor_WithType_ContainsTypeName()
    {
        var ex = new JsonSerializationInputRequiredException(typeof(string));

        ex.Message.Should().Contain("String");
    }

    [Fact]
    public void IsSerializationException_ByInheritance()
    {
        var ex = new JsonSerializationInputRequiredException(typeof(int));

        ex.Should().BeAssignableTo<SerializationException>();
    }
}
