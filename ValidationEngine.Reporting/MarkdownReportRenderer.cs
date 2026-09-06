using System.Globalization;
using System.Text;
using ValidationEngine.Models;

namespace ValidationEngine.Reporting;

/// <summary>
/// Default plug-in implementation of <see cref="IMarkdownReportRenderer"/>. Renders a
/// <see cref="ValidationReport"/> as human-readable Markdown. Consumers reference this
/// concrete type explicitly to opt into it; the core engine only depends on the interface.
/// </summary>
public sealed class MarkdownReportRenderer : IMarkdownReportRenderer
{
    string IMarkdownReportRenderer.Render(ValidationReport report) => Render(report);

    public static string Render(ValidationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var builder = new StringBuilder();
        builder.AppendLine("# Validation Discrepancies");
        builder.AppendLine();

        AppendManualReviewSection(builder, report);
        AppendViolationSection(builder, report);
        AppendEngineErrorSection(builder, report);

        return builder.ToString();
    }

    private static void AppendManualReviewSection(StringBuilder builder, ValidationReport report)
    {
        builder.AppendLine("## Manual Review Required (not blocking; must be addressed in PR review)");
        builder.AppendLine();

        var manualReviewItems = report.Findings.Where(finding => finding.Severity == ViolationSeverity.ManualReviewItem);
        foreach (var finding in manualReviewItems)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"- [{finding.RuleId}] ({finding.Priority}) {finding.Message}");
            AppendRationale(builder, finding);
        }

        builder.AppendLine();
    }

    private static void AppendViolationSection(StringBuilder builder, ValidationReport report)
    {
        builder.AppendLine("## Blocking Violations");
        builder.AppendLine();

        var violations = report.Findings.Where(finding => finding.Severity == ViolationSeverity.Violation);
        foreach (var finding in violations)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"- [{finding.RuleId}] ({finding.Priority}) {finding.Message}");
            AppendRationale(builder, finding);
        }

        builder.AppendLine();
    }

    private static void AppendEngineErrorSection(StringBuilder builder, ValidationReport report)
    {
        if (!report.HasEngineErrors)
        {
            return;
        }

        builder.AppendLine("## Engine Errors");
        builder.AppendLine();

        foreach (var engineError in report.EngineErrors)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"- [{engineError.Source}] {engineError.Message}");
        }

        builder.AppendLine();
    }

    private static void AppendRationale(StringBuilder builder, RuleFinding finding)
    {
        if (string.IsNullOrWhiteSpace(finding.Rationale))
        {
            return;
        }

        builder.AppendLine(CultureInfo.InvariantCulture, $"  - Rationale: {finding.Rationale}");
    }
}
