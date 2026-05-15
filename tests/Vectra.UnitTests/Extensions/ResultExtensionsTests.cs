using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Vectra.BuildingBlocks.Errors;
using Vectra.BuildingBlocks.Results;
using Vectra.Extensions;

namespace Vectra.UnitTests.Extensions;

public class ResultExtensionsTests
{
    private static readonly ErrorCode TestErrorCode = new(1234, ErrorCategory.Core);

    // ── Result (non-generic) ───────────────────────────────────────────────

    [Fact]
    public void ToHttpResult_SuccessResult_ReturnsNoContent()
    {
        var result = Result.Success();
        var httpResult = result.ToHttpResult();

        httpResult.Should().NotBeNull();
        // NoContent returns a 204 result
        AssertStatusCode(httpResult, 204);
    }

    [Fact]
    public void ToHttpResult_FailureNotFound_Returns404()
    {
        var error = Error.NotFound(TestErrorCode, "Not found");
        var result = Result.Failure(error);
        var httpResult = result.ToHttpResult();

        AssertStatusCode(httpResult, 404);
    }

    [Fact]
    public void ToHttpResult_FailureConflict_Returns409()
    {
        var error = Error.Conflict(TestErrorCode, "Conflict");
        var result = Result.Failure(error);
        var httpResult = result.ToHttpResult();

        AssertStatusCode(httpResult, 409);
    }

    [Fact]
    public void ToHttpResult_FailureUnauthorized_Returns401()
    {
        var error = Error.Unauthorized(TestErrorCode, "Unauthorized");
        var result = Result.Failure(error);
        var httpResult = result.ToHttpResult();

        AssertStatusCode(httpResult, 401);
    }

    [Fact]
    public void ToHttpResult_FailureForbidden_Returns403()
    {
        var error = Error.Forbidden(TestErrorCode, "Forbidden");
        var result = Result.Failure(error);
        var httpResult = result.ToHttpResult();

        AssertStatusCode(httpResult, 403);
    }

    [Fact]
    public void ToHttpResult_FailureGeneric_Returns500()
    {
        var error = Error.Failure(TestErrorCode, "Internal error");
        var result = Result.Failure(error);
        var httpResult = result.ToHttpResult();

        AssertStatusCode(httpResult, 500);
    }

    [Fact]
    public void ToHttpResult_FailureValidation_Returns400()
    {
        var validationErrors = new Dictionary<string, string[]> { { "field", new[] { "is required" } } };
        var error = Error.Validation(TestErrorCode, "Validation failed", validationErrors);
        var result = Result.Failure(error);
        var httpResult = result.ToHttpResult();

        AssertStatusCode(httpResult, 400);
    }

    // ── Result<T> ─────────────────────────────────────────────────────────

    [Fact]
    public void ToHttpResult_SuccessResultOfT_Returns200WithValue()
    {
        var result = Result<string>.Success("hello");
        var httpResult = result.ToHttpResult();

        AssertStatusCode(httpResult, 200);
    }

    [Fact]
    public void ToHttpResult_FailureResultOfT_ReturnsErrorStatus()
    {
        var error = Error.NotFound(TestErrorCode, "Not found");
        var result = Result<string>.Failure(error);
        var httpResult = result.ToHttpResult();

        AssertStatusCode(httpResult, 404);
    }

    // ── PaginatedResult<T> ────────────────────────────────────────────────

    [Fact]
    public void ToHttpResult_SuccessPaginatedResult_Returns200()
    {
        var items = new List<string> { "a", "b" };
        var result = PaginatedResult<string>.Success(items, 1, 10, 2);
        var httpResult = result.ToHttpResult();

        AssertStatusCode(httpResult, 200);
    }

    [Fact]
    public void ToHttpResult_FailurePaginatedResult_ReturnsErrorStatus()
    {
        var error = Error.NotFound(TestErrorCode, "No items");
        var result = PaginatedResult<string>.Failure(error);
        var httpResult = result.ToHttpResult();

        AssertStatusCode(httpResult, 404);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static void AssertStatusCode(IResult httpResult, int expectedStatus)
        => HttpTestHelpers.ExecuteAndGetStatusCode(httpResult).Should().Be(expectedStatus);
}
