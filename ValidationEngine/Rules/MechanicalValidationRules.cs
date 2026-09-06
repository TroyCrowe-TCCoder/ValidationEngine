using System.Text.RegularExpressions;
using ValidationEngine.Infrastructure;
using ValidationEngine.Models;

namespace ValidationEngine.Rules;

/// <summary>
/// Mechanical rule checks for the checklist-driven validation engine. Implements the automated
/// checks for the rule-item markers defined across GlobalFileSpecificationStandards.md,
/// GlobalRepositoryStandards.md, and GlobalSolutionStructureStandards.md that are mechanically
/// verifiable. Rules that cannot be reliably checked by inspection are reported as manual-review
/// items instead, so they remain visible in the PR checklist rather than being silently skipped.
/// </summary>
public sealed partial class MechanicalValidationRules
{
    public static IReadOnlyList<RuleFinding> Validate(
        IReadOnlyList<FileChange> changes,
        string repositoryRoot,
        IReadOnlySet<string> excludedMarkers)
    {
        ArgumentNullException.ThrowIfNull(changes);
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        ArgumentNullException.ThrowIfNull(excludedMarkers);

        var findings = new List<RuleFinding>();

        var activeChanges = changes.Where(change => change.Status != FileChangeStatus.Deleted).ToList();
        var markdownChanges = activeChanges.Where(change => change.Path.EndsWith(".md", StringComparison.OrdinalIgnoreCase)).ToList();
        var isGlobalStandardsRepo = Directory.Exists(Path.Combine(repositoryRoot, "Docs", "Standards"));

        if (isGlobalStandardsRepo)
        {
            ValidateStandardsFileNaming(markdownChanges, excludedMarkers, findings);
            ValidateStandardsHeaderAndMarkers(markdownChanges, repositoryRoot, excludedMarkers, findings);
        }

        ValidateGitignoreExcludesWorking(repositoryRoot, isGlobalStandardsRepo, excludedMarkers, findings);
        ValidateNoTrackedWorkingFiles(activeChanges, excludedMarkers, findings);
        ValidateNoScratchOrTempFiles(activeChanges, excludedMarkers, findings);
        AddManualOnlyStandardsItems(markdownChanges, isGlobalStandardsRepo, findings);

        return findings;
    }

    private static void ValidateStandardsFileNaming(IReadOnlyList<FileChange> markdownChanges, IReadOnlySet<string> excludedMarkers, List<RuleFinding> findings)
    {
        foreach (var change in markdownChanges)
        {
            if (!StandardsPathPattern().IsMatch(change.Path) || CompanionReadmePathPattern().IsMatch(change.Path))
            {
                continue;
            }

            var fileName = Path.GetFileName(change.Path);
            if (!StandardsFileNamePattern().IsMatch(fileName) && !ExclusionAddendumReader.IsMarkerExcluded("file-specification.6.10", excludedMarkers))
            {
                AddViolation(findings, change.Path, "file-specification.6.10", "Standards file naming", $"File name '{fileName}' does not match Global<Domain>Standards.md PascalCase convention");
            }
        }
    }

