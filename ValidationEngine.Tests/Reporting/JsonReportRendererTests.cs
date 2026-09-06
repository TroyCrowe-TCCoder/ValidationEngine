using System.Text.Json;
using ValidationEngine.Infrastructure;
using ValidationEngine.Models;
using ValidationEngine.Reporting;

namespace ValidationEngine.Tests.Reporting;

public sealed class JsonReportRendererTests
{
    [Fact]
    public void Render_WhenReportHasBlockingViolation_IncludesFindingUnderViolationsArray()
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
                    Message = "Missing guard clause.",
                    FilePath = "src/Foo.cs"
                }
            },
            EngineErrors = new List<EngineError>()
        };

        var renderedJson = JsonReportRenderer.Render(report, exitCode: 1);
        using var document = JsonDocument.Parse(renderedJson);
        var root = document.RootElement;

        Assert.Equal(1, root.GetProperty("summary").GetProperty("violationCount").GetInt32());
        Assert.True(root.GetProperty("summary").GetProperty("hasViolations").GetBoolean());

        var violations = root.GetProperty("violations");
        Assert.Equal(1, violations.GetArrayLength());
        Assert.Equal("coding.3.4", violations[0].GetProperty("ruleId").GetString());
        Assert.Equal("Missing guard clause.", violations[0].GetProperty("message").GetString());
        Assert.Equal("src/Foo.cs", violations[0].GetProperty("filePath").GetString());
    }

    [Fact]
    public void Render_WhenReportHasManualReviewFinding_IncludesFindingUnderManualReviewItemsArray()
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
            EngineErrors = new List<EngineError>(),
            RunContext = new RunContext
            {
                RepositoryName = "SampleRepo",
                RepositoryRoot = "C:\\repos\\SampleRepo",
                Mode = ValidationRunMode.System,
                TargetBranch = "origin/dev",
                DetectedAppType = "WebAppWebApi",
                ApplicableStandards = new List<string> { "coding" },
                ChangesEvaluated = 1
            }
        };

        var renderedJson = JsonReportRenderer.Render(report, exitCode: 0);
        using var document = JsonDocument.Parse(renderedJson);
        var root = document.RootElement;

        Assert.Equal(1, root.GetProperty("summary").GetProperty("manualReviewItemCount").GetInt32());
        Assert.Equal("SampleRepo", root.GetProperty("runContext").GetProperty("repositoryName").GetString());
        Assert.Equal("origin/dev", root.GetProperty("runContext").GetProperty("targetBranch").GetString());
        Assert.Equal("system", root.GetProperty("runContext").GetProperty("mode").GetProperty("value").GetString());

        var manualReviewItems = root.GetProperty("manualReviewItems");
        Assert.Equal(1, manualReviewItems.GetArrayLength());
        Assert.Equal("solution-structure.2", manualReviewItems[0].GetProperty("ruleId").GetString());
    }

    [Fact]
    public void Render_WhenReportHasEngineErrors_IncludesEngineErrorsArrayAndFlags()
    {
        var report = new ValidationReport
        {
            Findings = new List<RuleFinding>(),
            EngineErrors = new List<EngineError>
            {
                new() { Source = "Resolver", Message = "Matrix file not found." }
            }
        };

        var renderedJson = JsonReportRenderer.Render(report, exitCode: 2);
        using var document = JsonDocument.Parse(renderedJson);
        var root = document.RootElement;

        Assert.True(root.GetProperty("summary").GetProperty("hasEngineErrors").GetBoolean());
        Assert.Equal(2, root.GetProperty("exitCode").GetInt32());

        var engineErrors = root.GetProperty("engineErrors");
        Assert.Equal(1, engineErrors.GetArrayLength());
        Assert.Equal("Resolver", engineErrors[0].GetProperty("source").GetString());
        Assert.Equal("Matrix file not found.", engineErrors[0].GetProperty("message").GetString());
    }

    [Fact]
    public void Render_WhenReportIsClean_HasEmptyArraysAndZeroExitCode()
    {
        var report = new ValidationReport
        {
            Findings = new List<RuleFinding>(),
            EngineErrors = new List<EngineError>()
        };

        var renderedJson = JsonReportRenderer.Render(report, exitCode: 0);
        using var document = JsonDocument.Parse(renderedJson);
        var root = document.RootElement;

        Assert.Equal(0, root.GetProperty("violations").GetArrayLength());
        Assert.Equal(0, root.GetProperty("manualReviewItems").GetArrayLength());
        Assert.Equal(0, root.GetProperty("engineErrors").GetArrayLength());
        Assert.Equal(0, root.GetProperty("exitCode").GetInt32());
        Assert.False(root.GetProperty("summary").GetProperty("hasViolations").GetBoolean());
        Assert.False(root.GetProperty("summary").GetProperty("hasEngineErrors").GetBoolean());
    }
}
