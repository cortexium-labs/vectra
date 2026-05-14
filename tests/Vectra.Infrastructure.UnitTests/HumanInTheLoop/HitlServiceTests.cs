using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Net;
using Vectra.Application.Abstractions.Caches;
using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Abstractions.Persistence;
using Vectra.Application.Models;
using Vectra.BuildingBlocks.Clock;
using Vectra.BuildingBlocks.Configuration.HumanInTheLoop;
using Vectra.Domain.AuditTrails;
using Vectra.Infrastructure.Caches;
using Vectra.Infrastructure.HumanInTheLoop;

namespace Vectra.Infrastructure.UnitTests.HumanInTheLoop;

public class HitlServiceTests
{
    private readonly ICacheService _cacheService = Substitute.For<ICacheService>();
    private readonly ICacheProvider _cacheProvider = Substitute.For<ICacheProvider>();
    private readonly IAuditRepository _audit = Substitute.For<IAuditRepository>();
    private readonly IClock _clock = Substitute.For<IClock>();
    private readonly IHttpClientFactory _httpClientFactory = Substitute.For<IHttpClientFactory>();
    private readonly ILogger<HitlService> _logger = Substitute.For<ILogger<HitlService>>();

    private readonly DateTime _now = new(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    public HitlServiceTests()
    {
        _cacheService.Current.Returns(_cacheProvider);
        _clock.UtcNow.Returns(_now);
        _audit.AddAsync(Arg.Any<AuditTrail>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        // Default: empty index
        _cacheProvider.TryGetValueAsync<HashSet<string>>("hitl:index")
            .Returns((false, null));
    }

    private HitlService CreateSut(
        int timeoutSeconds = 300,
        int maxPending = 0,
        string? webhookUrl = null) =>
        new(_cacheService, _audit, _clock, Options.Create(new HumanInTheLoopConfiguration
        {
            TimeoutSeconds = timeoutSeconds,
            MaxPendingRequests = maxPending,
            NotificationWebhookUrl = webhookUrl
        }), _httpClientFactory, _logger);

    private RequestContext BuildContext(string method = "GET", string path = "/api/data") =>
        new()
        {
            AgentId = Guid.NewGuid(),
            Method = method,
            Path = path,
            TargetUrl = $"https://upstream.local{path}",
            Headers = new Dictionary<string, string> { ["Accept"] = "application/json" },
            Body = null
        };

    // ── SuspendRequestAsync ───────────────────────────────────────────────

    [Fact]
    public async Task SuspendRequestAsync_StoresRequestInCache_ReturnsId()
    {
        var sut = CreateSut();
        _cacheProvider.SetAsync(Arg.Any<string>(), Arg.Any<PendingHitlRequest>()).Returns(Task.FromResult<PendingHitlRequest>(null!));
        _cacheProvider.SetAsync(Arg.Any<string>(), Arg.Any<HashSet<string>>()).Returns(Task.FromResult<HashSet<string>>(null!));

        var id = await sut.SuspendRequestAsync(BuildContext(), "needs review");

        id.Should().NotBeNullOrWhiteSpace();
        await _cacheProvider.Received().SetAsync(
            Arg.Is<string>(k => k.StartsWith("hitl:")),
            Arg.Any<PendingHitlRequest>());
    }

    [Fact]
    public async Task SuspendRequestAsync_RedactsSensitiveHeaders()
    {
        var sut = CreateSut();
        PendingHitlRequest? stored = null;
        _cacheProvider.SetAsync(Arg.Any<string>(), Arg.Do<PendingHitlRequest>(r => stored = r))
            .Returns(x => Task.FromResult<PendingHitlRequest>(x.Arg<PendingHitlRequest>()));
        _cacheProvider.SetAsync(Arg.Any<string>(), Arg.Any<HashSet<string>>()).Returns(Task.FromResult<HashSet<string>>(null!));

        var ctx = BuildContext();
        ctx.Headers["Authorization"] = "Bearer token123";
        ctx.Headers["X-Api-Key"] = "secret-key";
        ctx.Headers["Accept"] = "application/json";

        await sut.SuspendRequestAsync(ctx, "test");

        stored.Should().NotBeNull();
        stored!.Headers["Authorization"].Should().Be("[REDACTED]");
        stored.Headers["X-Api-Key"].Should().Be("[REDACTED]");
        stored.Headers["Accept"].Should().Be("application/json");
    }

    [Fact]
    public async Task SuspendRequestAsync_SetsExpiresAtBasedOnTimeout()
    {
        var sut = CreateSut(timeoutSeconds: 600);
        PendingHitlRequest? stored = null;
        _cacheProvider.SetAsync(Arg.Any<string>(), Arg.Do<PendingHitlRequest>(r => stored = r))
            .Returns(x => Task.FromResult<PendingHitlRequest>(x.Arg<PendingHitlRequest>()));
        _cacheProvider.SetAsync(Arg.Any<string>(), Arg.Any<HashSet<string>>()).Returns(Task.FromResult<HashSet<string>>(null!));

        await sut.SuspendRequestAsync(BuildContext(), "test");

        stored!.ExpiresAt.Should().Be(_now.AddSeconds(600));
    }

    [Fact]
    public async Task SuspendRequestAsync_MaxPendingExceeded_ThrowsInvalidOperationException()
    {
        var sut = CreateSut(maxPending: 1);

        // Simulate one existing pending request
        var existingId = Guid.NewGuid().ToString();
        var existingIndex = new HashSet<string> { existingId };
        _cacheProvider.TryGetValueAsync<HashSet<string>>("hitl:index")
            .Returns((true, existingIndex));
        var existingRequest = new PendingHitlRequest(
            existingId, "GET", "https://x.com", new Dictionary<string, string>(),
            null, "reason", Guid.NewGuid(), _now, _now.AddSeconds(300));
        _cacheProvider.TryGetValueAsync<PendingHitlRequest>($"hitl:{existingId}")
            .Returns((true, existingRequest));

        var act = async () => await sut.SuspendRequestAsync(BuildContext(), "new reason");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*maximum*");
    }

    [Fact]
    public async Task SuspendRequestAsync_WithWebhook_SendsNotification()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{}");
        var httpClient = new HttpClient(handler);
        _httpClientFactory.CreateClient().Returns(httpClient);

        var sut = CreateSut(webhookUrl: "https://webhook.example.com/notify");
        _cacheProvider.SetAsync(Arg.Any<string>(), Arg.Any<PendingHitlRequest>()).Returns(Task.FromResult<PendingHitlRequest>(null!));
        _cacheProvider.SetAsync(Arg.Any<string>(), Arg.Any<HashSet<string>>()).Returns(Task.FromResult<HashSet<string>>(null!));

        var act = async () => await sut.SuspendRequestAsync(BuildContext(), "test");

        await act.Should().NotThrowAsync();
        handler.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task SuspendRequestAsync_WebhookFails_DoesNotThrow()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.InternalServerError, "error");
        var httpClient = new HttpClient(handler);
        _httpClientFactory.CreateClient().Returns(httpClient);

        var sut = CreateSut(webhookUrl: "https://webhook.example.com/notify");
        _cacheProvider.SetAsync(Arg.Any<string>(), Arg.Any<PendingHitlRequest>()).Returns(Task.FromResult<PendingHitlRequest>(null!));
        _cacheProvider.SetAsync(Arg.Any<string>(), Arg.Any<HashSet<string>>()).Returns(Task.FromResult<HashSet<string>>(null!));

        var act = async () => await sut.SuspendRequestAsync(BuildContext(), "test");

        await act.Should().NotThrowAsync();
    }

    // ── GetPendingAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task GetPendingAsync_ExistingNotExpired_ReturnsRequest()
    {
        var sut = CreateSut();
        var id = "test-id";
        var request = new PendingHitlRequest(id, "GET", "https://x.com",
            new Dictionary<string, string>(), null, "reason",
            Guid.NewGuid(), _now, _now.AddSeconds(300));
        _cacheProvider.TryGetValueAsync<PendingHitlRequest>($"hitl:{id}")
            .Returns((true, request));

        var result = await sut.GetPendingAsync(id);

        result.Should().Be(request);
    }

    [Fact]
    public async Task GetPendingAsync_NotFound_ReturnsNull()
    {
        var sut = CreateSut();
        _cacheProvider.TryGetValueAsync<PendingHitlRequest>(Arg.Any<string>())
            .Returns((false, null));

        var result = await sut.GetPendingAsync("missing-id");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetPendingAsync_Expired_CleansUpAndReturnsNull()
    {
        var sut = CreateSut();
        var id = "expired-id";
        var expiredRequest = new PendingHitlRequest(id, "GET", "https://x.com",
            new Dictionary<string, string>(), null, "reason",
            Guid.NewGuid(), _now.AddSeconds(-600), _now.AddSeconds(-1));
        _cacheProvider.TryGetValueAsync<PendingHitlRequest>($"hitl:{id}")
            .Returns((true, expiredRequest));
        _cacheProvider.RemoveAsync(Arg.Any<string>()).Returns(Task.CompletedTask);
        _cacheProvider.SetAsync(Arg.Any<string>(), Arg.Any<HashSet<string>>()).Returns(Task.FromResult<HashSet<string>>(null!));

        var result = await sut.GetPendingAsync(id);

        result.Should().BeNull();
        await _cacheProvider.Received().RemoveAsync($"hitl:{id}");
    }

    // ── GetStatusAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetStatusAsync_DecisionExists_ReturnsDecisionStatus()
    {
        var sut = CreateSut();
        var id = "decided-id";
        var decision = new HitlDecision(id, HitlRequestStatus.Approved, "reviewer-1", null, _now);
        _cacheProvider.TryGetValueAsync<HitlDecision>($"hitl:decision:{id}")
            .Returns((true, decision));

        var result = await sut.GetStatusAsync(id);

        result.Should().Be(HitlRequestStatus.Approved);
    }

    [Fact]
    public async Task GetStatusAsync_NoDecision_NoPending_ReturnsNotFound()
    {
        var sut = CreateSut();
        var id = "unknown-id";
        _cacheProvider.TryGetValueAsync<HitlDecision>($"hitl:decision:{id}")
            .Returns((false, null));
        _cacheProvider.TryGetValueAsync<PendingHitlRequest>($"hitl:{id}")
            .Returns((false, null));

        var result = await sut.GetStatusAsync(id);

        result.Should().Be(HitlRequestStatus.NotFound);
    }

    [Fact]
    public async Task GetStatusAsync_PendingExpired_ReturnsExpired()
    {
        var sut = CreateSut();
        var id = "old-id";
        _cacheProvider.TryGetValueAsync<HitlDecision>($"hitl:decision:{id}")
            .Returns((false, null));
        var expiredRequest = new PendingHitlRequest(id, "GET", "https://x.com",
            new Dictionary<string, string>(), null, "reason",
            Guid.NewGuid(), _now.AddSeconds(-600), _now.AddSeconds(-1));
        _cacheProvider.TryGetValueAsync<PendingHitlRequest>($"hitl:{id}")
            .Returns((true, expiredRequest));
        _cacheProvider.RemoveAsync(Arg.Any<string>()).Returns(Task.CompletedTask);
        _cacheProvider.SetAsync(Arg.Any<string>(), Arg.Any<HashSet<string>>()).Returns(Task.FromResult<HashSet<string>>(null!));

        var result = await sut.GetStatusAsync(id);

        result.Should().Be(HitlRequestStatus.Expired);
    }

    [Fact]
    public async Task GetStatusAsync_StillPending_ReturnsPending()
    {
        var sut = CreateSut();
        var id = "pending-id";
        _cacheProvider.TryGetValueAsync<HitlDecision>($"hitl:decision:{id}")
            .Returns((false, null));
        var request = new PendingHitlRequest(id, "GET", "https://x.com",
            new Dictionary<string, string>(), null, "reason",
            Guid.NewGuid(), _now, _now.AddSeconds(300));
        _cacheProvider.TryGetValueAsync<PendingHitlRequest>($"hitl:{id}")
            .Returns((true, request));

        var result = await sut.GetStatusAsync(id);

        result.Should().Be(HitlRequestStatus.Pending);
    }

    // ── GetAllPendingAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetAllPendingAsync_NoIndex_ReturnsEmpty()
    {
        var sut = CreateSut();
        _cacheProvider.TryGetValueAsync<HashSet<string>>("hitl:index")
            .Returns((false, null));

        var result = await sut.GetAllPendingAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllPendingAsync_MultipleItems_ReturnsNonExpired()
    {
        var sut = CreateSut();
        var id1 = "id-1";
        var id2 = "id-2-expired";
        _cacheProvider.TryGetValueAsync<HashSet<string>>("hitl:index")
            .Returns((true, new HashSet<string> { id1, id2 }));
        _cacheProvider.TryGetValueAsync<PendingHitlRequest>($"hitl:{id1}")
            .Returns((true, new PendingHitlRequest(id1, "GET", "https://x.com",
                new Dictionary<string, string>(), null, "r", Guid.NewGuid(), _now, _now.AddSeconds(300))));
        _cacheProvider.TryGetValueAsync<PendingHitlRequest>($"hitl:{id2}")
            .Returns((true, new PendingHitlRequest(id2, "GET", "https://x.com",
                new Dictionary<string, string>(), null, "r", Guid.NewGuid(), _now.AddSeconds(-600), _now.AddSeconds(-1))));
        _cacheProvider.RemoveAsync(Arg.Any<string>()).Returns(Task.CompletedTask);
        _cacheProvider.SetAsync(Arg.Any<string>(), Arg.Any<HashSet<string>>()).Returns(Task.FromResult<HashSet<string>>(null!));

        var result = await sut.GetAllPendingAsync();

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(id1);
    }

    // ── ApproveAsync / DenyAsync ──────────────────────────────────────────

    [Fact]
    public async Task ApproveAsync_StoresApprovedDecision()
    {
        var sut = CreateSut();
        var id = "to-approve";
        _cacheProvider.SetAsync(Arg.Any<string>(), Arg.Any<HitlDecision>()).Returns(Task.FromResult<HitlDecision>(null!));
        _cacheProvider.TryGetValueAsync<PendingHitlRequest>($"hitl:{id}")
            .Returns((false, null));

        await sut.ApproveAsync(id, "reviewer-1", "looks fine");

        await _cacheProvider.Received().SetAsync(
            $"hitl:decision:{id}",
            Arg.Is<HitlDecision>(d => d.Status == HitlRequestStatus.Approved && d.ReviewerId == "reviewer-1"));
    }

    [Fact]
    public async Task DenyAsync_StoresDeniedDecision()
    {
        var sut = CreateSut();
        var id = "to-deny";
        _cacheProvider.SetAsync(Arg.Any<string>(), Arg.Any<HitlDecision>()).Returns(Task.FromResult<HitlDecision>(null!));
        _cacheProvider.TryGetValueAsync<PendingHitlRequest>($"hitl:{id}")
            .Returns((false, null));

        await sut.DenyAsync(id, "reviewer-2", "too risky");

        await _cacheProvider.Received().SetAsync(
            $"hitl:decision:{id}",
            Arg.Is<HitlDecision>(d => d.Status == HitlRequestStatus.Denied && d.ReviewerId == "reviewer-2"));
    }

    // ── RemoveAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveAsync_RemovesFromCache()
    {
        var sut = CreateSut();
        var id = "to-remove";
        _cacheProvider.RemoveAsync(Arg.Any<string>()).Returns(Task.CompletedTask);
        _cacheProvider.SetAsync(Arg.Any<string>(), Arg.Any<HashSet<string>>()).Returns(Task.FromResult<HashSet<string>>(null!));

        await sut.RemoveAsync(id);

        await _cacheProvider.Received().RemoveAsync($"hitl:{id}");
    }

    // ── ReplayAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task ReplayAsync_NotFound_ReturnsFailure()
    {
        var sut = CreateSut();
        var id = "nf";
        _cacheProvider.TryGetValueAsync<HitlDecision>($"hitl:decision:{id}")
            .Returns((false, null));
        _cacheProvider.TryGetValueAsync<PendingHitlRequest>($"hitl:{id}")
            .Returns((false, null));

        var result = await sut.ReplayAsync(id);

        result.Success.Should().BeFalse();
        result.ErrorReason.Should().Contain("not found");
    }

    [Fact]
    public async Task ReplayAsync_Denied_ReturnsFailure()
    {
        var sut = CreateSut();
        var id = "denied";
        _cacheProvider.TryGetValueAsync<HitlDecision>($"hitl:decision:{id}")
            .Returns((true, new HitlDecision(id, HitlRequestStatus.Denied, "rev", null, _now)));

        var result = await sut.ReplayAsync(id);

        result.Success.Should().BeFalse();
        result.ErrorReason.Should().Contain("denied");
    }

    [Fact]
    public async Task ReplayAsync_Pending_ReturnsFailure()
    {
        var sut = CreateSut();
        var id = "still-pending";
        _cacheProvider.TryGetValueAsync<HitlDecision>($"hitl:decision:{id}")
            .Returns((false, null));
        var request = new PendingHitlRequest(id, "GET", "https://x.com",
            new Dictionary<string, string>(), null, "r", Guid.NewGuid(), _now, _now.AddSeconds(300));
        _cacheProvider.TryGetValueAsync<PendingHitlRequest>($"hitl:{id}")
            .Returns((true, request));

        var result = await sut.ReplayAsync(id);

        result.Success.Should().BeFalse();
        result.ErrorReason.Should().Contain("awaiting");
    }

    [Fact]
    public async Task ReplayAsync_Expired_ReturnsFailure()
    {
        var sut = CreateSut();
        var id = "exp";
        _cacheProvider.TryGetValueAsync<HitlDecision>($"hitl:decision:{id}")
            .Returns((false, null));
        var expired = new PendingHitlRequest(id, "GET", "https://x.com",
            new Dictionary<string, string>(), null, "r", Guid.NewGuid(), _now.AddSeconds(-600), _now.AddSeconds(-1));
        _cacheProvider.TryGetValueAsync<PendingHitlRequest>($"hitl:{id}")
            .Returns((true, expired));
        _cacheProvider.RemoveAsync(Arg.Any<string>()).Returns(Task.CompletedTask);
        _cacheProvider.SetAsync(Arg.Any<string>(), Arg.Any<HashSet<string>>()).Returns(Task.FromResult<HashSet<string>>(null!));

        var result = await sut.ReplayAsync(id);

        result.Success.Should().BeFalse();
        result.ErrorReason.Should().Contain("expired");
    }

    [Fact]
    public async Task ReplayAsync_Approved_SuccessfulUpstreamCall_ReturnsSuccess()
    {
        var sut = CreateSut();
        var id = "approved-replay";

        _cacheProvider.TryGetValueAsync<HitlDecision>($"hitl:decision:{id}")
            .Returns((true, new HitlDecision(id, HitlRequestStatus.Approved, "rev", null, _now)));

        var pending = new PendingHitlRequest(id, "GET", "https://upstream.local/api/data",
            new Dictionary<string, string> { ["Accept"] = "application/json" },
            null, "reason", Guid.NewGuid(), _now, _now.AddSeconds(300));
        _cacheProvider.TryGetValueAsync<PendingHitlRequest>($"hitl:{id}")
            .Returns((true, pending));

        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "upstream response");
        _httpClientFactory.CreateClient().Returns(new HttpClient(handler));
        _cacheProvider.RemoveAsync(Arg.Any<string>()).Returns(Task.CompletedTask);
        _cacheProvider.SetAsync(Arg.Any<string>(), Arg.Any<HashSet<string>>()).Returns(Task.FromResult<HashSet<string>>(null!));

        var result = await sut.ReplayAsync(id);

        result.Success.Should().BeTrue();
        result.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task ReplayAsync_Approved_UpstreamFails_ReturnsFailure()
    {
        var sut = CreateSut();
        var id = "upstream-fail";

        _cacheProvider.TryGetValueAsync<HitlDecision>($"hitl:decision:{id}")
            .Returns((true, new HitlDecision(id, HitlRequestStatus.Approved, "rev", null, _now)));

        var pending = new PendingHitlRequest(id, "POST", "https://upstream.local/api/data",
            new Dictionary<string, string>(), "body", "reason", Guid.NewGuid(), _now, _now.AddSeconds(300));
        _cacheProvider.TryGetValueAsync<PendingHitlRequest>($"hitl:{id}")
            .Returns((true, pending));

        var handler = new FailingHttpMessageHandler();
        _httpClientFactory.CreateClient().Returns(new HttpClient(handler));

        var result = await sut.ReplayAsync(id);

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(503);
    }

    [Fact]
    public async Task ReplayAsync_Approved_PendingDataMissing_ReturnsFailure()
    {
        var sut = CreateSut();
        var id = "data-missing";

        _cacheProvider.TryGetValueAsync<HitlDecision>($"hitl:decision:{id}")
            .Returns((true, new HitlDecision(id, HitlRequestStatus.Approved, "rev", null, _now)));
        _cacheProvider.TryGetValueAsync<PendingHitlRequest>($"hitl:{id}")
            .Returns((false, null));

        var result = await sut.ReplayAsync(id);

        result.Success.Should().BeFalse();
        result.ErrorReason.Should().Contain("no longer available");
    }

    [Fact]
    public async Task SuspendRequestAsync_AuditIsRecorded()
    {
        var sut = CreateSut();
        _cacheProvider.SetAsync(Arg.Any<string>(), Arg.Any<PendingHitlRequest>()).Returns(Task.FromResult<PendingHitlRequest>(null!));
        _cacheProvider.SetAsync(Arg.Any<string>(), Arg.Any<HashSet<string>>()).Returns(Task.FromResult<HashSet<string>>(null!));

        await sut.SuspendRequestAsync(BuildContext(), "audit test");

        await _audit.Received(1).AddAsync(
            Arg.Is<AuditTrail>(a => a.Status == "PENDING_HITL"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Constructor_NullConfig_ThrowsArgumentNullException()
    {
        var act = () => new HitlService(_cacheService, _audit, _clock,
            null!, _httpClientFactory, _logger);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task SuspendRequestAsync_BelowMaxPending_DoesNotThrow()
    {
        // MaxPending = 3, currently 1 pending → should not throw (tests line 54 "below limit" branch)
        var existingId = Guid.NewGuid().ToString();
        _cacheProvider.TryGetValueAsync<HashSet<string>>("hitl:index")
            .Returns((true, new HashSet<string> { existingId }));
        var existing = new PendingHitlRequest(existingId, "GET", "https://x.com",
            new Dictionary<string, string>(), null, "r", Guid.NewGuid(), _now, _now.AddSeconds(300));
        _cacheProvider.TryGetValueAsync<PendingHitlRequest>($"hitl:{existingId}")
            .Returns((true, existing));
        _cacheProvider.SetAsync(Arg.Any<string>(), Arg.Any<PendingHitlRequest>())
            .Returns(x => Task.FromResult(x.Arg<PendingHitlRequest>()));
        _cacheProvider.SetAsync(Arg.Any<string>(), Arg.Any<HashSet<string>>())
            .Returns(Task.FromResult<HashSet<string>>(null!));

        var sut = CreateSut(maxPending: 3);

        var act = async () => await sut.SuspendRequestAsync(BuildContext(), "test");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ReplayAsync_WithContentTypeHeader_SetsContentHeader()
    {
        // Exercises line 188 - Content-Type header goes to content headers
        var sut = CreateSut();
        var id = "ct-test";

        _cacheProvider.TryGetValueAsync<HitlDecision>($"hitl:decision:{id}")
            .Returns((true, new HitlDecision(id, HitlRequestStatus.Approved, "rev", null, _now)));

        var pending = new PendingHitlRequest(id, "POST", "https://upstream.local/api",
            new Dictionary<string, string> { ["Content-Type"] = "application/json" },
            """{"x":1}""", "reason", Guid.NewGuid(), _now, _now.AddSeconds(300));
        _cacheProvider.TryGetValueAsync<PendingHitlRequest>($"hitl:{id}")
            .Returns((true, pending));

        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "{}");
        _httpClientFactory.CreateClient().Returns(new HttpClient(handler));
        _cacheProvider.RemoveAsync(Arg.Any<string>()).Returns(Task.CompletedTask);
        _cacheProvider.SetAsync(Arg.Any<string>(), Arg.Any<HashSet<string>>())
            .Returns(Task.FromResult<HashSet<string>>(null!));

        var result = await sut.ReplayAsync(id);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SuspendRequestAsync_AuditFails_DoesNotThrow()
    {
        // Exercises lines 276-279: audit fail is swallowed
        _audit.AddAsync(Arg.Any<AuditTrail>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("DB down")));
        _cacheProvider.SetAsync(Arg.Any<string>(), Arg.Any<PendingHitlRequest>())
            .Returns(x => Task.FromResult(x.Arg<PendingHitlRequest>()));
        _cacheProvider.SetAsync(Arg.Any<string>(), Arg.Any<HashSet<string>>())
            .Returns(Task.FromResult<HashSet<string>>(null!));

        var sut = CreateSut();

        var act = async () => await sut.SuspendRequestAsync(BuildContext(), "test");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SuspendRequestAsync_WebhookThrowsException_DoesNotThrow()
    {
        // Exercises lines 320-323: webhook exception is swallowed
        var handler = new ThrowingHttpMessageHandler();
        _httpClientFactory.CreateClient().Returns(new HttpClient(handler));

        var sut = CreateSut(webhookUrl: "https://webhook.example.com/notify");
        _cacheProvider.SetAsync(Arg.Any<string>(), Arg.Any<PendingHitlRequest>())
            .Returns(x => Task.FromResult(x.Arg<PendingHitlRequest>()));
        _cacheProvider.SetAsync(Arg.Any<string>(), Arg.Any<HashSet<string>>())
            .Returns(Task.FromResult<HashSet<string>>(null!));

        var act = async () => await sut.SuspendRequestAsync(BuildContext(), "test");

        await act.Should().NotThrowAsync();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private sealed class FakeHttpMessageHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content)
            });
        }
    }

    private sealed class FailingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException("upstream unreachable");
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new TaskCanceledException("timeout");
    }
}

