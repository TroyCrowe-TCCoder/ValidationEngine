using ValidationEngine.Infrastructure;

namespace ValidationEngine.Models;

/// <summary>
/// Run metadata describing how a validation run was executed and what it evaluated. This is
/// descriptive context about the run itself — not a finding — and must never be surfaced as a
/// violation or manual-review item.
/// </summary>
public sealed record RunContext
{
    public required string RepositoryName { get; init; }
    public required string RepositoryRoot { get; init; }
    public required ValidationRunMode Mode { get; init; }
    public string? TargetBranch { get; init; }
    public required string DetectedAppType { get; init; }
    public required IReadOnlyList<string> ApplicableStandards { get; init; }
    public required int ChangesEvaluated { get; init; }
}
