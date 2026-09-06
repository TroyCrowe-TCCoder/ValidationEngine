using ValidationEngine.Models;

namespace ValidationEngine.Reporting;

/// <summary>
/// Finding shape for JSON output. Severity is intentionally omitted — it is already implied
/// by which array (violations vs manualReviewItems) the finding appears under.
/// </summary>
internal sealed record FindingDto
{
    public string RuleId { get; }
    public string StandardFile { get; }
    public RulePriority Priority { get; }
    public string Message { get; }
    public string? FilePath { get; }

    /// <summary>
    /// Optional Explain-purpose rationale (AGT-012). Null unless an Explain provider is active.
    /// </summary>
    public string? Rationale { get; }

    public FindingDto(RuleFinding finding)
    {
        RuleId = finding.RuleId;
        StandardFile = finding.StandardFile;
        Priority = finding.Priority;
        Message = finding.Message;
        FilePath = finding.FilePath;
        Rationale = finding.Rationale;
    }
}
