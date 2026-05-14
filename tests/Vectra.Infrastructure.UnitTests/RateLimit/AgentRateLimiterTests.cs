using FluentAssertions;
using Microsoft.Extensions.Options;
using Vectra.BuildingBlocks.Configuration.System;
using Vectra.BuildingBlocks.Configuration.System.RateLimit;
using Vectra.Infrastructure.RateLimit;

namespace Vectra.Infrastructure.UnitTests.RateLimit;

public class AgentRateLimiterTests
{
    private static AgentRateLimiter CreateSut(bool enabled = true, int requestsPerMinute = 5)
    {
        var config = new SystemConfiguration
        {
            RateLimit = new RateLimitConfiguration
            {
                Enabled = enabled,
                DefaultRequestsPerMinute = requestsPerMinute
            }
        };
        return new AgentRateLimiter(Options.Create(config));
    }

    [Fact]
    public async Task IsAllowedAsync_WhenDisabled_AlwaysReturnsTrue()
    {
        var sut = CreateSut(enabled: false, requestsPerMinute: 1);
        var agentId = Guid.NewGuid();

        for (int i = 0; i < 10; i++)
        {
            var result = await sut.IsAllowedAsync(agentId);
            result.Should().BeTrue();
        }
    }

    [Fact]
    public async Task IsAllowedAsync_WithinLimit_ReturnsTrue()
    {
        var sut = CreateSut(requestsPerMinute: 5);
        var agentId = Guid.NewGuid();

        for (int i = 0; i < 5; i++)
        {
            var result = await sut.IsAllowedAsync(agentId);
            result.Should().BeTrue($"request {i + 1} should be allowed");
        }
    }

    [Fact]
    public async Task IsAllowedAsync_ExceedsLimit_ReturnsFalse()
    {
        var sut = CreateSut(requestsPerMinute: 3);
        var agentId = Guid.NewGuid();

        for (int i = 0; i < 3; i++)
            await sut.IsAllowedAsync(agentId);

        var result = await sut.IsAllowedAsync(agentId);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAllowedAsync_DifferentAgents_HaveSeparateWindows()
    {
        var sut = CreateSut(requestsPerMinute: 2);
        var agent1 = Guid.NewGuid();
        var agent2 = Guid.NewGuid();

        await sut.IsAllowedAsync(agent1);
        await sut.IsAllowedAsync(agent1);
        // agent1 is exhausted
        (await sut.IsAllowedAsync(agent1)).Should().BeFalse();

        // agent2 is independent
        (await sut.IsAllowedAsync(agent2)).Should().BeTrue();
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new AgentRateLimiter(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
