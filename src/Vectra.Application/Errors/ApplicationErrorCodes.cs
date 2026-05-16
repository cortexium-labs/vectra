using Vectra.BuildingBlocks.Errors;

namespace Vectra.Application.Errors;

public static class ApplicationErrorCodes
{
    public static readonly ErrorCode AgentNotFound = new(0501002, ErrorCategory.Persistence);
    public static readonly ErrorCode PolicyNotFound = new(0501003, ErrorCategory.Persistence);
    public static readonly ErrorCode HitlRequestNotFound = new(0501004, ErrorCategory.Persistence);
    public static readonly ErrorCode InvalidClientSession = new(0800003, ErrorCategory.Security);
    public static readonly ErrorCode RequestCancelled = new(0000499, ErrorCategory.Core);
}