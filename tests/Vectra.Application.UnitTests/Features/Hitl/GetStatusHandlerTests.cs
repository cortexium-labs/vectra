using FluentAssertions;
using NSubstitute;
using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Features.Hitl.GetStatus;
using Vectra.BuildingBlocks.Results;

namespace Vectra.Application.UnitTests.Features.Hitl;

public class GetStatusHandlerTests
{
    private readonly IHitlService _hitlService = Substitute.For<IHitlService>();
    private readonly GetStatusHandler _sut;

    public GetStatusHandlerTests()
    {
        _sut = new GetStatusHandler(_hitlService);
    }

    private static PendingHitlRequest MakePending(string id) =>
        new(id, "GET", "http://test", [], null, "reason", Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddMinutes(5));

    // ── Handle ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenStatusIsNotFound()
    {
        // Arrange
        _hitlService.GetStatusAsync("missing", CancellationToken.None).Returns(HitlRequestStatus.NotFound);
        var request = new GetStatusRequest { Id = "missing" };

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error!.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnSuccess_WhenStatusIsApproved()
    {
        // Arrange
        _hitlService.GetStatusAsync("id1", CancellationToken.None).Returns(HitlRequestStatus.Approved);
        var request = new GetStatusRequest { Id = "id1" };

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be("id1");
        result.Value.Status.Should().Be(HitlRequestStatus.Approved.ToString());
        result.Value.Request.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldIncludePendingRequest_WhenStatusIsPending()
    {
        // Arrange
        var pending = MakePending("id1");
        _hitlService.GetStatusAsync("id1", CancellationToken.None).Returns(HitlRequestStatus.Pending);
        _hitlService.GetPendingAsync("id1", CancellationToken.None).Returns(pending);

        var request = new GetStatusRequest { Id = "id1" };

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(HitlRequestStatus.Pending.ToString());
        result.Value.Request.Should().Be(pending);
        await _hitlService.Received(1).GetPendingAsync("id1", CancellationToken.None);
    }

    [Theory]
    [InlineData(HitlRequestStatus.Approved)]
    [InlineData(HitlRequestStatus.Denied)]
    [InlineData(HitlRequestStatus.Expired)]
    public async Task Handle_ShouldNotCallGetPending_WhenStatusIsNotPending(HitlRequestStatus status)
    {
        // Arrange
        _hitlService.GetStatusAsync("id1", CancellationToken.None).Returns(status);
        var request = new GetStatusRequest { Id = "id1" };

        // Act
        await _sut.Handle(request, CancellationToken.None);

        // Assert
        await _hitlService.DidNotReceive().GetPendingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── Constructor ───────────────────────────────────────────────────────

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenHitlServiceIsNull()
    {
        var act = () => new GetStatusHandler(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("hitlService");
    }
}
