using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Models;
using Vectra.Domain.Policies;

namespace Vectra.Infrastructure.Decision;

public class DecisionEngine : IDecisionEngine
{
    private readonly IPolicyEngine _policyEngine;
    private readonly IRiskScoringService _riskScoring;
    private readonly ISemanticEngine _semanticEngine;

    public DecisionEngine(
        IPolicyEngine policyEngine, 
        IRiskScoringService riskScoring, 
        ISemanticEngine semanticEngine)
    {
        _policyEngine = policyEngine ?? throw new ArgumentNullException(nameof(policyEngine));
        _riskScoring = riskScoring ?? throw new ArgumentNullException(nameof(riskScoring));
        _semanticEngine = semanticEngine ?? throw new ArgumentNullException(nameof(semanticEngine));
    }

    public async Task<DecisionResult> EvaluateAsync(RequestContext context, CancellationToken ct)
    {
        var input = new Dictionary<string, object>
        {
            ["method"] = context.Method,
            ["path"] = context.Path,
            ["headers"] = context.Headers,
            ["agent"] = new Dictionary<string, object>
            {
                ["id"] = context.AgentId,
                ["trust_score"] = context.TrustScore
            }
        };

        var policyDecision = await _policyEngine.EvaluateAsync(context.PolicyName, input);
        if (policyDecision.IsDenied)
            return DecisionResult.Deny(policyDecision.Reason ?? "Policy denied");
        if (policyDecision.IsHitl)
            return DecisionResult.Hitl(policyDecision.Reason ?? "Policy requires HITL");

        // Continue with risk scoring & semantic
        var riskScore = _riskScoring.ComputeRiskScore(context);
        if (riskScore > 0.8)
            return DecisionResult.Hitl($"High risk score: {riskScore}");

        var semantic = await _semanticEngine.AnalyzeAsync(context.Body, context.Path, ct);
        if (semantic.Confidence < 0.7)
            return DecisionResult.Hitl($"Low semantic confidence: {semantic.Confidence}");

        return DecisionResult.Allow();
    }
}