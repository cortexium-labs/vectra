using Vectra.Application.Models;
using Vectra.Domain.Agents;

namespace Vectra.Application.Abstractions.Executions;

public interface IHitlService
{
    Task<string> SuspendRequestAsync(
        RequestContext context, 
        string reason, 
        CancellationToken cancellationToken = default);

    Task<PendingHitlRequest?> GetPendingAsync(
        string id, 
        CancellationToken cancellationToken = default);

    Task<HitlRequestStatus> GetStatusAsync(
        string id, 
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<PendingHitlRequest> Items, int TotalCount)> GetAllPendingPagedAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PendingHitlRequest>> GetAllPendingAsync(
        CancellationToken cancellationToken = default);

    Task ApproveAsync(
        string id, 
        string reviewerId, 
        string? comment, 
        CancellationToken cancellationToken = default);

    Task DenyAsync(
        string id, 
        string reviewerId, 
        string? comment, 
        CancellationToken cancellationToken = default);

    Task RemoveAsync(
        string id, 
        CancellationToken cancellationToken = default);

    Task<HitlReplayResult> ReplayAsync(
        string id, 
        CancellationToken cancellationToken = default);
}

public record HitlReplayResult(
    bool Success,
    int? StatusCode,
    string? ErrorReason,
    Dictionary<string, string>? ResponseHeaders,
    Stream? ResponseBody);

public enum HitlRequestStatus { Pending, Approved, Denied, Expired, NotFound }

public record PendingHitlRequest(
    string Id,
    string Method,
    string Url,
    Dictionary<string, string> Headers,
    string? Body,
    string Reason,
    Guid AgentId,
    DateTime Timestamp,
    DateTime ExpiresAt);

public record HitlDecision(
    string Id,
    HitlRequestStatus Status,
    string ReviewerId,
    string? Comment,
    DateTime DecidedAt);
