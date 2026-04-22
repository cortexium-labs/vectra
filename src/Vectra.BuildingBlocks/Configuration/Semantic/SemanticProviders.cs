namespace Vectra.BuildingBlocks.Configuration.Semantic;

public class SemanticProviders
{
    public InternalOnnxConfiguration Internal { get; set; } = new();
    public AzureAiConfiguration AzureAi { get; set; } = new();
}