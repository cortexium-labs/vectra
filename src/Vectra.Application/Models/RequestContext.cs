namespace Vectra.Application.Models;

public class RequestContext
{
    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string? Body { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public Guid AgentId { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public double TrustScore { get; set; }
}