using FluentAssertions;
using Vectra.BuildingBlocks.Errors;
using Vectra.BuildingBlocks.Results;
using Xunit;

namespace Vectra.BuildingBlocks.UnitTests.Results;

public class ResultTests
{
    [Fact]
    public void Success_ShouldReturnSuccessResult()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task SuccessAsync_ShouldReturnSuccessResult()
    {
        var result = await Result.SuccessAsync();

        result.IsSuccess.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Failure_ShouldReturnFailureResult()
    {
        var error = Error.Failure(VectraErrors.SystemFailure, "Something went wrong");

        var result = Result.Failure(error);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(error);
    }

    [Fact]
    public async Task FailureAsync_ShouldReturnFailureResult()
    {
        var error = Error.Failure(VectraErrors.SystemFailure, "Something went wrong");

        var result = await Result.FailureAsync(error);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(error);
    }
}

public class ResultOfTTests
{
    [Fact]
    public void Success_ShouldReturnSuccessResultWithValue()
    {
        var result = Result<string>.Success("hello");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("hello");
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task SuccessAsync_ShouldReturnSuccessResultWithValue()
    {
        var result = await Result<int>.SuccessAsync(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Failure_ShouldReturnFailureResultWithError()
    {
        var error = Error.NotFound(VectraErrors.ResourceNotFound, "Resource not found");

        var result = Result<string>.Failure(error);

        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
        result.Error.Should().Be(error);
    }

    [Fact]
    public async Task FailureAsync_ShouldReturnFailureResultWithError()
    {
        var error = Error.NotFound(VectraErrors.ResourceNotFound, "Resource not found");

        var result = await Result<string>.FailureAsync(error);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(error);
    }

    [Fact]
    public void ImplicitConversion_FromValue_ShouldReturnSuccess()
    {
        Result<string> result = "implicit-value";

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("implicit-value");
    }

    [Fact]
    public void ImplicitConversion_FromError_ShouldReturnFailure()
    {
        var error = Error.Conflict(VectraErrors.DuplicateResource, "Duplicate resource");

        Result<string> result = error;

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(error);
    }
}
