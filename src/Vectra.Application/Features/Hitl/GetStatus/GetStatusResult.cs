using Vectra.Application.Abstractions.Executions;

namespace Vectra.Application.Features.Hitl.GetStatus;

public record GetStatusResult(string Id, string Status, PendingHitlRequest? Request);
