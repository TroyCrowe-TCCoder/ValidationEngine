using ValidationEngine.Infrastructure;
using ValidationEngine.Models;

namespace ValidationEngine.Reporting;

/// <summary>
/// Top-level JSON document shape produced by <see cref="JsonReportRenderer"/>.
/// </summary>
internal sealed record ValidationReportDocument
{
    public DateTime EvaluationTimestampUtc { get; }
    public RunContextDto? RunContext { get; }
    public int ExitCode { get; }
    public ValidationReportSummary Summary { get; }
    public IReadOnlyList<FindingDto> Violations { get; }
    public IReadOnlyList<FindingDto> ManualReviewItems { get; }
    public IReadOnlyList<EngineError> EngineErrors { get; }

    public ValidationReportDocument(ValidationReport report, int exitCode)
    {
        Violations = report.Findings
            .Where(finding => finding.Severity == ViolationSeverity.Violation)
            .Select(finding => new FindingDto(finding))
            .ToList();
        ManualReviewItems = report.Findings
            .Where(finding => finding.Severity == ViolationSeverity.ManualReviewItem)
            .Select(finding => new FindingDto(finding))
            .ToList();

        EvaluationTimestampUtc = DateTime.UtcNow;
        RunContext = report.RunContext is null ? null : new RunContextDto(report.RunContext);
        ExitCode = exitCode;
        Summary = new ValidationReportSummary(report, Violations.Count, ManualReviewItems.Count);
        EngineErrors = report.EngineErrors;
    }
}
