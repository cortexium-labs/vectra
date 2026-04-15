namespace Vectra.BuildingBlocks.Configuration.Policy;

public class PolicyConfiguration
{
    public bool? Enabled { get; set; } = true;
    public string DefaultProvider { get; set; } = "Internal";
    public PolicyProviders Providers { get; set; } = new();
}