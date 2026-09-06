using System.Text.RegularExpressions;
using ValidationEngine.Models;

namespace ValidationEngine.Rules;

/// <summary>
/// Parses the coverage matrices under ValidationEngine/CoverageMatrices/*.md and extracts the
/// Manual-only and Missing rules eligible for the AGT-005 AI-assisted validation pass. Owned by
/// ValidationEngine (ValidationOrchestrator) — this is coverage-collection-building logic, not
/// something a sibling executor tool (e.g. ValidationEngine.Agent) resolves for itself. Sibling
/// tools receive an already-resolved rule set as fuel; they never read coverage matrices.
/// </summary>
public static partial class CoverageMatrixReader
{
    public static IReadOnlyList<ManualOnlyRuleEntry> ReadManualOnlyAndMissingRules(
        string coverageMatricesDirectory,
        IReadOnlyList<string> applicableMarkerBases)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(coverageMatricesDirectory);
        ArgumentNullException.ThrowIfNull(applicableMarkerBases);

        var entries = new List<ManualOnlyRuleEntry>();

        if (!Directory.Exists(coverageMatricesDirectory))
        {
            return entries;
        }

        var applicableMarkerBaseSet = new HashSet<string>(applicableMarkerBases, StringComparer.Ordinal);

        foreach (var filePath in Directory.EnumerateFiles(coverageMatricesDirectory, "CoverageMatrix-*.md").OrderBy(path => path, StringComparer.Ordinal))
        {
            var standardFile = DeriveStandardFileName(Path.GetFileName(filePath));
            var content = File.ReadAllText(filePath);

            foreach (Match rowMatch in TableRowPattern().Matches(content))
            {
                var ruleId = rowMatch.Groups["ruleId"].Value.Trim();
                var ruleSummary = rowMatch.Groups["ruleSummary"].Value.Trim();
                var confidence = rowMatch.Groups["confidence"].Value.Trim();
                var severity = rowMatch.Groups["severity"].Value.Trim();
                var technique = rowMatch.Groups["technique"].Value.Trim();
                var status = rowMatch.Groups["status"].Value.Trim();

                var markerBase = DeriveMarkerBase(ruleId);
                if (!applicableMarkerBaseSet.Contains(markerBase))
                {
                    continue;
                }

                var isManualOnly = string.Equals(confidence, "Manual-only", StringComparison.OrdinalIgnoreCase);
                var isMissing = string.Equals(status, "Missing", StringComparison.OrdinalIgnoreCase);
                if (!isManualOnly && !isMissing)
                {
                    continue;
                }

                entries.Add(new ManualOnlyRuleEntry
                {
                    RuleId = ruleId,
                    RuleSummary = ruleSummary,
                    StandardFile = standardFile,
                    Technique = technique,
                    Priority = ParsePriority(severity)
                });
            }
        }

        return entries;
    }

    private static string DeriveStandardFileName(string coverageMatrixFileName)
    {
        const string prefix = "CoverageMatrix-";
        var name = coverageMatrixFileName;
        if (name.StartsWith(prefix, StringComparison.Ordinal))
        {
            name = name[prefix.Length..];
        }

        return name;
    }

    private static string DeriveMarkerBase(string ruleId)
    {
        var firstDotIndex = ruleId.IndexOf('.');
        return firstDotIndex < 0 ? ruleId : ruleId[..firstDotIndex];
    }

    private static RulePriority ParsePriority(string severity)
    {
        // Strip trailing parenthetical qualifiers, e.g. "Manual-only-comment (until Phase 2 activation)"
        // or "Manual-only-comment (Acknowledged-future-work until repository activates Phase 2)" —
        // the qualifier is documentation context for humans, not part of the enforcement-tier value.
        var normalized = ParenthenticalQualifierPattern().Replace(severity, string.Empty).Trim();

        return normalized switch
        {
            var s when string.Equals(s, "Hard-stop", StringComparison.OrdinalIgnoreCase) => RulePriority.HardStop,
            var s when string.Equals(s, "Warning", StringComparison.OrdinalIgnoreCase) => RulePriority.Warning,
            var s when string.Equals(s, "Manual-only-comment", StringComparison.OrdinalIgnoreCase) => RulePriority.ManualOnlyComment,
            // "Duplicate" rows are pure cross-references to a rule already captured (and severity-scored)
            // elsewhere, and definitional/dash rows carry no independent severity of their own — both are
            // non-enforcing by nature, so they map to the lowest (manual-only) enforcement tier rather than
            // failing matrix parsing.
            "Duplicate" => RulePriority.ManualOnlyComment,
            var s when string.Equals(s, "N/A", StringComparison.OrdinalIgnoreCase) => RulePriority.ManualOnlyComment,
            "—" or "-" or "" => RulePriority.ManualOnlyComment,
            _ => throw new InvalidOperationException($"Unrecognized coverage matrix Severity value: '{severity}'. Expected Hard-stop, Warning, or Manual-only-comment.")
        };
    }

    [GeneratedRegex(@"\s*\([^)]*\)\s*$")]
    private static partial Regex ParenthenticalQualifierPattern();

    [GeneratedRegex(@"(?m)^\|\s*(?<ruleId>[a-z0-9\-]+\.[a-z0-9.]+)\s*\|\s*(?<ruleSummary>[^|]+?)\s*\|\s*(?<category>[^|]+?)\s*\|\s*(?<confidence>[^|]+?)\s*\|\s*(?<severity>[^|]+?)\s*\|\s*(?<frequency>[^|]+?)\s*\|\s*(?<technique>[^|]+?)\s*\|\s*(?<status>[^|]+?)\s*\|\s*$")]
    private static partial Regex TableRowPattern();
}
