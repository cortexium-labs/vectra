using FluentAssertions;
using NSubstitute;
using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Features.Hitl.Approve;
using Vectra.BuildingBlocks.Results;

namespace Vectra.Application.UnitTests.Features.Hitl;

public class ApproveHandlerTests
{
    private readonly IHitlService _hitlService = Substitute.For<IHitlService>();
    private readonly ApproveHandler _sut;

    public ApproveHandlerTests()
    {
        _sut = new ApproveHandler(_hitlService);
    }

    private static ApproveRequest MakeRequest(string id = "id1") =>
        new() { Id = id, ReviewerId = "reviewer-1", Comment = "LGTM" };

    // ── Handle ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(HitlRequestStatus.NotFound)]
    [InlineData(HitlRequestStatus.Expired)]
    public async Task Handle_ShouldReturnNotFound_WhenStatusIsNotFoundOrExpired(HitlRequestStatus status)
    {
        // Arrange
        _hitlService.GetStatusAsync("id1", CancellationToken.None).Returns(status);

        // Act
        var result = await _sut.Handle(MakeRequest(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
        await _hitlService.DidNotReceive().ApproveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WithReplayResponse()
    {
        // Arrange
        var stream = new MemoryStream("ok"u8.ToArray());
        var replay = new HitlReplayResult(true, 200, null,
            new Dictionary<string, string> { ["Content-Type"] = "application/json" }, stream);

        _hitlService.GetStatusAsync("id1", CancellationToken.None).Returns(HitlRequestStatus.Pending);
        _hitlService.ApproveAsync("id1", "reviewer-1", "LGTM", CancellationToken.None).Returns(Task.CompletedTask);
        _hitlService.ReplayAsync("id1", CancellationToken.None).Returns(replay);

        // Act
        var result = await _sut.Handle(MakeRequest(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.StatusCode.Should().Be(200);
        result.Value.ContentType.Should().Be("application/json");
        result.Value.ResponseBody.Should().BeSameAs(stream);
    }

    [Fact]
    public async Task Handle_ShouldDefaultContentType_WhenReplayHeadersMissing()
    {
        // Arrange
        var stream = new MemoryStream("data"u8.ToArray());
        var replay = new HitlReplayResult(true, 200, null, null, stream);

        _hitlService.GetStatusAsync("id1", CancellationToken.None).Returns(HitlRequestStatus.Pending);
        _hitlService.ApproveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _hitlService.ReplayAsync("id1", CancellationToken.None).Returns(replay);

        // Act
        var result = await _sut.Handle(MakeRequest(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.ContentType.Should().Be("application/octet-stream");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenReplayFails()
    {
        // Arrange
        var replay = new HitlReplayResult(false, 400, "Bad upstream", null, null);

        _hitlService.GetStatusAsync("id1", CancellationToken.None).Returns(HitlRequestStatus.Pending);
        _hitlService.ApproveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _hitlService.ReplayAsync("id1", CancellationToken.None).Returns(replay);

        // Act
        var result = await _sut.Handle(MakeRequest(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.Failure);
        result.Error.Message.Should().Be("Bad upstream");
    }

    [Fact]
    public async Task Handle_ShouldReturnSystemFailure_WhenReplayReturns503()
    {
        // Arrange
        var replay = new HitlReplayResult(false, 503, "upstream down", null, null);

        _hitlService.GetStatusAsync("id1", CancellationToken.None).Returns(HitlRequestStatus.Pending);
        _hitlService.ApproveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _hitlService.ReplayAsync("id1", CancellationToken.None).Returns(replay);

        // Act
        var result = await _sut.Handle(MakeRequest(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public async Task Handle_ShouldCallApproveWithCorrectArguments()
    {
        // Arrange
        var stream = new MemoryStream();
        var replay = new HitlReplayResult(true, 200, null, null, stream);

        _hitlService.GetStatusAsync("id1", CancellationToken.None).Returns(HitlRequestStatus.Pending);
        _hitlService.ApproveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);
        _hitlService.ReplayAsync("id1", CancellationToken.None).Returns(replay);

        // Act
        await _sut.Handle(MakeRequest(), CancellationToken.None);

        // Assert
        await _hitlService.Received(1).ApproveAsync("id1", "reviewer-1", "LGTM", CancellationToken.None);
    }

    // ── Constructor ───────────────────────────────────────────────────────

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenHitlServiceIsNull()
    {
        var act = () => new ApproveHandler(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("hitlService");
    }
}
