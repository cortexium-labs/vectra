using Vectra.HealthCheck;

namespace Vectra.UnitTests.HealthCheck;

public class HealthCheckResponseTests
{
    [Fact]
    public void Properties_CanBeSetAndRead()
    {
        var response = new HealthCheckResponse
        {
            Status = "Healthy",
            HealthCheckDuration = TimeSpan.FromMilliseconds(42)
        };

        response.Status.Should().Be("Healthy");
        response.HealthCheckDuration.Should().Be(TimeSpan.FromMilliseconds(42));
    }

    [Fact]
    public void Status_DefaultIsNull()
    {
        var response = new HealthCheckResponse();
        response.Status.Should().BeNull();
    }

    [Fact]
    public void HealthCheckDuration_CanBeZero()
    {
        var response = new HealthCheckResponse { HealthCheckDuration = TimeSpan.Zero };
        response.HealthCheckDuration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Status_CanBeUnhealthy()
    {
        var response = new HealthCheckResponse { Status = "Unhealthy" };
        response.Status.Should().Be("Unhealthy");
    }
}
