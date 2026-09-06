using ValidationEngine.Models;

namespace ValidationEngine.Reporting;

/// <summary>
/// Aggregate counts/flags summarizing a <see cref="ValidationReport"/> for JSON output.
/// </summary>
internal sealed record ValidationReportSummary
{
    public int ViolationCount { get; }
    public int ManualReviewItemCount { get; }
    public int EngineErrorCount { get; }
    public bool HasViolations { get; }
    public bool HasEngineErrors { get; }

    public ValidationReportSummary(ValidationReport report, int violationCount, int manualReviewItemCount)
    {
        ViolationCount = violationCount;
        ManualReviewItemCount = manualReviewItemCount;
        EngineErrorCount = report.EngineErrors.Count;
        HasViolations = report.HasViolations;
        HasEngineErrors = report.HasEngineErrors;
    }
}
