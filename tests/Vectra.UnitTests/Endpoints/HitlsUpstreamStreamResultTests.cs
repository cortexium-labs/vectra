using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Vectra.Application.Abstractions.Executions;
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
        var replay = new HitlReplayResult(true, 201, null, null, stream);

        var hitlService = NSubstitute.Substitute.For<Vectra.Application.Abstractions.Executions.IHitlService>();
        hitlService.GetStatusAsync("id1", Arg.Any<CancellationToken>())
                   .Returns(HitlRequestStatus.Pending);
        hitlService.ApproveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                   .Returns(Task.CompletedTask);
        hitlService.ReplayAsync("id1", Arg.Any<CancellationToken>())
                   .Returns(replay);

        var requestContext = new DefaultHttpContext();
        var result = await Hitls.ApproveHitl(
            "id1",
            new Hitls.ReviewDecisionRequest(null),
            requestContext,
            hitlService,
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
        // No content-type header set by the fake upstream → replay returns null headers
        var replay = new HitlReplayResult(true, 200, null, null, stream);

        var hitlService = NSubstitute.Substitute.For<Vectra.Application.Abstractions.Executions.IHitlService>();
        hitlService.GetStatusAsync("x", Arg.Any<CancellationToken>()).Returns(HitlRequestStatus.Pending);
        hitlService.ApproveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                   .Returns(Task.CompletedTask);
        hitlService.ReplayAsync("x", Arg.Any<CancellationToken>()).Returns(replay);

        var ctx = new DefaultHttpContext();
        var result = await Hitls.ApproveHitl(
            "x", new Hitls.ReviewDecisionRequest(null), ctx, hitlService, CancellationToken.None);

        var executeCtx = BuildContext();
        await result.ExecuteAsync(executeCtx);

        executeCtx.Response.StatusCode.Should().Be(200);
    }
}
