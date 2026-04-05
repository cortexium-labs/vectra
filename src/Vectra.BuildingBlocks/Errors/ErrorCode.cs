namespace Vectra.BuildingBlocks.Errors;

public readonly record struct ErrorCode(int Value, ErrorCategory Category)
{
    public static string Prefix => "VEC";
    public override string ToString() => $"{Prefix}{Value:D6}";
}