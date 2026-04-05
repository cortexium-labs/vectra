using Vectra.BuildingBlocks.Results;

namespace Vectra.BuildingBlocks.Errors;

public class Error
{
    public ErrorCode ErrorCode { get; }
    public string Message { get; }
    public ErrorType Type { get; }
    public Dictionary<string, string[]>? ValidationErrors { get; }

    private Error(ErrorCode errorCode, string message, ErrorType type,
                  Dictionary<string, string[]>? validationErrors = null)
    {
        ErrorCode = errorCode;
        Message = message;
        Type = type;
        ValidationErrors = validationErrors;
    }

    public static Error Validation(ErrorCode errorCode, string message, Dictionary<string, string[]> errors)
        => new(errorCode, message, ErrorType.Validation, errors);

    public static Error NotFound(ErrorCode errorCode, string message)
        => new(errorCode, message, ErrorType.NotFound);

    public static Error Conflict(ErrorCode errorCode, string message)
        => new(errorCode, message, ErrorType.Conflict);

    public static Error Unauthorized(ErrorCode errorCode, string message)
        => new(errorCode, message, ErrorType.Unauthorized);

    public static Error Forbidden(ErrorCode errorCode, string message)
        => new(errorCode, message, ErrorType.Forbidden);

    public static Error Failure(ErrorCode errorCode, string message)
        => new(errorCode, message, ErrorType.Failure);

    public static Error FromCode(ErrorCode errorCode, string message,
                                 Dictionary<string, string[]>? validationErrors = null)
    {
        var type = errorCode.Category switch
        {
            ErrorCategory.Core when errorCode.Value / 1000 == 400 => ErrorType.Validation,
            ErrorCategory.Persistence when errorCode.Value / 1000 == 404 => ErrorType.NotFound,
            ErrorCategory.Persistence when errorCode.Value / 1000 == 409 => ErrorType.Conflict,
            ErrorCategory.Security when errorCode.Value / 1000 == 401 => ErrorType.Unauthorized,
            ErrorCategory.Security when errorCode.Value / 1000 == 403 => ErrorType.Forbidden,
            _ => ErrorType.Failure
        };
        return new Error(errorCode, message, type, validationErrors);
    }

    public override string ToString() => $"{ErrorCode}: {Message}";
}