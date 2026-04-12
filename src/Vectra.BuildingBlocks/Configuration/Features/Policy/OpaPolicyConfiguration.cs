namespace Vectra.BuildingBlocks.Configuration.Features.Policy;

public class OpaPolicyConfiguration
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Path { get; set; } = "/v1/data/vectra/authz";
    public TimeSpan? Timeout { get; set; } = TimeSpan.FromSeconds(5);
}