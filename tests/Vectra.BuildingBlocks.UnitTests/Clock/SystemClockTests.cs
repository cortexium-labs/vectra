using FluentAssertions;
using Vectra.BuildingBlocks.Clock;
using Xunit;

namespace Vectra.BuildingBlocks.UnitTests.Clock;

public class SystemClockTests
{
    [Fact]
    public void UtcNow_ShouldReturnCurrentUtcTime()
    {
        var clock = new SystemClock();
        var before = DateTime.UtcNow;

        var utcNow = clock.UtcNow;

        var after = DateTime.UtcNow;
        utcNow.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void UtcNow_ShouldReturnUtcKind()
    {
        var clock = new SystemClock();

        clock.UtcNow.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void SystemClock_ShouldImplementIClock()
    {
        var clock = new SystemClock();

        clock.Should().BeAssignableTo<IClock>();
    }
}
