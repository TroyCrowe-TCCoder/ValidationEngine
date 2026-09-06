using ValidationEngine.Models;
using ValidationEngine.Reporting;

namespace ValidationEngine.Tests.Reporting;

public sealed class MarkdownReportRendererTests
{
    [Fact]
    public void Render_WhenReportHasManualReviewFinding_IncludesFindingUnderManualReviewSection()
    {
        var report = new ValidationReport
        {
            Findings = new List<RuleFinding>
            {
                new()
                {
                    RuleId = "solution-structure.2",
                    StandardFile = "GlobalSolutionStructureStandards",
                    Severity = ViolationSeverity.ManualReviewItem,
                    Priority = RulePriority.ManualOnlyComment,
                    Message = "Verify folder naming."
                }
            },
            EngineErrors = new List<EngineError>()
        };

        var renderedReport = MarkdownReportRenderer.Render(report);

        Assert.Contains("## Manual Review Required", renderedReport, StringComparison.Ordinal);
        Assert.Contains("[solution-structure.2] (ManualOnlyComment) Verify folder naming.", renderedReport, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_WhenReportHasBlockingViolation_IncludesFindingUnderBlockingViolationsSection()
    {
        var report = new ValidationReport
        {
            Findings = new List<RuleFinding>
            {
                new()
                {
                    RuleId = "coding.3.4",
                    StandardFile = "GlobalCodingStandards",
                    Severity = ViolationSeverity.Violation,
                    Priority = RulePriority.HardStop,
                    Message = "Missing guard clause."
                }
            },
            EngineErrors = new List<EngineError>()
        };

        var renderedReport = MarkdownReportRenderer.Render(report);

        Assert.Contains("## Blocking Violations", renderedReport, StringComparison.Ordinal);
        Assert.Contains("[coding.3.4] (HardStop) Missing guard clause.", renderedReport, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_WhenReportHasNoEngineErrors_OmitsEngineErrorsSection()
    {
        var report = new ValidationReport
        {
            Findings = new List<RuleFinding>(),
            EngineErrors = new List<EngineError>()
        };

        var renderedReport = MarkdownReportRenderer.Render(report);

        Assert.DoesNotContain("## Engine Errors", renderedReport, StringComparison.Ordinal);
    }

    [Fact]
    public void Render_WhenReportHasEngineErrors_IncludesEngineErrorsSection()
    {
        var report = new ValidationReport
        {
            Findings = new List<RuleFinding>(),
            EngineErrors = new List<EngineError>
            {
                new() { Source = "Resolver", Message = "Matrix file not found." }
            }
        };

        var renderedReport = MarkdownReportRenderer.Render(report);

        Assert.Contains("## Engine Errors", renderedReport, StringComparison.Ordinal);
        Assert.Contains("[Resolver] Matrix file not found.", renderedReport, StringComparison.Ordinal);
    }
}
