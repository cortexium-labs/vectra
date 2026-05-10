using Microsoft.EntityFrameworkCore;
using Vectra.Domain.Agents;
using Vectra.Infrastructure.Persistence.Sqlite.Contexts;
using Vectra.Infrastructure.Persistence.Sqlite.Repositories;
using Vectra.Infrastructure.Persistence.Sqlite.UnitTests.Helpers;

namespace Vectra.Infrastructure.Persistence.Sqlite.UnitTests.Repositories;

public class AgentHistoryRepositoryTests
{
    private static IDbContextFactory<SqliteApplicationContext> CreateFactory(string dbName)
        => SqliteTestContextFactory.CreateFactory(dbName);

    private static async Task<Agent> SeedAgentAsync(string dbName)
    {
        var agent = new Agent("TestAgent", "owner-1", "hash");
        await using var ctx = SqliteTestContextFactory.Create(dbName);
        ctx.Agents.Add(agent);
        await ctx.SaveChangesAsync();
        return agent;
    }

    private static async Task SeedHistoryAsync(string dbName, Guid agentId, int totalRequests, int violationCount, double riskScore, DateTime windowStart)
    {
        await using var ctx = SqliteTestContextFactory.Create(dbName);
        ctx.AgentHistories.Add(new AgentHistory
        {
            Id = Guid.NewGuid(),
            AgentId = agentId,
            WindowStart = windowStart,
            WindowDurationSeconds = 60,
            TotalRequests = totalRequests,
            ViolationCount = violationCount,
            AverageRiskScore = riskScore
        });
        await ctx.SaveChangesAsync();
    }

