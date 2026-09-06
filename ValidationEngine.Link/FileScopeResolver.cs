using ValidationEngine.Infrastructure;

namespace ValidationEngine.Link;

/// <summary>
/// Resolves the set of Markdown files in scope for a run: by default, files changed relative to
/// a base branch (mirroring how ValidationEngine resolves its own changeset); in full-audit
/// mode, every tracked Markdown file in the repository.
/// </summary>
public static class FileScopeResolver
{
    // Mirrors the path-segment ignore list in .markdown-link-check.json (VAL-002) so both tools
    // scope-exclude the same directories: Templates/, KnowledgeBase/, References/ are
    // deliberately out of scope for link/anchor enforcement (drafts and reference material are
    // not held to the same link-integrity bar as published standards). Working/ is not included
    // here: it is gitignored/untracked, so it is never source-scanned as a Markdown file anyway,
    // but links pointing INTO Working/ from tracked files are still flagged by
    // InternalLinkValidator, since such links can never be durable.
    private static readonly string[] ExcludedPathSegments =
    [
        "Templates/",
        "KnowledgeBase/",
        "References/"
    ];

    public static IReadOnlyList<string> Resolve(string repositoryRoot, bool fullAudit, string? baseBranch)
    {
        var changes = fullAudit
            ? GitChangeResolver.GetAllTrackedFiles(repositoryRoot)
            : GitChangeResolver.GetChangesAgainstTargetBranch(repositoryRoot, baseBranch ?? "dev");

        // Merge the hardcoded segment list above with the shared, opt-in
        // Standards/FileExclusions.json used by ValidationEngine (FileExclusionListReader), so a
        // buyer editing that one file also governs link scope instead of it silently having no
        // effect on this tool.
        var sharedExclusionPatterns = ValidationEngine.Infrastructure.FileExclusionListReader.Read(repositoryRoot);

        return changes
            .Where(change => change.Status != FileChangeStatus.Deleted)
            .Select(change => change.Path)
            .Where(path => path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            .Where(path => !IsExcluded(path) && !ValidationEngine.Infrastructure.FileExclusionListReader.IsExcluded(path, sharedExclusionPatterns))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsExcluded(string path)
    {
        var normalizedPath = path.Replace('\\', '/');
        return ExcludedPathSegments.Any(segment => normalizedPath.Contains(segment, StringComparison.OrdinalIgnoreCase));
    }
}
