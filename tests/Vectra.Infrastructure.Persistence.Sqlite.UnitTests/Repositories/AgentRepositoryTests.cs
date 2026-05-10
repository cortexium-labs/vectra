using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Vectra.Domain.Agents;
using Vectra.Infrastructure.Persistence.Sqlite.Contexts;
using Vectra.Infrastructure.Persistence.Sqlite.Repositories;
using Vectra.Infrastructure.Persistence.Sqlite.UnitTests.Helpers;

namespace Vectra.Infrastructure.Persistence.Sqlite.UnitTests.Repositories;

public class AgentRepositoryTests
{
    private static IDbContextFactory<SqliteApplicationContext> CreateFactory(string dbName)
        => SqliteTestContextFactory.CreateFactory(dbName);

    private static Agent MakeAgent(string name = "TestAgent") =>
        new(name, "owner-1", "hash-abc");

    [Fact]
    public void Constructor_NullFactory_ThrowsArgumentNullException()
    {
        Action act = () => new AgentRepository(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("appContextFactory");
    }

    [Fact]
    public async Task AddAsync_Then_GetAllAsync_ReturnsSavedAgent()
    {
        var dbName = Guid.NewGuid().ToString();
        var factory = CreateFactory(dbName);
        var repo = new AgentRepository(factory);
        var agent = MakeAgent();

        await repo.AddAsync(agent);
        var all = await repo.GetAllAsync();

        all.Should().ContainSingle(a => a.Id == agent.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingAgent_ReturnsAgent()
    {
        var dbName = Guid.NewGuid().ToString();
        var factory = CreateFactory(dbName);
        var repo = new AgentRepository(factory);
        var agent = MakeAgent();

        await repo.AddAsync(agent);
        var result = await repo.GetByIdAsync(agent.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(agent.Id);
        result.Name.Should().Be("TestAgent");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingAgent_ReturnsNull()
    {
        var factory = CreateFactory(Guid.NewGuid().ToString());
        var repo = new AgentRepository(factory);

        var result = await repo.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsCorrectPageAndTotalCount()
    {
        var dbName = Guid.NewGuid().ToString();
        var factory = CreateFactory(dbName);
        var repo = new AgentRepository(factory);

        for (int i = 0; i < 5; i++)
            await repo.AddAsync(MakeAgent($"Agent{i:D2}"));

        var (items, totalCount) = await repo.GetPagedAsync(1, 3);

        totalCount.Should().Be(5);
        items.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetPagedAsync_SecondPage_ReturnsRemainingItems()
    {
        var dbName = Guid.NewGuid().ToString();
        var factory = CreateFactory(dbName);
        var repo = new AgentRepository(factory);

        for (int i = 0; i < 5; i++)
            await repo.AddAsync(MakeAgent($"Agent{i:D2}"));

        var (items, totalCount) = await repo.GetPagedAsync(2, 3);

        totalCount.Should().Be(5);
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPagedAsync_EmptyDatabase_ReturnsEmptyWithZeroCount()
    {
        var factory = CreateFactory(Guid.NewGuid().ToString());
        var repo = new AgentRepository(factory);

        var (items, totalCount) = await repo.GetPagedAsync(1, 10);

        totalCount.Should().Be(0);
        items.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_ModifiesAgent()
    {
        var dbName = Guid.NewGuid().ToString();
        var factory = CreateFactory(dbName);
        var repo = new AgentRepository(factory);
        var agent = MakeAgent();

        await repo.AddAsync(agent);
        agent.UpdateTrustScore(0.9);
        await repo.UpdateAsync(agent);

        var updated = await repo.GetByIdAsync(agent.Id);
        updated!.TrustScore.Should().BeApproximately(0.9, 0.001);
    }

    [Fact]
    public async Task DeleteAsync_ExistingAgent_RemovesIt()
    {
        var dbName = Guid.NewGuid().ToString();
        var factory = CreateFactory(dbName);
        var repo = new AgentRepository(factory);
        var agent = MakeAgent();

        await repo.AddAsync(agent);
        await repo.DeleteAsync(agent.Id);

        var result = await repo.GetByIdAsync(agent.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistingAgent_ThrowsInvalidOperationException()
    {
        var factory = CreateFactory(Guid.NewGuid().ToString());
        var repo = new AgentRepository(factory);

        Func<Task> act = () => repo.DeleteAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetAllAsync_MultipleAgents_ReturnsAll()
    {
        var dbName = Guid.NewGuid().ToString();
        var factory = CreateFactory(dbName);
        var repo = new AgentRepository(factory);

        await repo.AddAsync(MakeAgent("Alpha"));
        await repo.AddAsync(MakeAgent("Beta"));
        await repo.AddAsync(MakeAgent("Gamma"));

        var all = await repo.GetAllAsync();

        all.Should().HaveCount(3);
    }
}
