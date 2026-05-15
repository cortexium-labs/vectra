using Microsoft.AspNetCore.Http;
using NSubstitute;
using Vectra.Application.Abstractions.Dispatchers;
using Vectra.Application.Features.Authentications.GenerateToken;
using Vectra.BuildingBlocks.Errors;
using Vectra.BuildingBlocks.Results;
using Vectra.Endpoints;

namespace Vectra.UnitTests.Endpoints;

public class TokensEndpointTests
{
    private readonly IDispatcher _dispatcher;
    private static readonly ErrorCode TestCode = new(1003, ErrorCategory.Security);

    public TokensEndpointTests()
    {
        _dispatcher = Substitute.For<IDispatcher>();
    }

    [Fact]
    public async Task GetToken_Success_Returns200WithToken()
    {
        var token = new GenerateTokenResult { AccessToken = "jwt-token-value" };
        _dispatcher.Dispatch(Arg.Any<IAction<Result<GenerateTokenResult>>>(), Arg.Any<CancellationToken>())
                   .Returns(Result<GenerateTokenResult>.Success(token));

        var request = new GenerateTokenRequest { AgentId = Guid.NewGuid(), ClientSecret = "secret" };
        var result = await Tokens.GetToken(request, _dispatcher, CancellationToken.None);

        AssertStatusCode(result, 200);
    }

    [Fact]
    public async Task GetToken_Unauthorized_Returns401()
    {
        var error = Error.Unauthorized(TestCode, "invalid credentials");
        _dispatcher.Dispatch(Arg.Any<IAction<Result<GenerateTokenResult>>>(), Arg.Any<CancellationToken>())
                   .Returns(Result<GenerateTokenResult>.Failure(error));

        var request = new GenerateTokenRequest { AgentId = Guid.NewGuid(), ClientSecret = "wrong" };
        var result = await Tokens.GetToken(request, _dispatcher, CancellationToken.None);

        AssertStatusCode(result, 401);
    }

    [Fact]
    public async Task GetToken_NotFound_Returns404()
    {
        var error = Error.NotFound(TestCode, "agent not found");
        _dispatcher.Dispatch(Arg.Any<IAction<Result<GenerateTokenResult>>>(), Arg.Any<CancellationToken>())
                   .Returns(Result<GenerateTokenResult>.Failure(error));

        var request = new GenerateTokenRequest { AgentId = Guid.NewGuid(), ClientSecret = "s" };
        var result = await Tokens.GetToken(request, _dispatcher, CancellationToken.None);

        AssertStatusCode(result, 404);
    }

    [Fact]
    public async Task GetToken_ForwardsRequestToDispatcher()
    {
        var agentId = Guid.NewGuid();
        _dispatcher.Dispatch(Arg.Any<IAction<Result<GenerateTokenResult>>>(), Arg.Any<CancellationToken>())
                   .Returns(Result<GenerateTokenResult>.Success(new GenerateTokenResult { AccessToken = "t" }));

        var request = new GenerateTokenRequest { AgentId = agentId, ClientSecret = "s" };
        await Tokens.GetToken(request, _dispatcher, CancellationToken.None);

        await _dispatcher.Received(1).Dispatch(
            Arg.Is<IAction<Result<GenerateTokenResult>>>(r =>
                ((GenerateTokenRequest)r).AgentId == agentId),
            Arg.Any<CancellationToken>());
    }

    private static void AssertStatusCode(IResult httpResult, int expected)
        => HttpTestHelpers.ExecuteAndGetStatusCode(httpResult).Should().Be(expected);
}
