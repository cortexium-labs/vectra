using FluentAssertions;
using Microsoft.Extensions.Options;
using Vectra.BuildingBlocks.Configuration.System;
using Vectra.BuildingBlocks.Configuration.System.RateLimit;
using Vectra.Infrastructure.RateLimit;

namespace Vectra.Infrastructure.UnitTests.RateLimit;

public class AgentRateLimiterWindowResetTests
{
    [Fact]
    public async Task IsAllowedAsync_WindowExpired_ResetsCounterAndAllows()
    {
        // Use reflection to manipulate the internal Window start time
        // to simulate a new minute window
        var config = new SystemConfiguration
        {
            RateLimit = new RateLimitConfiguration
            {
                Enabled = true,
                DefaultRequestsPerMinute = 2
            }
        };
        var sut = new AgentRateLimiter(Options.Create(config));
        var agentId = Guid.NewGuid();

        // Exhaust the window
        await sut.IsAllowedAsync(agentId);
        await sut.IsAllowedAsync(agentId);
        (await sut.IsAllowedAsync(agentId)).Should().BeFalse("limit exhausted");

        // Use reflection to back-date the window start so the window has expired
        var windowsField = typeof(AgentRateLimiter)
            .GetField("_windows", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var windows = windowsField.GetValue(sut)!;
        var dictType = windows.GetType();
        var tryGetValue = dictType.GetMethod("TryGetValue")!;
        var args = new object?[] { agentId, null };
        tryGetValue.Invoke(windows, args);
        var window = args[1]!;
        var windowStartField = window.GetType()
            .GetField("WindowStartTicks", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!;
        // Set window start to 2 minutes ago
        windowStartField.SetValue(window, DateTime.UtcNow.AddMinutes(-2).Ticks);

        // Now first call in new window should be allowed
        var result = await sut.IsAllowedAsync(agentId);

        result.Should().BeTrue("window has reset");
    }
}
