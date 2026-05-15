using FluentAssertions;
using NSubstitute;
using Vectra.Application.Abstractions.Caches;
using Vectra.Application.Abstractions.Persistence;
using Vectra.Application.Models;
using Vectra.Domain.Agents;
using Vectra.Infrastructure.Caches;
using Vectra.Infrastructure.Risk;
using Microsoft.Extensions.Logging;

namespace Vectra.Infrastructure.UnitTests.Risk;

public class RiskScoringServiceTests
{
    private readonly RiskScoreAggregator _aggregator;
    private readonly IAgentHistoryRepository _historyRepo = Substitute.For<IAgentHistoryRepository>();
    private readonly ICacheService _cacheService = Substitute.For<ICacheService>();
    private readonly ICacheProvider _cacheProvider = Substitute.For<ICacheProvider>();
    private readonly ILogger<RiskScoringService> _logger = Substitute.For<ILogger<RiskScoringService>>();
    private readonly RiskScoringService _sut;

    public RiskScoringServiceTests()
    {
        _cacheService.Current.Returns(_cacheProvider);

        var calc = Substitute.For<IRiskCalculator>();
        calc.Weight.Returns(1.0);
        calc.CalculateAsync(Arg.Any<RequestContext>(), Arg.Any<AgentHistory?>(), Arg.Any<CancellationToken>())
            .Returns(0.4);
        _aggregator = new RiskScoreAggregator([calc]);

        _sut = new RiskScoringService(_aggregator, _historyRepo, _cacheService, _logger);
    }

    [Fact]
    public async Task ComputeRiskScoreAsync_CacheHit_ReturnsCachedScore()
    {
        var agentId = Guid.NewGuid();
        var context = new RequestContext { AgentId = agentId, Method = "GET", Path = "/api/data" };
        _cacheProvider.TryGetValueAsync<double>(Arg.Any<string>()).Returns((true, 0.99));

        var result = await _sut.ComputeRiskScoreAsync(context, TestContext.Current.CancellationToken);

        result.Should().Be(0.99);
        await _historyRepo.DidNotReceive().GetRecentAsync(Arg.Any<Guid>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ComputeRiskScoreAsync_CacheMiss_ComputesAndCachesScore()
    {
        var agentId = Guid.NewGuid();
        var context = new RequestContext { AgentId = agentId, Method = "GET", Path = "/api/data" };
        _cacheProvider.TryGetValueAsync<double>(Arg.Any<string>()).Returns((false, 0.0));
        _historyRepo.GetRecentAsync(agentId, Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns((AgentHistory?)null);

        var result = await _sut.ComputeRiskScoreAsync(context, TestContext.Current.CancellationToken);

        result.Should().BeApproximately(0.4, 1e-9);
        await _cacheProvider.Received(1).SetAsync(Arg.Any<string>(), Arg.Any<double>());
    }

    [Fact]
    public void Constructor_NullAggregator_ThrowsArgumentNullException()
    {
        var act = () => new RiskScoringService(null!, _historyRepo, _cacheService, _logger);

        act.Should().Throw<ArgumentNullException>().WithParameterName("aggregator");
    }

    [Fact]
    public void Constructor_NullHistoryRepo_ThrowsArgumentNullException()
    {
        var act = () => new RiskScoringService(_aggregator, null!, _cacheService, _logger);

        act.Should().Throw<ArgumentNullException>().WithParameterName("historyRepo");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new RiskScoringService(_aggregator, _historyRepo, _cacheService, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }
}
