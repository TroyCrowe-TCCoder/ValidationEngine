using ValidationEngine.Infrastructure;
using ValidationEngine.Models;
using ValidationEngine.Rules;

namespace ValidationEngine.Tests.Rules;

public sealed class MechanicalValidationRulesTests : IDisposable
{
    private readonly string _fixtureRoot;
    private static readonly HashSet<string> NoExclusions = new(StringComparer.Ordinal);

    public MechanicalValidationRulesTests()
    {
        _fixtureRoot = Path.Combine(Path.GetTempPath(), $"ValidationEngineTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_fixtureRoot);
    }

    [Fact]
    public void Validate_WhenStandardsFileNameIsNotPascalCase_ReturnsNamingViolation()
    {
        var standardsDirectory = Path.Combine(_fixtureRoot, "Docs", "Standards");
        Directory.CreateDirectory(standardsDirectory);
        File.WriteAllText(Path.Combine(standardsDirectory, "bad_name.md"), "content");

        var changes = new List<FileChange> { new() { Status = FileChangeStatus.Added, Path = "Docs/Standards/bad_name.md" } };

        var findings = MechanicalValidationRules.Validate(changes, _fixtureRoot, NoExclusions);

        Assert.Contains(findings, finding => finding.RuleId == "file-specification.6.10" && finding.Severity == ViolationSeverity.Violation);
    }

    [Fact]
    public void Validate_WhenStandardsFileMissingVersionAndStatus_ReturnsHeaderViolations()
    {
        var standardsDirectory = Path.Combine(_fixtureRoot, "Docs", "Standards");
        Directory.CreateDirectory(standardsDirectory);
        File.WriteAllText(Path.Combine(standardsDirectory, "GlobalSampleStandards.md"), "# Header\nNo version or status here.");

        var changes = new List<FileChange> { new() { Status = FileChangeStatus.Modified, Path = "Docs/Standards/GlobalSampleStandards.md" } };

        var findings = MechanicalValidationRules.Validate(changes, _fixtureRoot, NoExclusions);

        Assert.Contains(findings, finding => finding.RuleId == "file-specification.6.8" && finding.Message.Contains("version", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(findings, finding => finding.RuleId == "file-specification.6.8" && finding.Message.Contains("status", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_WhenMarkerExcluded_SuppressesViolation()
    {
        var standardsDirectory = Path.Combine(_fixtureRoot, "Docs", "Standards");
        Directory.CreateDirectory(standardsDirectory);
        File.WriteAllText(Path.Combine(standardsDirectory, "bad_name.md"), "content");

        var changes = new List<FileChange> { new() { Status = FileChangeStatus.Added, Path = "Docs/Standards/bad_name.md" } };
        var excludedMarkers = new HashSet<string>(StringComparer.Ordinal) { "file-specification.6.10" };

        var findings = MechanicalValidationRules.Validate(changes, _fixtureRoot, excludedMarkers);

        Assert.DoesNotContain(findings, finding => finding.RuleId == "file-specification.6.10");
    }

    [Fact]
    public void Validate_WhenNonStandardsRepoMissingGitignore_ReturnsGitignoreViolation()
    {
        var changes = new List<FileChange>();

        var findings = MechanicalValidationRules.Validate(changes, _fixtureRoot, NoExclusions);

        Assert.Contains(findings, finding => finding.RuleId == "solution-structure.14.4");
    }

    [Fact]
    public void Validate_WhenGitignoreExcludesWorking_ReturnsNoGitignoreViolation()
    {
        File.WriteAllText(Path.Combine(_fixtureRoot, ".gitignore"), "Working/\nbin/\n");

        var changes = new List<FileChange>();

        var findings = MechanicalValidationRules.Validate(changes, _fixtureRoot, NoExclusions);

        Assert.DoesNotContain(findings, finding => finding.RuleId == "solution-structure.14.4");
    }

    [Fact]
    public void Validate_WhenTrackedFileUnderWorkingFolder_ReturnsTrackedWorkingViolation()
    {
        var changes = new List<FileChange> { new() { Status = FileChangeStatus.Added, Path = "Working/scratch.txt" } };

        var findings = MechanicalValidationRules.Validate(changes, _fixtureRoot, NoExclusions);

        Assert.Contains(findings, finding => finding.RuleId == "solution-structure.2.2");
    }

    [Fact]
    public void Validate_WhenTrackedValidationDiscrepanciesUnderWorking_DoesNotReturnTrackedWorkingViolation()
    {
        var changes = new List<FileChange> { new() { Status = FileChangeStatus.Added, Path = "Working/ValidationDiscrepancies.md" } };

        var findings = MechanicalValidationRules.Validate(changes, _fixtureRoot, NoExclusions);

        Assert.DoesNotContain(findings, finding => finding.RuleId == "solution-structure.2.2");
    }

    [Fact]
    public void Validate_WhenFileNameMatchesScratchPattern_ReturnsTemporaryFileViolation()
    {
        var changes = new List<FileChange> { new() { Status = FileChangeStatus.Added, Path = "src/_scratch_notes.cs" } };

        var findings = MechanicalValidationRules.Validate(changes, _fixtureRoot, NoExclusions);

        Assert.Contains(findings, finding => finding.RuleId == "repository.10.16");
    }

    public void Dispose()
    {
        if (Directory.Exists(_fixtureRoot))
        {
            Directory.Delete(_fixtureRoot, recursive: true);
        }
    }
}