    [Fact]
    public void Constructor_NullFactory_ThrowsArgumentNullException()
    {
        Action act = () => new AgentHistoryRepository(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("appContextFactory");
    }

    // ── GetStatsAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStatsAsync_NoHistories_ReturnsNull()
    {
        var factory = CreateFactory(Guid.NewGuid().ToString());
        var repo = new AgentHistoryRepository(factory);

        var result = await repo.GetStatsAsync(Guid.NewGuid(), TimeSpan.FromHours(1), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetStatsAsync_WithHistories_ReturnsAggregatedStats()
    {
        var dbName = Guid.NewGuid().ToString();
        var agent = await SeedAgentAsync(dbName);
        var now = DateTime.UtcNow;

        await SeedHistoryAsync(dbName, agent.Id, 10, 2, 0.3, now.AddMinutes(-10));
        await SeedHistoryAsync(dbName, agent.Id, 20, 5, 0.5, now.AddMinutes(-5));

        var factory = CreateFactory(dbName);
        var repo = new AgentHistoryRepository(factory);

        var stats = await repo.GetStatsAsync(agent.Id, TimeSpan.FromHours(1), CancellationToken.None);

        stats.Should().NotBeNull();
        stats!.AgentId.Should().Be(agent.Id);
        stats.TotalRequests.Should().Be(30);
        stats.ViolationCount.Should().Be(7);
        stats.CurrentRequestsPerMinute.Should().BeApproximately(30.0 / 60, 0.01);
    }

    [Fact]
    public async Task GetStatsAsync_HistoriesOutsideWindow_ReturnsNull()
    {
        var dbName = Guid.NewGuid().ToString();
        var agent = await SeedAgentAsync(dbName);

        await SeedHistoryAsync(dbName, agent.Id, 10, 2, 0.3, DateTime.UtcNow.AddHours(-5));

        var factory = CreateFactory(dbName);
        var repo = new AgentHistoryRepository(factory);

        var result = await repo.GetStatsAsync(agent.Id, TimeSpan.FromMinutes(30), CancellationToken.None);

        result.Should().BeNull();
    }

    // ── GetBaselineAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetBaselineAsync_NoHistories_ReturnsNull()
    {
        var factory = CreateFactory(Guid.NewGuid().ToString());
        var repo = new AgentHistoryRepository(factory);

        var result = await repo.GetBaselineAsync(Guid.NewGuid(), TimeSpan.FromDays(7), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetBaselineAsync_WithHistories_ReturnsBaseline()
    {
        var dbName = Guid.NewGuid().ToString();
        var agent = await SeedAgentAsync(dbName);
        var now = DateTime.UtcNow;

        await SeedHistoryAsync(dbName, agent.Id, 100, 10, 0.2, now.AddHours(-1));
        await SeedHistoryAsync(dbName, agent.Id, 200, 20, 0.4, now.AddHours(-2));

        var factory = CreateFactory(dbName);
        var repo = new AgentHistoryRepository(factory);

        var baseline = await repo.GetBaselineAsync(agent.Id, TimeSpan.FromDays(1), CancellationToken.None);

        baseline.Should().NotBeNull();
        baseline!.AverageRequestsPerMinute.Should().BeGreaterThan(0);
        baseline.AverageViolationRate.Should().BeApproximately(30.0 / 300, 0.001);
    }

    // ── GetRecentAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRecentAsync_NoHistories_ReturnsNull()
    {
        var factory = CreateFactory(Guid.NewGuid().ToString());
        var repo = new AgentHistoryRepository(factory);

        var result = await repo.GetRecentAsync(Guid.NewGuid(), TimeSpan.FromMinutes(5), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetRecentAsync_WithHistories_ReturnsAggregatedHistory()
    {
        var dbName = Guid.NewGuid().ToString();
        var agent = await SeedAgentAsync(dbName);
        var now = DateTime.UtcNow;

        await SeedHistoryAsync(dbName, agent.Id, 5, 1, 0.1, now.AddMinutes(-2));
        await SeedHistoryAsync(dbName, agent.Id, 10, 3, 0.2, now.AddMinutes(-1));

        var factory = CreateFactory(dbName);
        var repo = new AgentHistoryRepository(factory);

        var recent = await repo.GetRecentAsync(agent.Id, TimeSpan.FromMinutes(10), CancellationToken.None);

        recent.Should().NotBeNull();
        recent!.AgentId.Should().Be(agent.Id);
        recent.TotalRequests.Should().Be(15);
        recent.ViolationCount.Should().Be(4);
    }

    // ── RecordRequestAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task RecordRequestAsync_CreatesNewHistoryWindow()
    {
        var dbName = Guid.NewGuid().ToString();
        var agent = await SeedAgentAsync(dbName);

        var factory = CreateFactory(dbName);
        var repo = new AgentHistoryRepository(factory);

        await repo.RecordRequestAsync(agent.Id, wasViolation: false, riskScore: 0.2, CancellationToken.None);

        await using var ctx = SqliteTestContextFactory.Create(dbName);
        var history = await ctx.AgentHistories.FirstOrDefaultAsync(h => h.AgentId == agent.Id);

        history.Should().NotBeNull();
        history!.TotalRequests.Should().Be(1);
        history.ViolationCount.Should().Be(0);
        history.AverageRiskScore.Should().BeApproximately(0.2, 0.001);
    }

    [Fact]
    public async Task RecordRequestAsync_ViolationRequest_IncrementsViolationCount()
    {
        var dbName = Guid.NewGuid().ToString();
        var agent = await SeedAgentAsync(dbName);

        var factory = CreateFactory(dbName);
        var repo = new AgentHistoryRepository(factory);

        await repo.RecordRequestAsync(agent.Id, wasViolation: true, riskScore: 0.9, CancellationToken.None);

        await using var ctx = SqliteTestContextFactory.Create(dbName);
        var history = await ctx.AgentHistories.FirstOrDefaultAsync(h => h.AgentId == agent.Id);

        history!.ViolationCount.Should().Be(1);
    }

    [Fact]
    public async Task RecordRequestAsync_SameWindow_UpdatesExistingRecord()
    {
        var dbName = Guid.NewGuid().ToString();
        var agent = await SeedAgentAsync(dbName);

        var factory = CreateFactory(dbName);
        var repo = new AgentHistoryRepository(factory);

        // Two requests in the same minute window
        await repo.RecordRequestAsync(agent.Id, wasViolation: false, riskScore: 0.1, CancellationToken.None);
        await repo.RecordRequestAsync(agent.Id, wasViolation: true, riskScore: 0.9, CancellationToken.None);

        await using var ctx = SqliteTestContextFactory.Create(dbName);
        var histories = await ctx.AgentHistories.Where(h => h.AgentId == agent.Id).ToListAsync();

        histories.Should().HaveCount(1);
        histories[0].TotalRequests.Should().Be(2);
        histories[0].ViolationCount.Should().Be(1);
    }

    [Fact]
    public async Task RecordRequestAsync_MultipleRequests_UpdatesAverageRiskScore()
    {
        var dbName = Guid.NewGuid().ToString();
        var agent = await SeedAgentAsync(dbName);

        var factory = CreateFactory(dbName);
        var repo = new AgentHistoryRepository(factory);

        await repo.RecordRequestAsync(agent.Id, wasViolation: false, riskScore: 0.0, CancellationToken.None);
        await repo.RecordRequestAsync(agent.Id, wasViolation: false, riskScore: 1.0, CancellationToken.None);

        await using var ctx = SqliteTestContextFactory.Create(dbName);
        var history = await ctx.AgentHistories.FirstAsync(h => h.AgentId == agent.Id);

        history.AverageRiskScore.Should().BeApproximately(0.5, 0.001);
    }
}
