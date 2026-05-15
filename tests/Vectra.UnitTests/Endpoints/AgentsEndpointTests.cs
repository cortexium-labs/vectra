using Microsoft.AspNetCore.Http;
using NSubstitute;
using Vectra.Application.Abstractions.Dispatchers;
using VectraVoid = Vectra.Application.Abstractions.Dispatchers.Void;
using Vectra.Application.Features.Agents.AgentsList;
using Vectra.Application.Features.Agents.AssignPolicy;
using Vectra.Application.Features.Agents.RegisterAgent;
using Vectra.BuildingBlocks.Errors;
using Vectra.BuildingBlocks.Results;
using Vectra.Domain.Agents;
using Vectra.Endpoints;

namespace Vectra.UnitTests.Endpoints;

public class AgentsEndpointTests
{
    private readonly IDispatcher _dispatcher;
    private static readonly ErrorCode TestCode = new(1001, ErrorCategory.Core);

    public AgentsEndpointTests()
    {
        _dispatcher = Substitute.For<IDispatcher>();
    }

    // ── AgentsList ────────────────────────────────────────────────────────

    [Fact]
    public async Task AgentsList_SuccessResult_Returns200()
    {
        var items = new List<AgentsListResult>
        {
            new() { AgentId = Guid.NewGuid(), Name = "TestAgent", Status = AgentStatus.Active }
        };
        var paginated = PaginatedResult<AgentsListResult>.Success(items, 1, 25, 1);
        _dispatcher.Dispatch(Arg.Any<IAction<PaginatedResult<AgentsListResult>>>(), Arg.Any<CancellationToken>())
                   .Returns(paginated);

        var result = await Agents.AgentsList(_dispatcher, CancellationToken.None);

        AssertStatusCode(result, 200);
    }

    [Fact]
    public async Task AgentsList_FailureResult_Returns500()
    {
        var error = Error.Failure(TestCode, "fail");
        var paginated = PaginatedResult<AgentsListResult>.Failure(error);
        _dispatcher.Dispatch(Arg.Any<IAction<PaginatedResult<AgentsListResult>>>(), Arg.Any<CancellationToken>())
                   .Returns(paginated);

        var result = await Agents.AgentsList(_dispatcher, CancellationToken.None);

        AssertStatusCode(result, 500);
    }

    [Fact]
    public async Task AgentsList_PassesPageParameters()
    {
        var paginated = PaginatedResult<AgentsListResult>.Success([], 2, 10, 0);
        _dispatcher.Dispatch(Arg.Any<IAction<PaginatedResult<AgentsListResult>>>(), Arg.Any<CancellationToken>())
                   .Returns(paginated);

        await Agents.AgentsList(_dispatcher, CancellationToken.None, page: 2, pageSize: 10);

        await _dispatcher.Received(1).Dispatch(
            Arg.Is<IAction<PaginatedResult<AgentsListResult>>>(r =>
                ((Vectra.Application.Features.Agents.AgentsList.AgentsListRequest)r).Page == 2 &&
                ((Vectra.Application.Features.Agents.AgentsList.AgentsListRequest)r).PageSize == 10),
            Arg.Any<CancellationToken>());
    }

    // ── RegisterAgent ─────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAgent_SuccessResult_Returns200()
    {
        var agentId = Guid.NewGuid();
        var successResult = Result<CreateAgentResult>.Success(new CreateAgentResult { AgentId = agentId });
        _dispatcher.Dispatch(Arg.Any<IAction<Result<CreateAgentResult>>>(), Arg.Any<CancellationToken>())
                   .Returns(successResult);

        var request = new CreateAgentRequest { Name = "agent", OwnerId = "owner", ClientSecret = "secret" };
        var result = await Agents.RegisterAgent(request, _dispatcher, CancellationToken.None);

        AssertStatusCode(result, 200);
    }

    [Fact]
    public async Task RegisterAgent_FailureResult_Returns500()
    {
        var error = Error.Failure(TestCode, "error");
        _dispatcher.Dispatch(Arg.Any<IAction<Result<CreateAgentResult>>>(), Arg.Any<CancellationToken>())
                   .Returns(Result<CreateAgentResult>.Failure(error));

        var request = new CreateAgentRequest { Name = "a", OwnerId = "o", ClientSecret = "s" };
        var result = await Agents.RegisterAgent(request, _dispatcher, CancellationToken.None);

        AssertStatusCode(result, 500);
    }

    // ── AssignPolicyToAgent ───────────────────────────────────────────────

    [Fact]
    public async Task AssignPolicyToAgent_Success_Returns200()
    {
        _dispatcher.Dispatch(Arg.Any<IAction<Result<VectraVoid>>>(), Arg.Any<CancellationToken>())
                   .Returns(Result<VectraVoid>.Success(VectraVoid.Value));

        var result = await Agents.AssignPolicyToAgent(
            Guid.NewGuid().ToString(),
            new AssignPolicyRequestModel { PolicyName = "default" },
            _dispatcher,
            CancellationToken.None);

        AssertStatusCode(result, 200);
    }

    [Fact]
    public async Task AssignPolicyToAgent_NotFound_Returns404()
    {
        var error = Error.NotFound(TestCode, "not found");
        _dispatcher.Dispatch(Arg.Any<IAction<Result<VectraVoid>>>(), Arg.Any<CancellationToken>())
                   .Returns(Result<VectraVoid>.Failure(error));

        var result = await Agents.AssignPolicyToAgent(
            Guid.NewGuid().ToString(),
            new AssignPolicyRequestModel { PolicyName = "default" },
            _dispatcher,
            CancellationToken.None);

        AssertStatusCode(result, 404);
    }

    // ── DeleteAgent ───────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAgent_Success_Returns204()
    {
        _dispatcher.Dispatch(Arg.Any<IAction<Result<VectraVoid>>>(), Arg.Any<CancellationToken>())
                   .Returns(Result<VectraVoid>.Success(VectraVoid.Value));

        var result = await Agents.DeleteAgent(Guid.NewGuid().ToString(), _dispatcher, CancellationToken.None);

        // ToHttpResult for Result<Void> success maps to 200 Ok
        AssertStatusCode(result, 200);
    }

    [Fact]
    public async Task DeleteAgent_NotFound_Returns404()
    {
        var error = Error.NotFound(TestCode, "not found");
        _dispatcher.Dispatch(Arg.Any<IAction<Result<VectraVoid>>>(), Arg.Any<CancellationToken>())
                   .Returns(Result<VectraVoid>.Failure(error));

        var result = await Agents.DeleteAgent(Guid.NewGuid().ToString(), _dispatcher, CancellationToken.None);

        AssertStatusCode(result, 404);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static void AssertStatusCode(IResult httpResult, int expected)
        => HttpTestHelpers.ExecuteAndGetStatusCode(httpResult).Should().Be(expected);
}
