namespace Vectra.BuildingBlocks.Configuration.HumanInTheLoop;

public class HumanInTheLoopConfiguration
{
    public bool? Enabled { get; set; } = true;
    public double? Threshold { get; set; } = 0.8;

    /// <summary>How long (seconds) a suspended request remains reviewable before it auto-expires.</summary>
    public int TimeoutSeconds { get; set; } = 3600;

    /// <summary>Optional webhook URL to POST when a new request is suspended for review.</summary>
    public string? NotificationWebhookUrl { get; set; }

    /// <summary>Maximum number of concurrently pending HITL requests. 0 = unlimited.</summary>
    public int MaxPendingRequests { get; set; } = 100;
}
