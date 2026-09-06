using System.Text.Json;
using System.Text.RegularExpressions;

namespace ValidationEngine.Infrastructure;

/// <summary>
/// Reads the optional, solution-wide, blanket per-file exclusion list — distinct from the
/// AppType relevance resolver (whole-standards-file granularity) and the exclusion addendum
/// (rule-marker granularity). This is a file-level exclusion: a matched file is dropped from
/// the base <see cref="ValidationScope"/> entirely, before any rule/standard is even
/// considered, so every downstream tool (mechanical rules, the Agent, any future sibling
/// engine) benefits uniformly without needing its own file-exclusion logic.
///
/// Configuration lives at <c>Standards/FileExclusions.json</c> in the repository root,
/// alongside the exclusion addendum's <c>Standards/</c> folder. Absence of the file means
/// nothing is excluded — this is strictly opt-in, not a default behavior.
/// </summary>
public static class FileExclusionListReader
{
    private sealed record FileExclusionListDocument
    {
        public List<string> Exclude { get; init; } = [];
    }

    public static IReadOnlyList<string> Read(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);

        var configFilePath = Path.Combine(repositoryRoot, "Standards", "FileExclusions.json");
        if (!File.Exists(configFilePath))
        {
            return [];
        }

        var jsonContent = File.ReadAllText(configFilePath);
        var document = JsonSerializer.Deserialize<FileExclusionListDocument>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return document?.Exclude ?? [];
    }

    /// <summary>
    /// Determines whether a file path matches any pattern in the exclusion list. Patterns
    /// support standard shell-glob semantics: <c>*</c> matches any run of characters EXCLUDING
    /// the path separator <c>/</c> (use <c>**</c> to match across directory boundaries, e.g.
    /// <c>Templates/**</c> to match every file anywhere under <c>Templates/</c>), and <c>?</c>
    /// matches any single non-separator character. All other characters are matched literally.
    /// Matching is case-insensitive and is performed against the file's repository-relative
    /// path with forward slashes.
    /// </summary>
    public static bool IsExcluded(string filePath, IReadOnlyList<string> exclusionPatterns)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(exclusionPatterns);

        if (exclusionPatterns.Count == 0)
        {
            return false;
        }

        var normalizedPath = filePath.Replace('\\', '/');

        foreach (var pattern in exclusionPatterns)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                continue;
            }

            var normalizedPattern = pattern.Replace('\\', '/');
            if (MatchesGlob(normalizedPath, normalizedPattern))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesGlob(string path, string pattern)
    {
        // "**" -> match across path separators (any run of characters, including "/").
        // "*"  -> match within a single path segment only (excludes "/").
        // "?"  -> match a single character, excluding "/".
        const string doubleStarPlaceholder = "\u0000DOUBLESTAR\u0000";

        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace(@"\*\*", doubleStarPlaceholder, StringComparison.Ordinal)
            .Replace(@"\*", "[^/]*", StringComparison.Ordinal)
            .Replace(@"\?", "[^/]", StringComparison.Ordinal)
            .Replace(doubleStarPlaceholder, ".*", StringComparison.Ordinal) + "$";

        return Regex.IsMatch(path, regexPattern, RegexOptions.IgnoreCase);
    }
}
