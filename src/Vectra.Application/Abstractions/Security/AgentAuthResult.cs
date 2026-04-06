namespace Vectra.Application.Abstractions.Security;

public sealed record AgentAuthResult
{
    public bool Succeeded { get; init; }
    public string? Token { get; init; }
    public string? Error { get; init; }

    public static AgentAuthResult Success(string? token = null)
        => new() { Succeeded = true, Token = token };

    public static AgentAuthResult Failure(string error)
        => new() { Succeeded = false, Error = error };
}