    private static void ValidateStandardsHeaderAndMarkers(IReadOnlyList<FileChange> markdownChanges, string repositoryRoot, IReadOnlySet<string> excludedMarkers, List<RuleFinding> findings)
    {
        foreach (var change in markdownChanges)
        {
            if (!StandardsPathPattern().IsMatch(change.Path) || CompanionReadmePathPattern().IsMatch(change.Path))
            {
                continue;
            }

            var fullPath = Path.Combine(repositoryRoot, change.Path);
            if (!File.Exists(fullPath))
            {
                continue;
            }

            var content = File.ReadAllText(fullPath);

            if (!VersionHeaderPattern().IsMatch(content) && !ExclusionAddendumReader.IsMarkerExcluded("file-specification.6.8", excludedMarkers))
            {
                AddViolation(findings, change.Path, "file-specification.6.8", "Header block requires version", "No **Version:** MAJOR.MINOR.PATCH found");
            }

            if (!StatusHeaderPattern().IsMatch(content) && !ExclusionAddendumReader.IsMarkerExcluded("file-specification.6.8", excludedMarkers))
            {
                AddViolation(findings, change.Path, "file-specification.6.8", "Header block requires valid status", "No **Status:** Draft|Active|Superseded found");
            }

            var headings = TopLevelHeadingPattern().Matches(content).Select(match => match.Groups[1].Value.Trim()).ToList();
            if (headings.Count < 2 || headings[^2] != "Compliance Verification" || headings[^1] != "Governance")
            {
                if (!ExclusionAddendumReader.IsMarkerExcluded("file-specification.6.9", excludedMarkers))
                {
                    var lastTwo = headings.Count >= 2 ? string.Join(", ", headings[^2..]) : string.Join(", ", headings);
                    AddViolation(findings, change.Path, "file-specification.6.9", "File must end with Compliance Verification then Governance", $"Last two top-level sections were: {lastTwo}");
                }
            }

            var baseName = Path.GetFileNameWithoutExtension(change.Path);
            baseName = GlobalPrefixPattern().Replace(baseName, string.Empty);
            baseName = StandardsSuffixPattern().Replace(baseName, string.Empty);
            var derivedMarkerBase = string.Join('-', PascalCaseWordPattern().Matches(baseName).Select(match => match.Value.ToLowerInvariant()));
            var existingBaseMatch = FileMarkerBasePattern().Match(content);
            var markerBase = existingBaseMatch.Success ? existingBaseMatch.Groups[1].Value : derivedMarkerBase;

            if (!content.Contains($"<!-- STD-MARKER: {markerBase}.file -->", StringComparison.Ordinal) && !ExclusionAddendumReader.IsMarkerExcluded("file-specification.6.15", excludedMarkers))
            {
                AddViolation(findings, change.Path, "file-specification.6.15", "File-level STD-MARKER missing", $"Expected <!-- STD-MARKER: {markerBase}.file --> beneath header block");
            }
        }
    }

    private static void ValidateGitignoreExcludesWorking(string repositoryRoot, bool isGlobalStandardsRepo, IReadOnlySet<string> excludedMarkers, List<RuleFinding> findings)
    {
        if (isGlobalStandardsRepo)
        {
            return;
        }

        var gitignorePath = Path.Combine(repositoryRoot, ".gitignore");
        if (!File.Exists(gitignorePath))
        {
            if (!ExclusionAddendumReader.IsMarkerExcluded("solution-structure.14.4", excludedMarkers))
            {
                AddViolation(findings, ".gitignore", "solution-structure.14.4", "Root .gitignore required", "No .gitignore found at repository root");
            }

            return;
        }

        if (!File.ReadAllText(gitignorePath).Contains("Working/", StringComparison.Ordinal) && !ExclusionAddendumReader.IsMarkerExcluded("solution-structure.14.4", excludedMarkers))
        {
            AddViolation(findings, ".gitignore", "solution-structure.14.4", "Root .gitignore must exclude Working/", "No Working/ entry found in .gitignore");
        }
    }

    private static void ValidateNoTrackedWorkingFiles(IReadOnlyList<FileChange> activeChanges, IReadOnlySet<string> excludedMarkers, List<RuleFinding> findings)
    {
        foreach (var change in activeChanges)
        {
            if (WorkingFolderPathPattern().IsMatch(change.Path) && !change.Path.EndsWith("ValidationDiscrepancies.md", StringComparison.Ordinal))
            {
                if (!ExclusionAddendumReader.IsMarkerExcluded("solution-structure.2.2", excludedMarkers))
                {
                    AddViolation(findings, change.Path, "solution-structure.2.2", "Working/ folder must not be tracked", $"'{change.Path}' must be listed in .gitignore and not committed");
                }
            }
        }
    }

    private static void ValidateNoScratchOrTempFiles(IReadOnlyList<FileChange> activeChanges, IReadOnlySet<string> excludedMarkers, List<RuleFinding> findings)
    {
        foreach (var change in activeChanges)
        {
            var fileName = Path.GetFileName(change.Path);
            if (ScratchFileNamePattern().IsMatch(fileName) && !ExclusionAddendumReader.IsMarkerExcluded("repository.10.16", excludedMarkers))
            {
                AddViolation(findings, change.Path, "repository.10.16", "No working/temporary files permitted", $"'{change.Path}' matches a temporary-file naming pattern and must be removed before commit");
            }
        }
    }

