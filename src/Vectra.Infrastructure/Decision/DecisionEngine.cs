using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Abstractions.Persistence;
using Vectra.Application.Models;
using Vectra.BuildingBlocks.Configuration.HumanInTheLoop;
using Vectra.BuildingBlocks.Configuration.Policy;
using Vectra.BuildingBlocks.Configuration.Semantic;
using Vectra.Domain.Policies;

namespace Vectra.Infrastructure.Decision;

public class DecisionEngine : IDecisionEngine
{
    private readonly IOptions<SemanticConfiguration> _semanticOptions;
    private readonly IOptions<HumanInTheLoopConfiguration> _hitlOptions;
    private readonly IOptions<PolicyConfiguration> _policyOptions;
    private readonly IPolicyProvider _policyProvider;
    private readonly IRiskScoringService _riskScoring;
    private readonly ISemanticProvider _semanticProvider;
    private readonly IAgentHistoryRepository _historyRecorder;
    private readonly ILogger<DecisionEngine> _logger;

    public DecisionEngine(
        IOptions<SemanticConfiguration> options,
        IOptions<HumanInTheLoopConfiguration> hitlOptions,
        IOptions<PolicyConfiguration> policyOptions,
        IPolicyProvider policyEngine, 
        IRiskScoringService riskScoring, 
        ISemanticProvider semanticProvider,
        IAgentHistoryRepository historyRecorder,
        ILogger<DecisionEngine> logger)
    {
        _semanticOptions = options ?? throw new ArgumentNullException(nameof(options));
        _hitlOptions = hitlOptions ?? throw new ArgumentNullException(nameof(hitlOptions));
        _policyOptions = policyOptions ?? throw new ArgumentNullException(nameof(policyOptions));
        _policyProvider = policyEngine ?? throw new ArgumentNullException(nameof(policyEngine));
        _riskScoring = riskScoring ?? throw new ArgumentNullException(nameof(riskScoring));
        _semanticProvider = semanticProvider ?? throw new ArgumentNullException(nameof(semanticProvider));
        _historyRecorder = historyRecorder ?? throw new ArgumentNullException(nameof(historyRecorder));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<DecisionResult> EvaluateAsync(RequestContext context, CancellationToken cancellationToken)
    {
        var policyInput = new Dictionary<string, object>
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

        var policyEnabled = _policyOptions.Value.Enabled ?? true;
        if (policyEnabled)
        {
            var policyDecision = await _policyProvider.EvaluateAsync(context.PolicyName, policyInput, cancellationToken);
            if (policyDecision.IsDenied || policyDecision.IsHitl)
            {
                var decision = policyDecision.IsDenied 
                    ? DecisionResult.Deny(policyDecision.Reason ?? "Policy denied")
                    : DecisionResult.Hitl(policyDecision.Reason ?? "Policy requires HITL");
                await RecordHistoryAsync(context, decision, 0, cancellationToken);
                return decision;
            }
        }

        var riskScore = await _riskScoring.ComputeRiskScoreAsync(context, cancellationToken);
        var hitlThreshold = _hitlOptions.Value.Threshold ?? 0.8;
        if (riskScore > hitlThreshold)
        {
            var riskDecision = DecisionResult.Hitl($"High risk score: {riskScore:F2}");
            await RecordHistoryAsync(context, riskDecision, riskScore, cancellationToken);
            return riskDecision;
        }

        var semanticEnabled = _semanticOptions.Value.Enabled ?? true;
        SemanticAnalysisResult? semantic = null;
        if (semanticEnabled)
        {
            semantic = await _semanticProvider.AnalyzeAsync(context.Body, context.Path, cancellationToken);
            var confidenceThreshold = _semanticOptions.Value.ConfidenceThreshold ?? 0.7;

            if (semantic.Confidence < confidenceThreshold)
            {
                if (_semanticOptions.Value.AllowLowConfidence == true)
                {
                    _logger.LogWarning("Low confidence semantic ({Confidence}), but allowing due to configuration", semantic.Confidence);
                }
                else
                {
                    var semanticDecision = DecisionResult.Hitl($"Low semantic confidence: {semantic.Confidence:F2}");
                    await RecordHistoryAsync(context, semanticDecision, riskScore, cancellationToken);
                    return semanticDecision;
                }
            }
        }

        var allowDecision = DecisionResult.Allow();
        await RecordHistoryAsync(context, allowDecision, riskScore, cancellationToken);
        return allowDecision;
    }

    private async Task RecordHistoryAsync(RequestContext context, DecisionResult decision, double riskScore, CancellationToken ct)
    {
        var wasViolation = decision.IsDenied || decision.IsHitl;
        try
        {
            await _historyRecorder.RecordRequestAsync(context.AgentId, wasViolation, riskScore, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record agent history for {AgentId}", context.AgentId);
        }
    }
}