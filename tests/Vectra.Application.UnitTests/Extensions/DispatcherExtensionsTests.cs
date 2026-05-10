using FluentAssertions;
using NSubstitute;
using Vectra.Application.Abstractions.Dispatchers;
using Vectra.Application.Extensions;
using Vectra.Application.Features.Agents.AgentsList;
using Vectra.Application.Features.Agents.AssignPolicy;
using Vectra.Application.Features.Agents.DeleteAgent;
using Vectra.Application.Features.Agents.RegisterAgent;
using Vectra.Application.Features.Authentications.GenerateToken;
using Vectra.Application.Features.Policies.PoliciesList;
using Vectra.Application.Features.Policies.PolicyDetails;
using Vectra.BuildingBlocks.Results;
using VoidType = Vectra.Application.Abstractions.Dispatchers.Void;

namespace Vectra.Application.UnitTests.Extensions;

public class DispatcherExtensionsTests
{
    private readonly IDispatcher _dispatcher = Substitute.For<IDispatcher>();

    [Fact]
    public async Task AgentsList_ShouldDispatchWithCorrectPaging()
    {
        var expected = PaginatedResult<AgentsListResult>.Success([], 2, 10, 0);
        _dispatcher.Dispatch(Arg.Any<AgentsListRequest>(), CancellationToken.None)
            .Returns(Task.FromResult(expected));

        var result = await _dispatcher.AgentsList(2, 10, CancellationToken.None);

        result.Should().BeSameAs(expected);
        await _dispatcher.Received(1).Dispatch(
            Arg.Is<AgentsListRequest>(r => r.Page == 2 && r.PageSize == 10),
            CancellationToken.None);
    }

    [Fact]
    public async Task RegisterAgent_ShouldDispatchRequest()
    {
        var request = new CreateAgentRequest { Name = "A", OwnerId = "o", ClientSecret = "s" };
        var expected = Result<CreateAgentResult>.Success(new CreateAgentResult { AgentId = Guid.NewGuid() });
        _dispatcher.Dispatch(request, CancellationToken.None).Returns(Task.FromResult(expected));

        var result = await _dispatcher.RegisterAgent(request, CancellationToken.None);

        result.Should().BeSameAs(expected);
        await _dispatcher.Received(1).Dispatch(request, CancellationToken.None);
    }

    [Fact]
    public async Task AssignPolicyToAgent_ShouldDispatchWithCorrectAgentIdAndPolicy()
    {
        var agentId = Guid.NewGuid().ToString();
        var voidResult = Result<VoidType>.Success(new VoidType());
        _dispatcher.Dispatch(Arg.Any<AssignPolicyRequest>(), CancellationToken.None)
            .Returns(Task.FromResult(voidResult));

        var result = await _dispatcher.AssignPolicyToAgent(agentId, "DefaultPolicy", CancellationToken.None);

        result.Should().BeSameAs(voidResult);
        await _dispatcher.Received(1).Dispatch(
            Arg.Is<AssignPolicyRequest>(r => r.AgentId == agentId && r.PolicyName == "DefaultPolicy"),
            CancellationToken.None);
    }

    [Fact]
    public async Task DeleteAgent_ShouldDispatchWithCorrectAgentId()
    {
        var agentId = Guid.NewGuid();
        var voidResult = Result<VoidType>.Success(new VoidType());
        _dispatcher.Dispatch(Arg.Any<DeleteAgentRequest>(), CancellationToken.None)
            .Returns(Task.FromResult(voidResult));

        var result = await _dispatcher.DeleteAgent(agentId, CancellationToken.None);

        result.Should().BeSameAs(voidResult);
        await _dispatcher.Received(1).Dispatch(
            Arg.Is<DeleteAgentRequest>(r => r.AgentId == agentId.ToString()),
            CancellationToken.None);
    }

    [Fact]
    public async Task GenerateToken_ShouldDispatchRequest()
    {
        var request = new GenerateTokenRequest { AgentId = Guid.NewGuid(), ClientSecret = "secret" };
        var expected = Result<GenerateTokenResult>.Success(new GenerateTokenResult { AccessToken = "jwt" });
        _dispatcher.Dispatch(request, CancellationToken.None).Returns(Task.FromResult(expected));

        var result = await _dispatcher.GenerateToken(request, CancellationToken.None);

        result.Should().BeSameAs(expected);
        await _dispatcher.Received(1).Dispatch(request, CancellationToken.None);
    }

    [Fact]
    public async Task PoliciesList_ShouldDispatchWithCorrectPaging()
    {
        var expected = PaginatedResult<PoliciesListResult>.Success([], 1, 25, 0);
        _dispatcher.Dispatch(Arg.Any<PoliciesListRequest>(), CancellationToken.None)
            .Returns(Task.FromResult(expected));

        var result = await _dispatcher.PoliciesList(1, 25, CancellationToken.None);

        result.Should().BeSameAs(expected);
        await _dispatcher.Received(1).Dispatch(
            Arg.Is<PoliciesListRequest>(r => r.Page == 1 && r.PageSize == 25),
            CancellationToken.None);
    }

    [Fact]
    public async Task PolicyDetails_ShouldDispatchWithCorrectName()
    {
        var expected = Result<PolicyDetailsResult>.Success(
            new PolicyDetailsResult { Name = "SecurityPolicy" });
        _dispatcher.Dispatch(Arg.Any<PolicyDetailsRequest>(), CancellationToken.None)
            .Returns(Task.FromResult(expected));

        var result = await _dispatcher.PolicyDetails("SecurityPolicy", CancellationToken.None);

        result.Should().BeSameAs(expected);
        await _dispatcher.Received(1).Dispatch(
            Arg.Is<PolicyDetailsRequest>(r => r.Name == "SecurityPolicy"),
            CancellationToken.None);
    }
}