    private static void AddManualOnlyStandardsItems(List<FileChange> markdownChanges, bool isGlobalStandardsRepo, List<RuleFinding> findings)
    {
        // GlobalFileSpecificationStandards.md itself scopes these rules to "All files in
        // Docs/Standards/ in the GlobalStandards repository" (see
        // StandardsApplicabilityMatrix.csv, file-specification row: N/A for
        // WebAppWebApi/Database/ClassLibrary). It governs authoring of standards files and has
        // no checkable surface in an application repository, so it is intentionally gated by
        // this directory heuristic rather than by ApplicableStandardsResolver/AppType — an
        // application repo's local Standards/ addendum file is governed by
        // GlobalRepositoryStandards/GlobalSolutionStructureStandards instead.
        if (!isGlobalStandardsRepo || markdownChanges.Count == 0)
        {
            return;
        }

        foreach (var change in markdownChanges.Where(change => StandardsPathPattern().IsMatch(change.Path) && !CompanionReadmePathPattern().IsMatch(change.Path)))
        {
            AddManualItem(findings, change.Path, "file-specification.6.2", $"Confirm {change.Path} owns exactly one domain and does not duplicate rules from another file");
            AddManualItem(findings, change.Path, "file-specification.6.3", $"Confirm every rule in {change.Path} is verifiable by inspection, tooling, or a defined manual check");
            AddManualItem(findings, change.Path, "file-specification.6.5", $"Confirm {change.Path} contains no rationale, history, or decision context");
            AddManualItem(findings, change.Path, "file-specification.6.6", $"Confirm every rule in {change.Path} is actionable without requiring outside context");
            AddManualItem(findings, change.Path, "file-specification.6.7", $"Confirm {change.Path} contains no speculative or future-state content");
        }
    }

    private static void AddViolation(List<RuleFinding> findings, string path, string ruleId, string summary, string detail, RulePriority priority = RulePriority.HardStop, int? lineNumber = null)
    {
        var location = lineNumber is null ? path : $"{path}:{lineNumber}";
        findings.Add(new RuleFinding
        {
            RuleId = ruleId,
            StandardFile = path,
            Severity = ViolationSeverity.Violation,
            Priority = priority,
            Message = $"{summary} — {detail} ({location})",
            FilePath = path
        });
    }

    private static void AddManualItem(List<RuleFinding> findings, string path, string ruleId, string message, RulePriority priority = RulePriority.ManualOnlyComment)
    {
        findings.Add(new RuleFinding
        {
            RuleId = ruleId,
            StandardFile = path,
            Severity = ViolationSeverity.ManualReviewItem,
            Priority = priority,
            Message = message,
            FilePath = path
        });
    }

    [GeneratedRegex(@"^Docs[\\/]Standards[\\/].+\.md$")]
    private static partial Regex StandardsPathPattern();

    // Companion readme files (file-specification.2.16) hold rationale/history and are explicitly
    // exempt from Section 3 (required file structure) and Section 4 (required header block).
    [GeneratedRegex(@"^Docs[\\/]Standards[\\/]Readme[\\/].+\.readme\.md$")]
    private static partial Regex CompanionReadmePathPattern();

    [GeneratedRegex(@"^Global[A-Za-z0-9]*Standards\.md$")]
    private static partial Regex StandardsFileNamePattern();

    [GeneratedRegex(@"\*\*Version:\*\*\s*\d+\.\d+\.\d+")]
    private static partial Regex VersionHeaderPattern();

    [GeneratedRegex(@"\*\*Status:\*\*\s*(Draft|Active|Superseded)")]
    private static partial Regex StatusHeaderPattern();

    [GeneratedRegex(@"(?m)^##\s+\d+\.\s+(.+)$")]
    private static partial Regex TopLevelHeadingPattern();

    [GeneratedRegex("^Global")]
    private static partial Regex GlobalPrefixPattern();

    [GeneratedRegex("Standards$")]
    private static partial Regex StandardsSuffixPattern();

    [GeneratedRegex("[A-Z][a-z0-9]*")]
    private static partial Regex PascalCaseWordPattern();

    [GeneratedRegex(@"(?m)^<!--\s*STD-MARKER:\s*([a-z0-9\-]+)\.1\s*-->")]
    private static partial Regex FileMarkerBasePattern();

    [GeneratedRegex(@"^Working[\\/]")]
    private static partial Regex WorkingFolderPathPattern();

    [GeneratedRegex(@"^(_scratch_|_draft_|_temp_)")]
    private static partial Regex ScratchFileNamePattern();
}
