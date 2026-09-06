using ValidationEngine.Models;

namespace ValidationEngine.Rules;

/// <summary>
/// A single Manual-only or Missing standards rule extracted from a coverage matrix,
/// eligible for AI-assisted evaluation (AGT-005).
/// </summary>
public sealed record ManualOnlyRuleEntry
{
    public required string RuleId { get; init; }
    public required string RuleSummary { get; init; }
    public required string StandardFile { get; init; }
    public required string Technique { get; init; }
    public required RulePriority Priority { get; init; }
}
