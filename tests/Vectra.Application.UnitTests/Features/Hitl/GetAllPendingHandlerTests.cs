using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Features.Hitl.GetAllPending;

namespace Vectra.Application.UnitTests.Features.Hitl;

public class GetAllPendingHandlerTests
{
    private readonly ILogger<GetAllPendingHandler> _logger = Substitute.For<ILogger<GetAllPendingHandler>>();
    private readonly IHitlService _hitlService = Substitute.For<IHitlService>();
    private readonly GetAllPendingHandler _sut;

    public GetAllPendingHandlerTests()
    {
        _sut = new GetAllPendingHandler(_logger, _hitlService);
    }

    private static PendingHitlRequest MakePending(string id) =>
        new(id, "GET", "http://test", [], null, "reason", Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow.AddMinutes(5));

    // ── Handle ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ShouldReturnPaginatedSuccess_WithMappedRequests()
    {
        // Arrange
        var items = new List<PendingHitlRequest> { MakePending("id1"), MakePending("id2") };
        _hitlService.GetAllPendingPagedAsync(1, 25, CancellationToken.None).Returns((items.AsReadOnly() as IReadOnlyList<PendingHitlRequest>, 2));

        var request = new GetAllPendingRequest { Page = 1, PageSize = 25 };

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(25);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyPage_WhenNoPendingRequests()
    {
        // Arrange
        _hitlService.GetAllPendingPagedAsync(1, 25, CancellationToken.None)
            .Returns((new List<PendingHitlRequest>().AsReadOnly() as IReadOnlyList<PendingHitlRequest>, 0));

        var request = new GetAllPendingRequest { Page = 1, PageSize = 25 };

        // Act
        var result = await _sut.Handle(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldPassPageAndPageSize_ToService()
    {
        // Arrange
        _hitlService.GetAllPendingPagedAsync(2, 10, CancellationToken.None)
            .Returns((new List<PendingHitlRequest>().AsReadOnly() as IReadOnlyList<PendingHitlRequest>, 0));

        var request = new GetAllPendingRequest { Page = 2, PageSize = 10 };

        // Act
        await _sut.Handle(request, CancellationToken.None);

        // Assert
        await _hitlService.Received(1).GetAllPendingPagedAsync(2, 10, CancellationToken.None);
    }

    // ── Constructor ───────────────────────────────────────────────────────

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
    {
        var act = () => new GetAllPendingHandler(null!, _hitlService);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenHitlServiceIsNull()
    {
        var act = () => new GetAllPendingHandler(_logger, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("hitlService");
    }
}
