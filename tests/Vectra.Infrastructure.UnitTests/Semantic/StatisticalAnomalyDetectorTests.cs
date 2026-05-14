using FluentAssertions;
using NSubstitute;
using Vectra.Application.Abstractions.Caches;
using Vectra.Application.Abstractions.Persistence;
using Vectra.Application.Models;
using Vectra.Domain.Agents;
using Vectra.Infrastructure.Caches;
using Vectra.Infrastructure.Semantic;
using Microsoft.Extensions.Logging;

namespace Vectra.Infrastructure.UnitTests.Semantic;

public class StatisticalAnomalyDetectorTests
{
    private readonly IAgentHistoryRepository _repo = Substitute.For<IAgentHistoryRepository>();
    private readonly StatisticalAnomalyDetector _sut;

    public StatisticalAnomalyDetectorTests()
    {
        _sut = new StatisticalAnomalyDetector(_repo);
    }

    [Fact]
    public async Task DetectAsync_NullBaseline_ReturnsZero()
    {
        var context = new RequestContext { AgentId = Guid.NewGuid() };
        _repo.GetBaselineAsync(Arg.Any<Guid>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns((AgentBaseline?)null);

        var result = await _sut.DetectAsync(context, CancellationToken.None);

        result.Should().Be(0.0);
    }

    [Fact]
    public async Task DetectAsync_NullStats_ReturnsZero()
    {
        var context = new RequestContext { AgentId = Guid.NewGuid() };
        _repo.GetBaselineAsync(Arg.Any<Guid>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new AgentBaseline { AverageRequestsPerMinute = 10 });
        _repo.GetStatsAsync(Arg.Any<Guid>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns((AgentStats?)null);

        var result = await _sut.DetectAsync(context, CancellationToken.None);

        result.Should().Be(0.0);
    }

    [Fact]
    public async Task DetectAsync_CurrentRateOver3xBaseline_ReturnsHighAnomaly()
    {
        var context = new RequestContext { AgentId = Guid.NewGuid() };
        _repo.GetBaselineAsync(Arg.Any<Guid>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new AgentBaseline { AverageRequestsPerMinute = 10 });
        _repo.GetStatsAsync(Arg.Any<Guid>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new AgentStats { CurrentRequestsPerMinute = 35 }); // 3.5x ratio

        var result = await _sut.DetectAsync(context, CancellationToken.None);

        result.Should().BeGreaterThanOrEqualTo(0.6);
    }

    [Fact]
    public async Task DetectAsync_CurrentRateOver2xBaseline_ReturnsMediumAnomaly()
    {
        var context = new RequestContext { AgentId = Guid.NewGuid() };
        _repo.GetBaselineAsync(Arg.Any<Guid>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new AgentBaseline { AverageRequestsPerMinute = 10 });
        _repo.GetStatsAsync(Arg.Any<Guid>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new AgentStats { CurrentRequestsPerMinute = 25 }); // 2.5x ratio

        var result = await _sut.DetectAsync(context, CancellationToken.None);

        result.Should().BeGreaterThanOrEqualTo(0.3).And.BeLessThan(0.6);
    }

    [Fact]
    public async Task DetectAsync_NormalRate_ReturnsZeroAnomaly()
    {
        var context = new RequestContext { AgentId = Guid.NewGuid() };
        _repo.GetBaselineAsync(Arg.Any<Guid>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new AgentBaseline { AverageRequestsPerMinute = 10 });
        _repo.GetStatsAsync(Arg.Any<Guid>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new AgentStats { CurrentRequestsPerMinute = 10 }); // 1x ratio

        var result = await _sut.DetectAsync(context, CancellationToken.None);

        result.Should().Be(0.0);
    }

    [Fact]
    public async Task DetectAsync_ZeroBaselineRpm_ReturnsZero()
    {
        var context = new RequestContext { AgentId = Guid.NewGuid() };
        _repo.GetBaselineAsync(Arg.Any<Guid>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new AgentBaseline { AverageRequestsPerMinute = 0 });
        _repo.GetStatsAsync(Arg.Any<Guid>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new AgentStats { CurrentRequestsPerMinute = 100 });

        var result = await _sut.DetectAsync(context, CancellationToken.None);

        result.Should().Be(0.0);
    }

    [Fact]
    public async Task DetectAsync_ResultIsClampedTo1()
    {
        var context = new RequestContext { AgentId = Guid.NewGuid() };
        _repo.GetBaselineAsync(Arg.Any<Guid>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new AgentBaseline { AverageRequestsPerMinute = 1 });
        _repo.GetStatsAsync(Arg.Any<Guid>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns(new AgentStats { CurrentRequestsPerMinute = 1000 }); // enormous ratio

        var result = await _sut.DetectAsync(context, CancellationToken.None);

        result.Should().BeLessThanOrEqualTo(1.0);
    }

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        var act = () => new StatisticalAnomalyDetector(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
