namespace Vectra.Application.Abstractions.Executions;

public interface ISemanticProvider
{
    Task<SemanticAnalysisResult> AnalyzeAsync(string? body, string metadata, CancellationToken cancellationToken = default);
}

public class SemanticAnalysisResult
{
    public string Intent { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string[] RiskTags { get; set; } = Array.Empty<string>();
    public bool FallbackSafe { get; set; }
    public string? Explanation { get; set; }
}