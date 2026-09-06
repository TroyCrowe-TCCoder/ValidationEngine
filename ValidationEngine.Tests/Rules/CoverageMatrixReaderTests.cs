using ValidationEngine.Models;
using ValidationEngine.Rules;

namespace ValidationEngine.Tests.Rules;

public class CoverageMatrixReaderTests
{
    [Fact]
    public void ReadManualOnlyAndMissingRules_ParenthenticalQualifierOnManualOnlyComment_ReturnsManualOnlyComment()
    {
        var directory = CreateMatrixDirectory(
            "| perf.2.1 | Some rule | Development | Heuristic | Manual-only-comment (until Phase 2 activation) | Periodic | Detect it | Missing |");

        var result = CoverageMatrixReader.ReadManualOnlyAndMissingRules(directory, new[] { "perf" });

        var entry = Assert.Single(result);
        Assert.Equal(RulePriority.ManualOnlyComment, entry.Priority);
    }

    [Fact]
    public void ReadManualOnlyAndMissingRules_DuplicateSeverity_ReturnsManualOnlyComment()
    {
        var directory = CreateMatrixDirectory(
            "| db.5.4 | Cross-reference rule | Development | Manual-only | Duplicate | Every-Commit | Cross-reference only | Missing |");

        var result = CoverageMatrixReader.ReadManualOnlyAndMissingRules(directory, new[] { "db" });

        var entry = Assert.Single(result);
        Assert.Equal(RulePriority.ManualOnlyComment, entry.Priority);
    }

    [Fact]
    public void ReadManualOnlyAndMissingRules_DefinitionalDashSeverity_ReturnsManualOnlyComment()
    {
        var directory = CreateMatrixDirectory(
            "| repo.8.1 | Definitional rule | Procedural | Heuristic | — (definitional, no independent severity) | — | Regex scan | Missing |");

        var result = CoverageMatrixReader.ReadManualOnlyAndMissingRules(directory, new[] { "repo" });

        var entry = Assert.Single(result);
        Assert.Equal(RulePriority.ManualOnlyComment, entry.Priority);
    }

    [Fact]
    public void ReadManualOnlyAndMissingRules_UnrecognizedSeverity_Throws()
    {
        var directory = CreateMatrixDirectory(
            "| bad.1.1 | Bad rule | Development | Manual-only | NotARealSeverity | Every-Commit | n/a | Missing |");

        Assert.Throws<InvalidOperationException>(
            () => CoverageMatrixReader.ReadManualOnlyAndMissingRules(directory, new[] { "bad" }));
    }

    private static string CreateMatrixDirectory(string tableRow)
    {
        var directory = Path.Combine(Path.GetTempPath(), "CoverageMatrixReaderTests_" + Guid.NewGuid());
        Directory.CreateDirectory(directory);

        var content = "# Coverage Matrix — Test\n\n" +
            "| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |\n" +
            "|---|---|---|---|---|---|---|---|\n" +
            tableRow + "\n";

        File.WriteAllText(Path.Combine(directory, "CoverageMatrix-Test.md"), content);
        return directory;
    }
}
