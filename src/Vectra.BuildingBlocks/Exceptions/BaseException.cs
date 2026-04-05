using Vectra.BuildingBlocks.Errors;

namespace Vectra.BuildingBlocks.Exceptions;

public abstract class BaseException : Exception
{
    public ErrorCode ErrorCode { get; }

    protected BaseException(ErrorCode errorCode, string message, Exception? innerException = null)
        : base($"{errorCode}: {message}", innerException)
    {
        ErrorCode = errorCode;
    }

    protected BaseException(Error error, Exception? innerException = null)
        : this(error.ErrorCode, error.Message, innerException)
    { }

    public override string ToString() => $"{ErrorCode}: {Message}";
}