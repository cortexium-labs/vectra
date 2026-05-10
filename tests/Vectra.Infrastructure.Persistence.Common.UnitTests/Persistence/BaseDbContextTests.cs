using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Vectra.Domain.Agents;
using Vectra.Domain.AuditTrails;
using Vectra.Infrastructure.Persistence.Common.Exceptions;

namespace Vectra.Infrastructure.Persistence.Common.UnitTests.Persistence;

// Concrete subclass of BaseDbContext used for testing.
internal sealed class TestDbContext : BaseDbContext
{
    public TestDbContext(DbContextOptions options)
        : base(options, NullLogger<BaseDbContext>.Instance)
    {
    }
}

public class BaseDbContextTests : IDisposable
{
    private readonly TestDbContext _context;

    public BaseDbContextTests()
    {
        var options = new DbContextOptionsBuilder()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new TestDbContext(options);
    }

    public void Dispose() => _context.Dispose();

    // ── DbSets ───────────────────────────────────────────────────────────────

    [Fact]
    public void AuditLogs_DbSet_ShouldNotBeNull()
    {
        _context.AuditLogs.Should().NotBeNull();
    }

    [Fact]
    public void Agents_DbSet_ShouldNotBeNull()
    {
        _context.Agents.Should().NotBeNull();
    }

    [Fact]
    public void AgentHistories_DbSet_ShouldNotBeNull()
    {
        _context.AgentHistories.Should().NotBeNull();
    }

    // ── SaveChangesAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task SaveChangesAsync_WithValidEntity_ShouldPersistAndReturnCount()
    {
        var agent = new Agent("TestAgent", "owner-1", "hash-1");
        _context.Agents.Add(agent);

        var result = await _context.SaveChangesAsync();

        result.Should().Be(1);
    }

    [Fact]
    public async Task SaveChangesAsync_WithMultipleEntities_ShouldReturnCorrectCount()
    {
        _context.Agents.Add(new Agent("Agent-A", "owner-1", "hash-a"));
        _context.Agents.Add(new Agent("Agent-B", "owner-2", "hash-b"));

        var result = await _context.SaveChangesAsync();

        result.Should().Be(2);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenCancelled_ShouldWrapInDatabaseSaveException()
    {
        var agent = new Agent("TestAgent", "owner-1", "hash");
        _context.Agents.Add(agent);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = async () => await _context.SaveChangesAsync(cts.Token);

        await act.Should().ThrowAsync<DatabaseSaveException>();
    }

    [Fact]
    public async Task SaveChangesAsync_WithAuditTrail_ShouldPersistRecord()
    {
        var audit = new AuditTrail
        {
            AgentId = Guid.NewGuid(),
            Action = "POST /test",
            TargetUrl = "https://example.com",
            Status = "ALLOWED",
            Timestamp = DateTime.UtcNow
        };
        _context.AuditLogs.Add(audit);

        await _context.SaveChangesAsync();

        var saved = await _context.AuditLogs.FirstOrDefaultAsync();
        saved.Should().NotBeNull();
        saved!.Action.Should().Be("POST /test");
    }

    [Fact]
    public async Task SaveChangesAsync_WithAgentHistory_ShouldPersistRecord()
    {
        var agent = new Agent("HistAgent", "owner-hist", "hash-hist");
        _context.Agents.Add(agent);
        await _context.SaveChangesAsync();

        var history = new AgentHistory
        {
            AgentId = agent.Id,
            WindowStart = DateTime.UtcNow,
            TotalRequests = 10,
            ViolationCount = 1,
            AverageRiskScore = 0.3
        };
        _context.AgentHistories.Add(history);
        await _context.SaveChangesAsync();

        var saved = await _context.AgentHistories.FirstOrDefaultAsync();
        saved.Should().NotBeNull();
        saved!.TotalRequests.Should().Be(10);
    }

    // ── Implements IDatabaseContext ──────────────────────────────────────────

    [Fact]
    public void Context_ShouldImplement_IDatabaseContext()
    {
        _context.Should().BeAssignableTo<IDatabaseContext>();
    }
}
