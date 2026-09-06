namespace ValidationEngine.Infrastructure;

/// <summary>
/// Captures the outcome of a sibling validation tool (e.g. LinkValidationEngine, see VAL-011)
/// launched as an independent OS process so it can run concurrently with the main
/// <see cref="ValidationOrchestrator"/> pass.
/// </summary>
public sealed class ExternalToolResult
{
    public required string ToolName { get; init; }

    public required int ExitCode { get; init; }

    public required string StandardOutput { get; init; }

    public required string StandardError { get; init; }

    public bool Succeeded => ExitCode == 0;
}
