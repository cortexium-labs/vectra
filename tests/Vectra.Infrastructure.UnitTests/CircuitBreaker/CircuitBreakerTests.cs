using FluentAssertions;
using Microsoft.Extensions.Options;
using Vectra.BuildingBlocks.Configuration.System;
using Vectra.BuildingBlocks.Configuration.System.CircuitBreaker;
using CircuitBreakerImpl = Vectra.Infrastructure.CircuitBreaker.CircuitBreaker;

namespace Vectra.Infrastructure.UnitTests.CircuitBreaker;

public class CircuitBreakerTests
{
    private static CircuitBreakerImpl CreateSut(
        bool enabled = true,
        int failureThreshold = 3,
        int openDurationSeconds = 60,
        int samplingWindowSeconds = 120)
    {
        var config = new SystemConfiguration
        {
            CircuitBreaker = new CircuitBreakerConfiguration
            {
                Enabled = enabled,
                FailureThreshold = failureThreshold,
                OpenDurationSeconds = openDurationSeconds,
                SamplingWindowSeconds = samplingWindowSeconds
            }
        };
        return new CircuitBreakerImpl(Options.Create(config));
    }

    [Fact]
    public void IsAllowed_WhenDisabled_AlwaysReturnsTrue()
    {
        var sut = CreateSut(enabled: false);

        sut.IsAllowed("host1").Should().BeTrue();
        sut.RecordFailure("host1");
        sut.RecordFailure("host1");
        sut.RecordFailure("host1");
        sut.IsAllowed("host1").Should().BeTrue();
    }

    [Fact]
    public void IsAllowed_NewHost_ReturnsTrueWhenClosed()
    {
        var sut = CreateSut();

        sut.IsAllowed("host1").Should().BeTrue();
    }

    [Fact]
    public void RecordFailure_BelowThreshold_StaysClosed()
    {
        var sut = CreateSut(failureThreshold: 3);

        sut.RecordFailure("host1");
        sut.RecordFailure("host1");

        sut.IsAllowed("host1").Should().BeTrue();
    }

    [Fact]
    public void RecordFailure_AtThreshold_OpensCircuit()
    {
        var sut = CreateSut(failureThreshold: 3);

        sut.RecordFailure("host1");
        sut.RecordFailure("host1");
        sut.RecordFailure("host1");

        sut.IsAllowed("host1").Should().BeFalse();
    }

    [Fact]
    public void RecordSuccess_ClosesCircuit_AndResetsFailureCount()
    {
        var sut = CreateSut(failureThreshold: 2);
        sut.RecordFailure("host1");
        sut.RecordFailure("host1"); // opens circuit
        sut.IsAllowed("host1").Should().BeFalse();

        sut.RecordSuccess("host1");

        sut.IsAllowed("host1").Should().BeTrue();
    }

    [Fact]
    public void RecordSuccess_WhenDisabled_DoesNothing()
    {
        var sut = CreateSut(enabled: false);

        var act = () => sut.RecordSuccess("host1");

        act.Should().NotThrow();
    }

    [Fact]
    public void RecordFailure_WhenDisabled_DoesNothing()
    {
        var sut = CreateSut(enabled: false);

        var act = () => sut.RecordFailure("host1");

        act.Should().NotThrow();
    }

    [Fact]
    public void IsAllowed_DifferentHosts_AreTrackedIndependently()
    {
        var sut = CreateSut(failureThreshold: 2);

        sut.RecordFailure("host1");
        sut.RecordFailure("host1"); // opens host1

        sut.IsAllowed("host1").Should().BeFalse();
        sut.IsAllowed("host2").Should().BeTrue();
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new CircuitBreakerImpl(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsAllowed_OpenCircuit_AfterDuration_AllowsProbeRequest()
    {
        // Use a very short openDurationSeconds — we can't wait in unit tests,
        // so instead verify the transition logic is correctly modeled by testing
        // a circuit that has been reset via RecordSuccess (HalfOpen → Closed).
        var sut = CreateSut(failureThreshold: 2, openDurationSeconds: 0);
        sut.RecordFailure("host1");
        sut.RecordFailure("host1"); // open

        // With openDurationSeconds=0, elapsed >= 0 is always true → transitions to HalfOpen
        sut.IsAllowed("host1").Should().BeTrue();
    }
}
