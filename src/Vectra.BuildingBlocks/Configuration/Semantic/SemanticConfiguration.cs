namespace Vectra.BuildingBlocks.Configuration.Semantic;

public class SemanticConfiguration
{
    public bool? Enabled { get; set; } = false;
    public double? ConfidenceThreshold { get; set; } = 0.7;
    public bool? AllowLowConfidence { get; set; } = false;
    public string DefaultProvider { get; set; } = "Internal";
    public SemanticProviders Providers { get; set; } = new();
}