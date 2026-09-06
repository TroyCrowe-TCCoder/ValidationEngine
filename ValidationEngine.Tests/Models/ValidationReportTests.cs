using ValidationEngine.Models;

namespace ValidationEngine.Tests.Models;

public sealed class ValidationReportTests
{
    [Fact]
    public void HasViolations_WhenFindingsContainViolationSeverity_ReturnsTrue()
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

        Assert.True(report.HasViolations);
    }

    [Fact]
    public void HasViolations_WhenFindingsOnlyContainManualReviewItems_ReturnsFalse()
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

        Assert.False(report.HasViolations);
    }

    [Fact]
    public void HasEngineErrors_WhenEngineErrorsListIsEmpty_ReturnsFalse()
    {
        var report = new ValidationReport
        {
            Findings = new List<RuleFinding>(),
            EngineErrors = new List<EngineError>()
        };

        Assert.False(report.HasEngineErrors);
    }

    [Fact]
    public void HasEngineErrors_WhenEngineErrorsListIsNotEmpty_ReturnsTrue()
    {
        var report = new ValidationReport
        {
            Findings = new List<RuleFinding>(),
            EngineErrors = new List<EngineError>
            {
                new() { Source = "Resolver", Message = "Matrix file not found." }
            }
        };

        Assert.True(report.HasEngineErrors);
    }
}

