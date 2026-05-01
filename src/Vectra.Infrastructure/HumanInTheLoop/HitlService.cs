using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vectra.Application.Abstractions.Executions;
using Vectra.Application.Abstractions.Persistence;
using Vectra.Application.Models;
using Vectra.BuildingBlocks.Clock;
using Vectra.BuildingBlocks.Configuration.HumanInTheLoop;
using Vectra.Domain.AuditTrails;
using Vectra.Infrastructure.Caches;

namespace Vectra.Infrastructure.HumanInTheLoop;

public class HitlService : IHitlService
{
    private static readonly HashSet<string> _sensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization", "Cookie", "Set-Cookie", "X-Api-Key", "X-Auth-Token"
    };

    private readonly ICacheService _cache;
    private readonly IAuditRepository _audit;
    private readonly IClock _clock;
    private readonly HumanInTheLoopConfiguration _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<HitlService> _logger;

    public HitlService(
        ICacheService cache,
        IAuditRepository audit,
        IClock clock,
        IOptions<HumanInTheLoopConfiguration> config,
        IHttpClientFactory httpClientFactory,
        ILogger<HitlService> logger)
    {
        _cache = cache;
        _audit = audit;
        _clock = clock;
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<string> SuspendRequestAsync(RequestContext context, string reason, CancellationToken cancellationToken = default)
    {
        if (_config.MaxPendingRequests > 0)
        {
            var current = await GetAllPendingAsync(cancellationToken);
            if (current.Count >= _config.MaxPendingRequests)
            {
                _logger.LogWarning("HITL request rejected for agent {AgentId}: limit of {Max} concurrent pending requests reached", context.AgentId, _config.MaxPendingRequests);
                throw new InvalidOperationException($"The maximum number of concurrent pending HITL requests ({_config.MaxPendingRequests}) has been reached.");
            }
        }

        var id = Guid.NewGuid().ToString();
        var now = _clock.UtcNow;
        var expiresAt = now.AddSeconds(_config.TimeoutSeconds);

        var pending = new PendingHitlRequest(
            Id: id,
            Method: context.Method,
            Url: context.TargetUrl,
            Headers: RedactHeaders(context.Headers),
            Body: context.Body,
            Reason: reason,
            AgentId: context.AgentId,
            Timestamp: now,
            ExpiresAt: expiresAt);

        var ttl = TimeSpan.FromSeconds(_config.TimeoutSeconds);
        await _cache.Current.SetAsync($"hitl:{id}", pending);

        // Track pending ID in an index so GetAllPendingAsync works
        var index = await GetPendingIndexAsync();
        index.Add(id);
        await _cache.Current.SetAsync("hitl:index", index);

        _logger.LogInformation("HITL request {HitlId} suspended for agent {AgentId}. Reason: {Reason}. Expires: {ExpiresAt}",
            id, context.AgentId, reason, expiresAt);

        await RecordAuditAsync(id, context.AgentId, context.Method, context.Path, "PENDING_HITL", reason, cancellationToken);
        await SendWebhookNotificationAsync(pending, cancellationToken);

        return id;
    }

    public async Task<PendingHitlRequest?> GetPendingAsync(string id, CancellationToken cancellationToken = default)
    {
        var (found, value) = await _cache.Current.TryGetValueAsync<PendingHitlRequest>($"hitl:{id}");
        if (!found || value is null)
            return null;

        if (_clock.UtcNow > value.ExpiresAt)
        {
            _logger.LogWarning("HITL request {HitlId} has expired", id);
            await CleanupAsync(id);
            return null;
        }

        return value;
    }

    public async Task<HitlRequestStatus> GetStatusAsync(string id, CancellationToken cancellationToken = default)
    {
        var (approvedFound, _) = await _cache.Current.TryGetValueAsync<HitlDecision>($"hitl:decision:{id}");
        if (approvedFound)
        {
            var (_, decision) = await _cache.Current.TryGetValueAsync<HitlDecision>($"hitl:decision:{id}");
            return decision?.Status ?? HitlRequestStatus.NotFound;
        }

        var (pendingFound, pending) = await _cache.Current.TryGetValueAsync<PendingHitlRequest>($"hitl:{id}");
        if (!pendingFound || pending is null)
            return HitlRequestStatus.NotFound;

        if (_clock.UtcNow > pending.ExpiresAt)
        {
            await CleanupAsync(id);
            return HitlRequestStatus.Expired;
        }

        return HitlRequestStatus.Pending;
    }

    public async Task<IReadOnlyList<PendingHitlRequest>> GetAllPendingAsync(CancellationToken cancellationToken = default)
    {
        var index = await GetPendingIndexAsync();
        var results = new List<PendingHitlRequest>();

        foreach (var id in index.ToList())
        {
            var item = await GetPendingAsync(id, cancellationToken);
            if (item is not null)
                results.Add(item);
        }

        return results.AsReadOnly();
    }

    public async Task ApproveAsync(string id, string reviewerId, string? comment, CancellationToken cancellationToken = default)
    {
        await RecordDecisionAsync(id, HitlRequestStatus.Approved, reviewerId, comment, cancellationToken);
        _logger.LogInformation("HITL request {HitlId} approved by reviewer {ReviewerId}", id, reviewerId);
    }

    public async Task DenyAsync(string id, string reviewerId, string? comment, CancellationToken cancellationToken = default)
    {
        await RecordDecisionAsync(id, HitlRequestStatus.Denied, reviewerId, comment, cancellationToken);
        _logger.LogInformation("HITL request {HitlId} denied by reviewer {ReviewerId}", id, reviewerId);
    }

    public async Task RemoveAsync(string id, CancellationToken cancellationToken = default)
        => await CleanupAsync(id);

    public async Task<HitlReplayResult> ReplayAsync(string id, CancellationToken cancellationToken = default)
    {
        // 1. Verify the decision is Approved
        var status = await GetStatusAsync(id, cancellationToken);
        if (status == HitlRequestStatus.NotFound)
            return new HitlReplayResult(false, null, "HITL request not found.", null, null);
        if (status == HitlRequestStatus.Expired)
            return new HitlReplayResult(false, null, "HITL request has expired.", null, null);
        if (status == HitlRequestStatus.Denied)
            return new HitlReplayResult(false, null, "HITL request was denied by a reviewer.", null, null);
        if (status == HitlRequestStatus.Pending)
            return new HitlReplayResult(false, null, "HITL request is still awaiting a reviewer decision.", null, null);

        // 2. Retrieve the original suspended request
        var (found, pending) = await _cache.Current.TryGetValueAsync<PendingHitlRequest>($"hitl:{id}");
        if (!found || pending is null)
            return new HitlReplayResult(false, null, "Original request data is no longer available.", null, null);

        // 3. Build and dispatch the upstream HTTP request
        _logger.LogInformation("Replaying approved HITL request {HitlId} → {Method} {Url}", id, pending.Method, pending.Url);

        var httpClient = _httpClientFactory.CreateClient();
        var upstreamRequest = new HttpRequestMessage
        {
            Method = new HttpMethod(pending.Method),
            RequestUri = new Uri(pending.Url, UriKind.Absolute),
            Content = pending.Body is not null ? new StringContent(pending.Body) : null
        };

        foreach (var (key, value) in pending.Headers)
        {
            if (string.Equals(key, "Content-Type", StringComparison.OrdinalIgnoreCase))
                upstreamRequest.Content?.Headers.TryAddWithoutValidation(key, value);
            else
                upstreamRequest.Headers.TryAddWithoutValidation(key, value);
        }

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(upstreamRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or TimeoutException)
        {
            _logger.LogError(ex, "Replay of HITL request {HitlId} failed during upstream call", id);
            return new HitlReplayResult(false, 503, $"Upstream unreachable: {ex.Message}", null, null);
        }

        // 4. Collect response headers (excluding hop-by-hop)
        var responseHeaders = response.Headers
            .Concat(response.Content.Headers)
            .ToDictionary(h => h.Key, h => string.Join(", ", h.Value));

        var responseBody = await response.Content.ReadAsStreamAsync(cancellationToken);

        // 5. Audit the completed replay
        await RecordAuditAsync(id, pending.AgentId, pending.Method, pending.Url,
            "HITL_REPLAYED", $"Upstream responded with {(int)response.StatusCode}", cancellationToken);

        // 6. Clean up — request fully processed, no longer needed in cache
        await CleanupAsync(id);
        await _cache.Current.RemoveAsync($"hitl:decision:{id}");

        return new HitlReplayResult(true, (int)response.StatusCode, null, responseHeaders, responseBody);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task RecordDecisionAsync(
        string id,
        HitlRequestStatus status,
        string reviewerId,
        string? comment,
        CancellationToken cancellationToken)
    {
        var decision = new HitlDecision(
            Id: id,
            Status: status,
            ReviewerId: reviewerId,
            Comment: comment,
            DecidedAt: _clock.UtcNow);

        var ttl = TimeSpan.FromSeconds(_config.TimeoutSeconds);
        await _cache.Current.SetAsync($"hitl:decision:{id}", decision);

        var (found, pending) = await _cache.Current.TryGetValueAsync<PendingHitlRequest>($"hitl:{id}");

        await RecordAuditAsync(
            id,
            found && pending is not null ? pending.AgentId : Guid.Empty,
            found && pending is not null ? pending.Method : "UNKNOWN",
            found && pending is not null ? pending.Url : "UNKNOWN",
            status == HitlRequestStatus.Approved ? "HITL_APPROVED" : "HITL_DENIED",
            $"Reviewer: {reviewerId}. {comment}",
            cancellationToken);
    }

    private async Task RecordAuditAsync(
        string hitlId,
        Guid agentId,
        string method,
        string path,
        string status,
        string? reason,
        CancellationToken cancellationToken)
    {
        try
        {
            var audit = new AuditTrail
            {
                AgentId = agentId,
                Action = $"{method} {path}",
                TargetUrl = path,
                Status = status,
                Reason = reason,
                Intent = $"HITL:{hitlId}",
                Timestamp = _clock.UtcNow
            };
            await _audit.AddAsync(audit, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record audit for HITL request {HitlId}", hitlId);
        }
    }

    private async Task CleanupAsync(string id)
    {
        await _cache.Current.RemoveAsync($"hitl:{id}");
        var index = await GetPendingIndexAsync();
        index.Remove(id);
        await _cache.Current.SetAsync("hitl:index", index);
    }

    private async Task<HashSet<string>> GetPendingIndexAsync()
    {
        var (found, index) = await _cache.Current.TryGetValueAsync<HashSet<string>>("hitl:index");
        return found && index is not null ? index : new HashSet<string>();
    }

    private async Task SendWebhookNotificationAsync(PendingHitlRequest pending, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_config.NotificationWebhookUrl))
            return;

        try
        {
            var payload = new
            {
                pending.Id,
                AgentId = pending.AgentId.ToString(),
                pending.Method,
                pending.Url,
                pending.Reason,
                pending.Timestamp,
                pending.ExpiresAt
            };

            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.PostAsJsonAsync(_config.NotificationWebhookUrl, payload, cancellationToken);

            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("HITL webhook notification for request {HitlId} returned {StatusCode}", pending.Id, (int)response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send HITL webhook notification for request {HitlId}", pending.Id);
        }
    }

    private static Dictionary<string, string> RedactHeaders(Dictionary<string, string> headers)
        => headers.ToDictionary(
            h => h.Key,
            h => _sensitiveHeaders.Contains(h.Key) ? "[REDACTED]" : h.Value);
}
