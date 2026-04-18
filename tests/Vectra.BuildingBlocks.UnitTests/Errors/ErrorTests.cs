using FluentAssertions;
using Vectra.BuildingBlocks.Errors;
using Vectra.BuildingBlocks.Results;
using Xunit;

namespace Vectra.BuildingBlocks.UnitTests.Errors;

public class ErrorTests
{
    [Fact]
    public void Validation_ShouldCreateErrorWithValidationType()
    {
        var validationErrors = new Dictionary<string, string[]> { { "Name", ["Name is required"] } };

        var error = Error.Validation(VectraErrors.ValidationFailed, "Validation failed", validationErrors);

        error.Type.Should().Be(ErrorType.Validation);
        error.ErrorCode.Should().Be(VectraErrors.ValidationFailed);
        error.Message.Should().Be("Validation failed");
        error.ValidationErrors.Should().BeEquivalentTo(validationErrors);
    }

    [Fact]
    public void NotFound_ShouldCreateErrorWithNotFoundType()
    {
        var error = Error.NotFound(VectraErrors.ResourceNotFound, "Resource not found");

        error.Type.Should().Be(ErrorType.NotFound);
        error.ErrorCode.Should().Be(VectraErrors.ResourceNotFound);
        error.Message.Should().Be("Resource not found");
        error.ValidationErrors.Should().BeNull();
    }

    [Fact]
    public void Conflict_ShouldCreateErrorWithConflictType()
    {
        var error = Error.Conflict(VectraErrors.DuplicateResource, "Duplicate resource");

        error.Type.Should().Be(ErrorType.Conflict);
        error.Message.Should().Be("Duplicate resource");
    }

    [Fact]
    public void Unauthorized_ShouldCreateErrorWithUnauthorizedType()
    {
        var error = Error.Unauthorized(VectraErrors.Unauthorized, "Unauthorized access");

        error.Type.Should().Be(ErrorType.Unauthorized);
        error.Message.Should().Be("Unauthorized access");
    }

    [Fact]
    public void Forbidden_ShouldCreateErrorWithForbiddenType()
    {
        var error = Error.Forbidden(VectraErrors.AccessDenied, "Access denied");

        error.Type.Should().Be(ErrorType.Forbidden);
        error.Message.Should().Be("Access denied");
    }

    [Fact]
    public void Failure_ShouldCreateErrorWithFailureType()
    {
        var error = Error.Failure(VectraErrors.SystemFailure, "System failure");

        error.Type.Should().Be(ErrorType.Failure);
        error.Message.Should().Be("System failure");
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        var error = Error.Failure(VectraErrors.SystemFailure, "System failure");

        error.ToString().Should().Be($"{VectraErrors.SystemFailure}: System failure");
    }

    [Theory]
    [InlineData(400000, ErrorCategory.Core, ErrorType.Validation)]
    [InlineData(404000, ErrorCategory.Persistence, ErrorType.NotFound)]
    [InlineData(409000, ErrorCategory.Persistence, ErrorType.Conflict)]
    [InlineData(401000, ErrorCategory.Security, ErrorType.Unauthorized)]
    [InlineData(403000, ErrorCategory.Security, ErrorType.Forbidden)]
    [InlineData(1, ErrorCategory.System, ErrorType.Failure)]
    public void FromCode_ShouldMapErrorCodeToCorrectErrorType(int codeValue, ErrorCategory category, ErrorType expectedType)
    {
        var errorCode = new ErrorCode(codeValue, category);

        var error = Error.FromCode(errorCode, "test message");

        error.Type.Should().Be(expectedType);
    }

    [Fact]
    public void FromCode_WithValidationErrors_ShouldIncludeValidationErrors()
    {
        var validationErrors = new Dictionary<string, string[]> { { "Field", ["Required"] } };
        var errorCode = new ErrorCode(400000, ErrorCategory.Core);

        var error = Error.FromCode(errorCode, "Validation failed", validationErrors);

        error.ValidationErrors.Should().BeEquivalentTo(validationErrors);
    }
}
