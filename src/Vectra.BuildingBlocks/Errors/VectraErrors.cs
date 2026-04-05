namespace Vectra.BuildingBlocks.Errors;

// Hierarchical Error Codes ([CC][MM][NNN] format)
public static class VectraErrors
{
    // ─────────────────────────────────────────────────────────────
    // 00 SYSTEM ERRORS (CC=00)
    // ─────────────────────────────────────────────────────────────
    public static readonly ErrorCode SystemFailure = new(0000001, ErrorCategory.System);

    // ─────────────────────────────────────────────────────────────
    // 01 INFRASTRUCTURE ERRORS (CC=01)
    // ─────────────────────────────────────────────────────────────
    public static readonly ErrorCode FileNotFound = new(0102001, ErrorCategory.Infrastructure); // MM=00 core infra

    // ─────────────────────────────────────────────────────────────
    // 02 SERIALIZATION ERRORS (CC=02)
    // ─────────────────────────────────────────────────────────────
    public static readonly ErrorCode SerializationFailed = new(0200001, ErrorCategory.Serialization);
    public static readonly ErrorCode DeserializationFailed = new(0200002, ErrorCategory.Serialization);
    public static readonly ErrorCode InvalidJson = new(0200003, ErrorCategory.Serialization);

    // ─────────────────────────────────────────────────────────────
    // 03 SECURITY ERRORS (CC=03)
    // ─────────────────────────────────────────────────────────────
    public static readonly ErrorCode Unauthorized = new(0301001, ErrorCategory.Security);

    // ─────────────────────────────────────────────────────────────
    // 04 VALIDATION ERRORS (CC=04)
    // ─────────────────────────────────────────────────────────────
    // Core validation (MM=00)
    public static readonly ErrorCode ValidationFailed = new(0400001, ErrorCategory.Core);
    public static readonly ErrorCode RequiredFieldMissing = new(0400002, ErrorCategory.Core);

    // ─────────────────────────────────────────────────────────────
    // 05 NOT FOUND ERRORS (CC=05)
    // ─────────────────────────────────────────────────────────────
    public static readonly ErrorCode ResourceNotFound = new(0501001, ErrorCategory.Persistence);

    // ─────────────────────────────────────────────────────────────
    // 06 CONFLICT ERRORS (CC=06)
    // ─────────────────────────────────────────────────────────────
    public static readonly ErrorCode DuplicateResource = new(0601001, ErrorCategory.Persistence);

    // ─────────────────────────────────────────────────────────────
    // 07 FORBIDDEN ERRORS (CC=07)
    // ─────────────────────────────────────────────────────────────
    public static readonly ErrorCode AccessDenied = new(0700001, ErrorCategory.Security);

    // ─────────────────────────────────────────────────────────────
    // 08 UNAUTHORIZED ERRORS (CC=08)
    // ─────────────────────────────────────────────────────────────
    public static readonly ErrorCode MissingCredentials = new(0800001, ErrorCategory.Security);
    public static readonly ErrorCode ExpiredSession = new(0800002, ErrorCategory.Security);
}