using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Vectra.Domain.Agents;
using Vectra.Domain.AuditTrails;
using Vectra.Infrastructure.Persistence.Common.Exceptions;
using Vectra.Infrastructure.Persistence.Sqlite.Contexts;
using Vectra.Infrastructure.Persistence.Sqlite.UnitTests.Helpers;

namespace Vectra.Infrastructure.Persistence.Sqlite.UnitTests.Contexts;

public class SqliteApplicationContextTests
{
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var options = new DbContextOptionsBuilder<SqliteApplicationContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        Action act = () => new SqliteApplicationContext(options, null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task DbContext_CanAddAndQueryAgent()
    {
        await using var ctx = SqliteTestContextFactory.Create();
        var agent = new Agent("ctx-agent", "owner-99", "hash-xyz");

        ctx.Agents.Add(agent);
        await ctx.SaveChangesAsync();

        var found = await ctx.Agents.FirstOrDefaultAsync(a => a.Id == agent.Id);
        found.Should().NotBeNull();
    }

    [Fact]
    public async Task DbContext_CanAddAndQueryAuditTrail()
    {
        await using var ctx = SqliteTestContextFactory.Create();
        var trail = new AuditTrail
        {
            AgentId = Guid.NewGuid(),
            Action = "GET /health",
            TargetUrl = "https://svc/health",
            Status = "ALLOWED",
            Timestamp = DateTime.UtcNow
        };

        ctx.AuditLogs.Add(trail);
        await ctx.SaveChangesAsync();

        var found = await ctx.AuditLogs.FirstOrDefaultAsync(a => a.AgentId == trail.AgentId);
        found.Should().NotBeNull();
        found!.Action.Should().Be("GET /health");
    }

    [Fact]
    public async Task DbContext_CanAddAndQueryAgentHistory()
    {
        await using var ctx = SqliteTestContextFactory.Create();
        var agent = new Agent("hist-agent", "owner-2", "hash-2");
        ctx.Agents.Add(agent);
        await ctx.SaveChangesAsync();

        var history = new AgentHistory
        {
            Id = Guid.NewGuid(),
            AgentId = agent.Id,
            WindowStart = DateTime.UtcNow.AddMinutes(-1),
            WindowDurationSeconds = 60,
            TotalRequests = 5,
            ViolationCount = 1,
            AverageRiskScore = 0.3
        };
        ctx.AgentHistories.Add(history);
        await ctx.SaveChangesAsync();

        var found = await ctx.AgentHistories.FirstOrDefaultAsync(h => h.AgentId == agent.Id);
        found.Should().NotBeNull();
        found!.TotalRequests.Should().Be(5);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenSaveSucceeds_ReturnsAffectedRowCount()
    {
        await using var ctx = SqliteTestContextFactory.Create();
        var agent = new Agent("row-agent", "owner-3", "hash-3");
        ctx.Agents.Add(agent);

        var rows = await ctx.SaveChangesAsync();

        rows.Should().Be(1);
    }
}
