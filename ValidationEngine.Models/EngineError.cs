namespace ValidationEngine.Models;

public sealed record EngineError
{
    public required string Source { get; init; }
    public required string Message { get; init; }
}
