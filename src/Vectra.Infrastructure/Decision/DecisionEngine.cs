using Vectra.Core.DTOs;
using Vectra.Core.Entities;
using Vectra.Core.Interfaces;

namespace Vectra.Infrastructure.Decision;

public class DecisionEngine : IDecisionEngine
{
    private readonly IPolicyEngine _policyEngine;
    private readonly IRiskScoringService _riskScoring;
    private readonly ISemanticEngine _semanticEngine;

    public DecisionEngine(IPolicyEngine policyEngine, IRiskScoringService riskScoring, ISemanticEngine semanticEngine)
    {
        _policyEngine = policyEngine;
        _riskScoring = riskScoring;
        _semanticEngine = semanticEngine;
    }

    public async Task<DecisionResult> EvaluateAsync(RequestContext context, CancellationToken cancellationToken = default)
    {
        // 1. OPA evaluation (deterministic)
        var opaInput = new
        {
            method = context.Method,
            path = context.Path,
            agent = new { trust_score = context.TrustScore }
        };
        var opaDecision = await _policyEngine.EvaluateAsync("vectra/authz", opaInput, cancellationToken);
        if (opaDecision.Decision == "deny")
            return DecisionResult.Deny("OPA policy denied");
        if (opaDecision.Decision == "hitl")
            return DecisionResult.Hitl("OPA policy requires HITL");

        // 2. Risk scoring
        var riskScore = _riskScoring.ComputeRiskScore(context);
        if (riskScore > 0.8)
            return DecisionResult.Hitl($"High risk score: {riskScore}");

        // 3. Semantic analysis (optional, can be async, here we block)
        // In production, you might do this in background and allow low-risk requests to proceed.
        var semantic = await _semanticEngine.AnalyzeAsync(context.Body, context.Path, cancellationToken);
        if (semantic.Confidence < 0.7)
            return DecisionResult.Hitl($"Low semantic confidence: {semantic.Confidence}");

        // All checks passed
        return DecisionResult.Allow();
    }
}