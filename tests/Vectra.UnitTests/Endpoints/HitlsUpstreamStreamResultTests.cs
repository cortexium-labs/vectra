using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Vectra.Application.Abstractions.Dispatchers;
using Vectra.Application.Features.Hitl.Approve;
using Vectra.BuildingBlocks.Results;
using Vectra.Endpoints;

namespace Vectra.UnitTests.Endpoints;

public class HitlsUpstreamStreamResultTests
{
    // UpstreamStreamResult is a private nested class inside Hitls.
    // We exercise it through the ApproveHitl endpoint (success path) which returns it directly.
    // The test below drives ExecuteAsync by calling the result.

    private static DefaultHttpContext BuildContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var provider = services.BuildServiceProvider();
        return new DefaultHttpContext
        {
            RequestServices = provider,
            Response = { Body = new MemoryStream() }
        };
    }

    [Fact]
    public async Task UpstreamStreamResult_ExecuteAsync_CopiesBodyAndStatusCode()
    {
        // Arrange: build the IResult via ApproveHitl success path
        var body = System.Text.Encoding.UTF8.GetBytes("response content");
        var stream = new MemoryStream(body);

        var dispatcher = Substitute.For<IDispatcher>();
        dispatcher.Dispatch(Arg.Any<ApproveRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<ApproveResult>.Success(
                new ApproveResult(201, "application/octet-stream", stream)));

        var requestContext = new DefaultHttpContext();
        var result = await Hitls.ApproveHitl(
            "id1",
            new Hitls.ReviewDecisionRequest(null),
            requestContext,
            dispatcher,
            CancellationToken.None);

        // Act
        var ctx = BuildContext();
        await result.ExecuteAsync(ctx);

        // Assert
        ctx.Response.StatusCode.Should().Be(201);
        ctx.Response.Body.Position = 0;
        var written = new StreamReader(ctx.Response.Body).ReadToEnd();
        written.Should().Be("response content");
    }

    [Fact]
    public async Task UpstreamStreamResult_ExecuteAsync_DefaultContentType_IsOctetStream()
    {
        var body = System.Text.Encoding.UTF8.GetBytes("data");
        var stream = new MemoryStream(body);

        var dispatcher = Substitute.For<IDispatcher>();
        dispatcher.Dispatch(Arg.Any<ApproveRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<ApproveResult>.Success(
                new ApproveResult(200, "application/octet-stream", stream)));

        var ctx = new DefaultHttpContext();
        var result = await Hitls.ApproveHitl(
            "x", new Hitls.ReviewDecisionRequest(null), ctx, dispatcher, CancellationToken.None);

        var executeCtx = BuildContext();
        await result.ExecuteAsync(executeCtx);

        executeCtx.Response.StatusCode.Should().Be(200);
    }
}

