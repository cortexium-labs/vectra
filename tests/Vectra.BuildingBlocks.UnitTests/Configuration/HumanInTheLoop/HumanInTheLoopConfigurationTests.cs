using FluentAssertions;
using Vectra.BuildingBlocks.Configuration.HumanInTheLoop;
using Xunit;

namespace Vectra.BuildingBlocks.UnitTests.Configuration.HumanInTheLoop;

public class HumanInTheLoopConfigurationTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var config = new HumanInTheLoopConfiguration();

        config.Enabled.Should().BeTrue();
        config.Threshold.Should().Be(0.8);
        config.TimeoutSeconds.Should().Be(3600);
        config.NotificationWebhookUrl.Should().BeNull();
        config.MaxPendingRequests.Should().Be(100);
    }

    [Fact]
    public void ShouldAllowCustomValues()
    {
        var config = new HumanInTheLoopConfiguration
        {
            Enabled = false,
            Threshold = 0.9,
            TimeoutSeconds = 7200,
            NotificationWebhookUrl = "https://example.com/webhook",
            MaxPendingRequests = 200
        };

        config.Enabled.Should().BeFalse();
        config.Threshold.Should().Be(0.9);
        config.TimeoutSeconds.Should().Be(7200);
        config.NotificationWebhookUrl.Should().Be("https://example.com/webhook");
        config.MaxPendingRequests.Should().Be(200);
    }

    [Fact]
    public void MaxPendingRequests_Zero_ShouldMeanUnlimited()
    {
        var config = new HumanInTheLoopConfiguration { MaxPendingRequests = 0 };

        config.MaxPendingRequests.Should().Be(0);
    }
}
