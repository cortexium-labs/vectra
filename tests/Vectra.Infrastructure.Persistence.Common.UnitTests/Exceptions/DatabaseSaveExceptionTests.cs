using FluentAssertions;
using Vectra.Infrastructure.Persistence.Common.Errors;
using Vectra.Infrastructure.Persistence.Common.Exceptions;

namespace Vectra.Infrastructure.Persistence.Common.UnitTests.Exceptions;

public class DatabaseSaveExceptionTests
{
    [Fact]
    public void Constructor_ShouldSetInnerException()
    {
        var inner = new InvalidOperationException("db error");

        var ex = new DatabaseSaveException(inner);

        ex.InnerException.Should().Be(inner);
    }

    [Fact]
    public void Constructor_ShouldSetCorrectErrorCode()
    {
        var ex = new DatabaseSaveException(new Exception());

        ex.ErrorCode.Should().Be(PersistenceErrorCodes.DatabaseSaveData);
    }

    [Fact]
    public void Constructor_ShouldContainExpectedMessage()
    {
        var ex = new DatabaseSaveException(new Exception());

        ex.Message.Should().Contain("Failed to save changes to the database.");
    }

    [Fact]
    public void ShouldBe_PersistenceException()
    {
        var ex = new DatabaseSaveException(new Exception());

        ex.Should().BeAssignableTo<PersistenceException>();
    }
}
