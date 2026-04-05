using Microsoft.Extensions.Configuration;
using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Models;
using Vectra.Domain.Policies;

namespace Vectra.Infrastructure.Decision;

public class DecisionEngine : IDecisionEngine
{
    private readonly IPolicyEngine _policyEngine;
    private readonly IRiskScoringService _riskScoring;
    private readonly ISemanticEngine _semanticEngine;
    private readonly IConfiguration _config;

    public DecisionEngine(IPolicyEngine policyEngine, IRiskScoringService riskScoring, ISemanticEngine semanticEngine, IConfiguration config)
    {
        _policyEngine = policyEngine;
        _riskScoring = riskScoring;
        _semanticEngine = semanticEngine;
        _config = config;
    }

    public async Task<DecisionResult> EvaluateAsync(RequestContext context, CancellationToken ct)
    {
        var policyId = Guid.Parse(_config["Policy:DefaultPolicyId"]); // store in appsettings

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

        var data = new Dictionary<string, object>(); // optional external data

        var policyDecision = await _policyEngine.EvaluateAsync(policyId, input, data);
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