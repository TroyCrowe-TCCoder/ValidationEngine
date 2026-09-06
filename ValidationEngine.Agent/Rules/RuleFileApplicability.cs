using ValidationEngine.Rules;

namespace ValidationEngine.Agent.Rules;

/// <summary>
/// AGT-007: File-level applicability lookup for Manual-only/Missing rules, used to avoid
/// invoking the model for rule/file pairings that are obviously irrelevant (e.g. a C#
/// coding rule evaluated against a markdown file). Exclude-based: every standard file
/// applies to all changed files by default; only standards known to be scoped to a
/// specific technology are narrowed to their relevant extensions.
/// </summary>
public static class RuleFileApplicability
{
    // null => applies to every file in the change set (no narrowing).
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>?> StandardFileExtensions =
        new Dictionary<string, IReadOnlyList<string>?>(StringComparer.OrdinalIgnoreCase)
        {
            ["GlobalCodingStandards.md"] = [".cs"],
            ["GlobalDatabaseStandards.md"] = [".sql"],
            ["GlobalReactProjectStandards.md"] = [".tsx", ".jsx", ".ts", ".js"],
            ["GlobalTypeScriptStandards.md"] = [".ts", ".tsx"],
            ["GlobalAzureDevOpsPipelineStandards.md"] = [".yml", ".yaml"],
            ["GlobalNuGetLibraryStandards.md"] = [".csproj", ".config"]
        };

    /// <summary>
    /// Builds a rule -> applicable-file lookup for the given rules and change set in a single
    /// pass, so the AGT-005 orchestrator loop only invokes the model for genuinely applicable
    /// rule/file pairings instead of the full rules x files cross product.
    /// </summary>
    public static IReadOnlyDictionary<ManualOnlyRuleEntry, IReadOnlyList<T>> BuildApplicabilityLookup<T>(
        IReadOnlyList<ManualOnlyRuleEntry> rules,
        IReadOnlyList<T> files,
        Func<T, string> pathSelector)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(pathSelector);

        var filesByExtension = files
            .GroupBy(file => Path.GetExtension(pathSelector(file)), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, IReadOnlyList<T> (group) => [.. group], StringComparer.OrdinalIgnoreCase);

        var lookup = new Dictionary<ManualOnlyRuleEntry, IReadOnlyList<T>>();

        foreach (var rule in rules)
        {
            var applicableExtensions = GetApplicableExtensions(rule.StandardFile);

            lookup[rule] = applicableExtensions is null
                ? files
                : [.. applicableExtensions
                    .SelectMany(extension => filesByExtension.TryGetValue(extension, out var matches) ? matches : [])
                    .Distinct()];
        }

        return lookup;
    }

    private static IReadOnlyList<string>? GetApplicableExtensions(string standardFile) =>
        StandardFileExtensions.TryGetValue(standardFile, out var extensions) ? extensions : null;
}
