using FluentAssertions;
using Vectra.Infrastructure.Persistence.Common.Errors;
using Vectra.Infrastructure.Persistence.Common.Exceptions;

namespace Vectra.Infrastructure.Persistence.Common.UnitTests.Exceptions;

public class DatabaseModelCreatingExceptionTests
{
    [Fact]
    public void Constructor_ShouldSetInnerException()
    {
        var inner = new InvalidOperationException("model error");

        var ex = new DatabaseModelCreatingException(inner);

        ex.InnerException.Should().Be(inner);
    }

    [Fact]
    public void Constructor_ShouldSetCorrectErrorCode()
    {
        var ex = new DatabaseModelCreatingException(new Exception());

        ex.ErrorCode.Should().Be(PersistenceErrorCodes.DatabaseModelCreating);
    }

    [Fact]
    public void Constructor_ShouldContainExpectedMessage()
    {
        var ex = new DatabaseModelCreatingException(new Exception());

        ex.Message.Should().Contain("An error occurred while building the EF Core model.");
    }

    [Fact]
    public void ShouldBe_PersistenceException()
    {
        var ex = new DatabaseModelCreatingException(new Exception());

        ex.Should().BeAssignableTo<PersistenceException>();
    }
}
