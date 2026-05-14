using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Abstractions.Persistence;
using Vectra.Application.Models;
using Vectra.BuildingBlocks.Clock;
using Vectra.BuildingBlocks.Configuration.HumanInTheLoop;
using Vectra.BuildingBlocks.Configuration.Policy;
using Vectra.BuildingBlocks.Configuration.Semantic;
using Vectra.Domain.AuditTrails;
using Vectra.Domain.Policies;
using Vectra.Infrastructure.Decision;

namespace Vectra.Infrastructure.UnitTests.Decision;

public class DecisionEngineTests
{
    private readonly IPolicyProvider _policyProvider = Substitute.For<IPolicyProvider>();
    private readonly IRiskScoringService _riskScoring = Substitute.For<IRiskScoringService>();
    private readonly ISemanticProvider _semanticProvider = Substitute.For<ISemanticProvider>();
    private readonly IAgentHistoryRepository _history = Substitute.For<IAgentHistoryRepository>();
    private readonly IAuditRepository _audit = Substitute.For<IAuditRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly ILogger<DecisionEngine> _logger = Substitute.For<ILogger<DecisionEngine>>();

    private DecisionEngine CreateSut(
        bool policyEnabled = true,
        bool semanticEnabled = false,
        double hitlThreshold = 0.8,
        double semanticConfidenceThreshold = 0.7,
        bool allowLowConfidence = false)
    {
        _clock.UtcNow.Returns(DateTime.UtcNow);
        _audit.AddAsync(Arg.Any<AuditTrail>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var semanticConfig = new SemanticConfiguration
        {
            Enabled = semanticEnabled,
            ConfidenceThreshold = semanticConfidenceThreshold,
            AllowLowConfidence = allowLowConfidence
        };
        var hitlConfig = new HumanInTheLoopConfiguration { Threshold = hitlThreshold };
        var policyConfig = new PolicyConfiguration { Enabled = policyEnabled };

        return new DecisionEngine(
            Options.Create(semanticConfig),
            Options.Create(hitlConfig),
            Options.Create(policyConfig),
            _policyProvider,
            _riskScoring,
            _semanticProvider,
            _history,
            _audit,
            _clock,
            _logger);
    }

    private void SetupAllow()
    {
        _policyProvider.EvaluateAsync(Arg.Any<string>(), Arg.Any<Dictionary<string, object>>(), Arg.Any<CancellationToken>())
            .Returns(PolicyDecision.Allow());
        _riskScoring.ComputeRiskScoreAsync(Arg.Any<RequestContext>(), Arg.Any<CancellationToken>())
            .Returns(0.1);
        _semanticProvider.AnalyzeAsync(Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new SemanticAnalysisResult { Confidence = 0.9 });
    }

    [Fact]
    public async Task EvaluateAsync_PolicyDeny_ReturnsDeny()
    {
        var sut = CreateSut();
        _policyProvider.EvaluateAsync(Arg.Any<string>(), Arg.Any<Dictionary<string, object>>(), Arg.Any<CancellationToken>())
            .Returns(PolicyDecision.Deny("blocked by policy"));
        var context = BuildContext();

        var result = await sut.EvaluateAsync(context);

        result.IsDenied.Should().BeTrue();
        result.Reason.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_PolicyHitl_ReturnsHitl()
    {
        var sut = CreateSut();
        _policyProvider.EvaluateAsync(Arg.Any<string>(), Arg.Any<Dictionary<string, object>>(), Arg.Any<CancellationToken>())
            .Returns(PolicyDecision.Hitl("review required"));
        var context = BuildContext();

        var result = await sut.EvaluateAsync(context);

        result.IsHitl.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_PolicyDisabled_SkipsPolicyCheck()
    {
        var sut = CreateSut(policyEnabled: false);
        _riskScoring.ComputeRiskScoreAsync(Arg.Any<RequestContext>(), Arg.Any<CancellationToken>())
            .Returns(0.1);
        var context = BuildContext();

        var result = await sut.EvaluateAsync(context);

        await _policyProvider.DidNotReceive()
            .EvaluateAsync(Arg.Any<string>(), Arg.Any<Dictionary<string, object>>(), Arg.Any<CancellationToken>());
        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_HighRiskScore_ReturnsHitl()
    {
        var sut = CreateSut(hitlThreshold: 0.7);
        _policyProvider.EvaluateAsync(Arg.Any<string>(), Arg.Any<Dictionary<string, object>>(), Arg.Any<CancellationToken>())
            .Returns(PolicyDecision.Allow());
        _riskScoring.ComputeRiskScoreAsync(Arg.Any<RequestContext>(), Arg.Any<CancellationToken>())
            .Returns(0.9); // above threshold
        var context = BuildContext();

        var result = await sut.EvaluateAsync(context);

        result.IsHitl.Should().BeTrue();
        result.Reason.Should().Contain("risk score");
    }

    [Fact]
    public async Task EvaluateAsync_LowRiskScore_Allows()
    {
        var sut = CreateSut();
        SetupAllow();
        var context = BuildContext();

        var result = await sut.EvaluateAsync(context);

        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_SemanticEnabled_LowConfidence_ReturnsHitl()
    {
        var sut = CreateSut(semanticEnabled: true, semanticConfidenceThreshold: 0.7);
        _policyProvider.EvaluateAsync(Arg.Any<string>(), Arg.Any<Dictionary<string, object>>(), Arg.Any<CancellationToken>())
            .Returns(PolicyDecision.Allow());
        _riskScoring.ComputeRiskScoreAsync(Arg.Any<RequestContext>(), Arg.Any<CancellationToken>())
            .Returns(0.1);
        _semanticProvider.AnalyzeAsync(Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new SemanticAnalysisResult { Confidence = 0.4 }); // below threshold

        var result = await sut.EvaluateAsync(BuildContext());

        result.IsHitl.Should().BeTrue();
        result.Reason.Should().Contain("semantic confidence");
    }

    [Fact]
    public async Task EvaluateAsync_SemanticDisabled_SkipsSemanticCheck()
    {
        var sut = CreateSut(semanticEnabled: false);
        SetupAllow();
        var context = BuildContext();

        var result = await sut.EvaluateAsync(context);

        await _semanticProvider.DidNotReceive()
            .AnalyzeAsync(Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateAsync_SemanticEnabled_AllowLowConfidence_DoesNotHitl()
    {
        var sut = CreateSut(semanticEnabled: true, allowLowConfidence: true, semanticConfidenceThreshold: 0.7);
        _policyProvider.EvaluateAsync(Arg.Any<string>(), Arg.Any<Dictionary<string, object>>(), Arg.Any<CancellationToken>())
            .Returns(PolicyDecision.Allow());
        _riskScoring.ComputeRiskScoreAsync(Arg.Any<RequestContext>(), Arg.Any<CancellationToken>())
            .Returns(0.1);
        _semanticProvider.AnalyzeAsync(Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new SemanticAnalysisResult { Confidence = 0.3 }); // below threshold, but allowed

        var result = await sut.EvaluateAsync(BuildContext());

        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_RecordsAudit()
    {
        var sut = CreateSut();
        SetupAllow();
        var context = BuildContext();

        await sut.EvaluateAsync(context);

        await _audit.Received(1).AddAsync(Arg.Any<AuditTrail>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Constructor_NullSemanticOptions_ThrowsArgumentNullException()
    {
        var act = () => new DecisionEngine(
            null!,
            Options.Create(new HumanInTheLoopConfiguration()),
            Options.Create(new PolicyConfiguration()),
            _policyProvider, _riskScoring, _semanticProvider,
            _history, _audit, _clock, _logger);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullPolicyProvider_ThrowsArgumentNullException()
    {
        var act = () => new DecisionEngine(
            Options.Create(new SemanticConfiguration()),
            Options.Create(new HumanInTheLoopConfiguration()),
            Options.Create(new PolicyConfiguration()),
            null!, _riskScoring, _semanticProvider,
            _history, _audit, _clock, _logger);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullRiskScoring_ThrowsArgumentNullException()
    {
        var act = () => new DecisionEngine(
            Options.Create(new SemanticConfiguration()),
            Options.Create(new HumanInTheLoopConfiguration()),
            Options.Create(new PolicyConfiguration()),
            _policyProvider, null!, _semanticProvider,
            _history, _audit, _clock, _logger);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullHistory_ThrowsArgumentNullException()
    {
        var act = () => new DecisionEngine(
            Options.Create(new SemanticConfiguration()),
            Options.Create(new HumanInTheLoopConfiguration()),
            Options.Create(new PolicyConfiguration()),
            _policyProvider, _riskScoring, _semanticProvider,
            null!, _audit, _clock, _logger);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullAudit_ThrowsArgumentNullException()
    {
        var act = () => new DecisionEngine(
            Options.Create(new SemanticConfiguration()),
            Options.Create(new HumanInTheLoopConfiguration()),
            Options.Create(new PolicyConfiguration()),
            _policyProvider, _riskScoring, _semanticProvider,
            _history, null!, _clock, _logger);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullClock_ThrowsArgumentNullException()
    {
        var act = () => new DecisionEngine(
            Options.Create(new SemanticConfiguration()),
            Options.Create(new HumanInTheLoopConfiguration()),
            Options.Create(new PolicyConfiguration()),
            _policyProvider, _riskScoring, _semanticProvider,
            _history, _audit, null!, _logger);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DecisionEngine(
            Options.Create(new SemanticConfiguration()),
            Options.Create(new HumanInTheLoopConfiguration()),
            Options.Create(new PolicyConfiguration()),
            _policyProvider, _riskScoring, _semanticProvider,
            _history, _audit, _clock, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task EvaluateAsync_HistoryRecordFails_StillReturnsDecision()
    {
        // RecordHistoryAsync has a try/catch — failure should not propagate
        var sut = CreateSut();
        SetupAllow();
        _history.RecordRequestAsync(
            Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<double>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("DB error")));

        var result = await sut.EvaluateAsync(BuildContext());

        result.IsAllowed.Should().BeTrue("history failure should be swallowed");
    }

    [Fact]
    public async Task EvaluateAsync_SemanticEnabled_HighConfidence_AllowsContinuation()
    {
        var sut = CreateSut(semanticEnabled: true, semanticConfidenceThreshold: 0.7);
        _policyProvider.EvaluateAsync(Arg.Any<string>(), Arg.Any<Dictionary<string, object>>(), Arg.Any<CancellationToken>())
            .Returns(PolicyDecision.Allow());
        _riskScoring.ComputeRiskScoreAsync(Arg.Any<RequestContext>(), Arg.Any<CancellationToken>())
            .Returns(0.1);
        _semanticProvider.AnalyzeAsync(Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new SemanticAnalysisResult { Confidence = 0.95 }); // above threshold

        var result = await sut.EvaluateAsync(BuildContext());

        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_AllowDecision_RecordsHistoryWithNoViolation()
    {
        var sut = CreateSut();
        SetupAllow();
        _history.RecordRequestAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<double>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await sut.EvaluateAsync(BuildContext());

        await _history.Received(1).RecordRequestAsync(
            Arg.Any<Guid>(),
            false, // not a violation
            Arg.Any<double>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EvaluateAsync_DenyDecision_RecordsHistoryAsViolation()
    {
        var sut = CreateSut();
        _policyProvider.EvaluateAsync(Arg.Any<string>(), Arg.Any<Dictionary<string, object>>(), Arg.Any<CancellationToken>())
            .Returns(PolicyDecision.Deny("blocked"));
        _history.RecordRequestAsync(Arg.Any<Guid>(), Arg.Any<bool>(), Arg.Any<double>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        await sut.EvaluateAsync(BuildContext());

        await _history.Received(1).RecordRequestAsync(
            Arg.Any<Guid>(),
            true, // violation = true
            Arg.Any<double>(),
            Arg.Any<CancellationToken>());
    }

    private static RequestContext BuildContext() => new()
    {
        AgentId = Guid.NewGuid(),
        Method = "GET",
        Path = "/api/data",
        TargetUrl = "https://service.local/api/data",
        PolicyName = "default-policy",
        TrustScore = 0.8
    };
}

