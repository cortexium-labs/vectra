using FluentAssertions;
using NSubstitute;
using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Features.Hitl.Deny;
using Vectra.BuildingBlocks.Results;

namespace Vectra.Application.UnitTests.Features.Hitl;

public class DenyHandlerTests
{
    private readonly IHitlService _hitlService = Substitute.For<IHitlService>();
    private readonly DenyHandler _sut;

    public DenyHandlerTests()
    {
        _sut = new DenyHandler(_hitlService);
    }

    private static DenyRequest MakeRequest(string id = "id1") =>
        new() { Id = id, ReviewerId = "reviewer-1", Comment = "Not approved" };

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
        await _hitlService.DidNotReceive().DenyAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenRequestIsPending()
    {
        // Arrange
        _hitlService.GetStatusAsync("id1", CancellationToken.None).Returns(HitlRequestStatus.Pending);
        _hitlService.DenyAsync("id1", "reviewer-1", "Not approved", CancellationToken.None).Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Handle(MakeRequest(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldCallDenyWithCorrectArguments()
    {
        // Arrange
        _hitlService.GetStatusAsync("id1", CancellationToken.None).Returns(HitlRequestStatus.Pending);
        _hitlService.DenyAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        // Act
        await _sut.Handle(MakeRequest(), CancellationToken.None);

        // Assert
        await _hitlService.Received(1).DenyAsync("id1", "reviewer-1", "Not approved", CancellationToken.None);
    }

    [Fact]
    public async Task Handle_ShouldAllowNullComment()
    {
        // Arrange
        _hitlService.GetStatusAsync("id1", CancellationToken.None).Returns(HitlRequestStatus.Approved);
        _hitlService.DenyAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var request = new DenyRequest { Id = "id1", ReviewerId = "reviewer-1", Comment = null };

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        await _hitlService.Received(1).DenyAsync(
            Arg.Is<string>(x => x == "id1"),
            Arg.Is<string>(x => x == "reviewer-1"),
            Arg.Is<string?>(x => x == null),
            Arg.Any<CancellationToken>());
    }

    // ── Constructor ───────────────────────────────────────────────────────

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenHitlServiceIsNull()
    {
        var act = () => new DenyHandler(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("hitlService");
    }
}
