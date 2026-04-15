using Vectra.Application.Abstractions.Executions;

namespace Vectra.Infrastructure.Semantic;

public class SemanticEngineStub : ISemanticProvider
{
    public Task<SemanticAnalysisResult> AnalyzeAsync(string? body, string metadata, CancellationToken cancellationToken = default)
    {
        string intent = "normal";
        double confidence = 0.88;
        string[] riskTags = Array.Empty<string>();

        if (body != null && body.Contains("export", StringComparison.OrdinalIgnoreCase))
        {
            intent = "bulk_export";
            confidence = 0.92;
            riskTags = new[] { "data_exfiltration" };
        }

        return Task.FromResult(new SemanticAnalysisResult{ 
            Intent = intent, 
            Confidence = confidence, 
            RiskTags = riskTags
        });
    }
}