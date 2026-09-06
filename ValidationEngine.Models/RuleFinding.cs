namespace ValidationEngine.Models;

public sealed record RuleFinding
{
    public required string RuleId { get; init; }
    public required string StandardFile { get; init; }
    public required ViolationSeverity Severity { get; init; }
    public required RulePriority Priority { get; init; }
    public required string Message { get; init; }
    public string? FilePath { get; init; }

    /// <summary>
    /// The provider(s) that produced this finding (e.g., an AI validation agent's Name),
    /// so the report stays auditable when multiple providers are active (AGT-005/AGT-006).
    /// Null for findings produced by mechanical (non-AI) rules.
    /// </summary>
    public string? Source { get; init; }

    /// <summary>
    /// Optional human-readable rationale produced by an Explain-purpose agent (AGT-012),
    /// supplementing <see cref="Message"/> with richer context for report/onboarding use.
    /// Does not affect <see cref="Severity"/> or <see cref="Priority"/> — Explain never
    /// re-decides compliance, only narrates an already-determined finding.
    /// </summary>
    public string? Rationale { get; init; }
}
