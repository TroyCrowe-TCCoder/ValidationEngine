using System.Text.RegularExpressions;
using ValidationEngine.Models;

namespace ValidationEngine.Rules;

public sealed partial class ChecklistMetaCheckRule
{
    public static IReadOnlyList<RuleFinding> Validate(string standardsDirectory, IReadOnlyList<string> inScopeStandardsFileNames)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(standardsDirectory);
        ArgumentNullException.ThrowIfNull(inScopeStandardsFileNames);

        var findings = new List<RuleFinding>();

        foreach (var fileName in inScopeStandardsFileNames)
        {
            var filePath = Path.Combine(standardsDirectory, fileName);
            if (!File.Exists(filePath))
            {
                findings.Add(BuildFinding(fileName, "meta", "Standards file missing", $"Expected standards file not found at {filePath}"));
                continue;
            }

            var content = File.ReadAllText(filePath);
            var markerBase = ResolveMarkerBase(fileName, content);

            ValidateChecklistItemsCarryMarkers(fileName, content, markerBase, findings);
            ValidateSectionMarkersResolve(fileName, content, findings);
        }

        return findings;
    }

    private static string ResolveMarkerBase(string fileName, string content)
    {
        var baseName = Path.GetFileNameWithoutExtension(fileName);
        baseName = GlobalPrefixPattern().Replace(baseName, string.Empty);
        baseName = StandardsSuffixPattern().Replace(baseName, string.Empty);

        var derivedMarkerBase = string.Join('-', PascalCaseWordPattern().Matches(baseName).Select(match => match.Value.ToLowerInvariant()));

        var existingBaseMatch = FileMarkerBasePattern().Match(content);
        return existingBaseMatch.Success ? existingBaseMatch.Groups[1].Value : derivedMarkerBase;
    }

    private static void ValidateChecklistItemsCarryMarkers(string fileName, string content, string markerBase, List<RuleFinding> findings)
    {
        var complianceSectionMatch = ComplianceSectionPattern().Match(content);
        if (!complianceSectionMatch.Success)
        {
            findings.Add(BuildFinding(fileName, $"{markerBase}.checklist", "Compliance Verification section missing", "No ## N. Compliance Verification section found immediately before Governance"));
            return;
        }

        var expectedMarkerPattern = new Regex($@"<!--\s*STD-MARKER:\s*{Regex.Escape(markerBase)}\.\d+\.\d+\s*-->");
        foreach (Match checklistLine in ChecklistBulletPattern().Matches(complianceSectionMatch.Value))
        {
            if (!expectedMarkerPattern.IsMatch(checklistLine.Value))
            {
                findings.Add(BuildFinding(fileName, $"{markerBase}.checklist", "Checklist item missing rule-item marker", "Every Compliance Verification checklist bullet must carry its own file.section.item marker"));
            }
        }
    }

    private static void ValidateSectionMarkersResolve(string fileName, string content, List<RuleFinding> findings)
    {
        var sectionNumbers = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match sectionMatch in SectionHeadingPattern().Matches(content))
        {
            sectionNumbers.Add(sectionMatch.Groups[1].Value);
        }

        var markerBase = ResolveMarkerBase(fileName, content);

        foreach (Match markerMatch in AllMarkerPattern().Matches(content))
        {
            var markerId = markerMatch.Groups[1].Value;
            if (markerId == $"{markerBase}.file")
            {
                continue;
            }

            var remainder = markerId[(markerBase.Length + 1)..];
            var parts = remainder.Split('.');
            if (parts.Length >= 2)
            {
                continue;
            }

            if (!sectionNumbers.Contains(parts[0]))
            {
                findings.Add(BuildFinding(fileName, markerId, "Marker does not resolve to an existing section", $"No heading numbered {parts[0]} found in {fileName}"));
            }
        }
    }

    private static RuleFinding BuildFinding(string fileName, string markerId, string summary, string detail)
    {
        return new RuleFinding
        {
            RuleId = markerId,
            StandardFile = fileName,
            Severity = ViolationSeverity.Violation,
            Priority = RulePriority.HardStop,
            Message = $"{summary} — {detail}",
            FilePath = fileName
        };
    }

    [GeneratedRegex("^Global")]
    private static partial Regex GlobalPrefixPattern();

    [GeneratedRegex("Standards$")]
    private static partial Regex StandardsSuffixPattern();

    [GeneratedRegex("[A-Z][a-z0-9]*")]
    private static partial Regex PascalCaseWordPattern();

    [GeneratedRegex(@"(?m)^<!--\s*STD-MARKER:\s*([a-z0-9\-]+)\.1\s*-->")]
    private static partial Regex FileMarkerBasePattern();

    [GeneratedRegex(@"(?ms)^##\s+\d+\.\s+Compliance Verification.*?(?=^##\s+\d+\.\s+Governance)")]
    private static partial Regex ComplianceSectionPattern();

    [GeneratedRegex(@"(?m)^- \[ \] .+$")]
    private static partial Regex ChecklistBulletPattern();

    [GeneratedRegex(@"(?m)^#{2,3}\s+(\d+(?:\.\d+)?)\.\s")]
    private static partial Regex SectionHeadingPattern();

    [GeneratedRegex(@"<!--\s*STD-MARKER:\s*([a-z0-9\-]+\.[a-z0-9.]+)\s*-->")]
    private static partial Regex AllMarkerPattern();
}
