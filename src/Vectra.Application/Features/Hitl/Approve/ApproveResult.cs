namespace Vectra.Application.Features.Hitl.Approve;

public record ApproveResult(
    int StatusCode,
    string ContentType,
    Stream ResponseBody);
