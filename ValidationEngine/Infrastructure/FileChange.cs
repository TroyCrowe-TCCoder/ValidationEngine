namespace ValidationEngine.Infrastructure;

public sealed record FileChange
{
    public required FileChangeStatus Status { get; init; }
    public required string Path { get; init; }
    public string? OldPath { get; init; }
}
