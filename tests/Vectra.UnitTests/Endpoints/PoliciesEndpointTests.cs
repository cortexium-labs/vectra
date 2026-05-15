using Microsoft.AspNetCore.Http;
using NSubstitute;
using Vectra.Application.Abstractions.Dispatchers;
using Vectra.Application.Features.Policies.PoliciesList;
using Vectra.Application.Features.Policies.PolicyDetails;
using Vectra.BuildingBlocks.Errors;
using Vectra.BuildingBlocks.Results;
using Vectra.Endpoints;

namespace Vectra.UnitTests.Endpoints;

public class PoliciesEndpointTests
{
    private readonly IDispatcher _dispatcher;
    private static readonly ErrorCode TestCode = new(1002, ErrorCategory.Core);

    public PoliciesEndpointTests()
    {
        _dispatcher = Substitute.For<IDispatcher>();
    }

    // ── PoliciesList ──────────────────────────────────────────────────────

    [Fact]
    public async Task PoliciesList_Success_Returns200()
    {
        var items = new List<PoliciesListResult> { new() { PolicyName = "default" } };
        var paginated = PaginatedResult<PoliciesListResult>.Success(items, 1, 25, 1);
        _dispatcher.Dispatch(Arg.Any<IAction<PaginatedResult<PoliciesListResult>>>(), Arg.Any<CancellationToken>())
                   .Returns(paginated);

        var result = await Policies.PoliciesList(_dispatcher, CancellationToken.None);

        AssertStatusCode(result, 200);
    }

    [Fact]
    public async Task PoliciesList_Failure_Returns500()
    {
        var error = Error.Failure(TestCode, "fail");
        _dispatcher.Dispatch(Arg.Any<IAction<PaginatedResult<PoliciesListResult>>>(), Arg.Any<CancellationToken>())
                   .Returns(PaginatedResult<PoliciesListResult>.Failure(error));

        var result = await Policies.PoliciesList(_dispatcher, CancellationToken.None);

        AssertStatusCode(result, 500);
    }

    [Fact]
    public async Task PoliciesList_DefaultPagination_IsPage1Size25()
    {
        var paginated = PaginatedResult<PoliciesListResult>.Success([], 1, 25, 0);
        _dispatcher.Dispatch(Arg.Any<IAction<PaginatedResult<PoliciesListResult>>>(), Arg.Any<CancellationToken>())
                   .Returns(paginated);

        await Policies.PoliciesList(_dispatcher, CancellationToken.None);

        await _dispatcher.Received(1).Dispatch(
            Arg.Is<IAction<PaginatedResult<PoliciesListResult>>>(r =>
                ((Vectra.Application.Features.Policies.PoliciesList.PoliciesListRequest)r).Page == 1 &&
                ((Vectra.Application.Features.Policies.PoliciesList.PoliciesListRequest)r).PageSize == 25),
            Arg.Any<CancellationToken>());
    }

    // ── PolicyDetails ─────────────────────────────────────────────────────

    [Fact]
    public async Task PolicyDetails_Found_Returns200()
    {
        var detail = new PolicyDetailsResult { Name = "default", Owner = "admin" };
        _dispatcher.Dispatch(Arg.Any<IAction<Result<PolicyDetailsResult>>>(), Arg.Any<CancellationToken>())
                   .Returns(Result<PolicyDetailsResult>.Success(detail));

        var result = await Policies.PolicyDetails("default", _dispatcher, CancellationToken.None);

        AssertStatusCode(result, 200);
    }

    [Fact]
    public async Task PolicyDetails_NotFound_Returns404()
    {
        var error = Error.NotFound(TestCode, "not found");
        _dispatcher.Dispatch(Arg.Any<IAction<Result<PolicyDetailsResult>>>(), Arg.Any<CancellationToken>())
                   .Returns(Result<PolicyDetailsResult>.Failure(error));

        var result = await Policies.PolicyDetails("missing", _dispatcher, CancellationToken.None);

        AssertStatusCode(result, 404);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static void AssertStatusCode(IResult httpResult, int expected)
        => HttpTestHelpers.ExecuteAndGetStatusCode(httpResult).Should().Be(expected);
}
