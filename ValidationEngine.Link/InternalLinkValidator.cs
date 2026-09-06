using System.Text.RegularExpressions;
using ValidationEngine.Link.Models;

namespace ValidationEngine.Link;

public interface IInternalLinkValidator
{
    LinkIssue? Validate(LinkReference reference, string repositoryRoot, string runId, string app);
}

/// <summary>
/// Resolves internal (relative-path and/or anchor) links against the repository's file system.
/// Confirms the target file exists and, if an anchor is present, that a matching heading slug
/// exists in the target file (or the source file, for anchor-only links).
/// </summary>
public sealed partial class InternalLinkValidator : IInternalLinkValidator
{
    // Mirrors .markdown-link-check.json's ignorePatterns: links whose *target* path falls under
    // these directories are out of scope regardless of which file references them (drafts and
    // reference material aren't held to the same link-integrity bar as published standards).
    private static readonly string[] ExcludedTargetSegments =
    [
        "Templates/",
        "KnowledgeBase/",
        "References/"
    ];

    // Working/ is gitignored and its files are transient/routinely deleted, so a markdown link
    // into it can never be durable. Rather than silently skipping it, flag it for removal: any
    // reference to Working/ content must be a plain-text mention, not a clickable link.
    private const string WorkingTargetSegment = "Working/";

    public LinkIssue? Validate(LinkReference reference, string repositoryRoot, string runId, string app)
    {
        var target = reference.Target;
        var (pathPart, anchor) = SplitAnchor(target);

        if (IsWorkingTarget(pathPart))
        {
            return BuildIssue(reference, runId, app, "Link points into 'Working/', which is gitignored and transient; replace with a plain-text reference instead of a markdown link.");
        }

        if (IsExcludedTarget(pathPart))
        {
            return null;
        }

        var resolvedFilePath = ResolveTargetFilePath(reference.SourceFile, pathPart, repositoryRoot);

        if (!string.IsNullOrEmpty(pathPart) && !File.Exists(resolvedFilePath) && !Directory.Exists(resolvedFilePath))
        {
            return BuildIssue(reference, runId, app, $"Target file not found: '{pathPart}'.");
        }

        if (string.IsNullOrEmpty(anchor))
        {
            return null;
        }

        var fileToCheckForAnchor = string.IsNullOrEmpty(pathPart)
            ? Path.Combine(repositoryRoot, reference.SourceFile)
            : resolvedFilePath;

        if (!File.Exists(fileToCheckForAnchor))
        {
            // Missing-file case already reported above for non-empty pathPart; an empty
            // pathPart with a missing source file is an engine-level problem, not a link issue.
            return null;
        }

        var content = File.ReadAllText(fileToCheckForAnchor);
        if (!HeadingSlugExists(content, anchor))
        {
            return BuildIssue(reference, runId, app, $"Anchor '#{anchor}' not found in target file.");
        }

        return null;
    }

    private static (string PathPart, string Anchor) SplitAnchor(string target)
    {
        var hashIndex = target.IndexOf('#');
        return hashIndex < 0
            ? (target, string.Empty)
            : (target[..hashIndex], target[(hashIndex + 1)..]);
    }

    private static bool IsExcludedTarget(string pathPart)
    {
        if (string.IsNullOrEmpty(pathPart))
        {
            return false;
        }

        var normalizedPath = pathPart.Replace('\\', '/');
        return ExcludedTargetSegments.Any(segment => normalizedPath.Contains(segment, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsWorkingTarget(string pathPart)
    {
        if (string.IsNullOrEmpty(pathPart))
        {
            return false;
        }

        var normalizedPath = pathPart.Replace('\\', '/');
        return normalizedPath.Contains(WorkingTargetSegment, StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveTargetFilePath(string sourceFile, string pathPart, string repositoryRoot)
    {
        if (string.IsNullOrEmpty(pathPart))
        {
            return Path.Combine(repositoryRoot, sourceFile);
        }

        var sourceDirectory = Path.GetDirectoryName(Path.Combine(repositoryRoot, sourceFile)) ?? repositoryRoot;
        return Path.GetFullPath(Path.Combine(sourceDirectory, pathPart));
    }

    private static bool HeadingSlugExists(string content, string anchor)
    {
        foreach (Match match in HeadingPattern().Matches(content))
        {
            var heading = match.Groups["heading"].Value;
            if (string.Equals(Slugify(heading), anchor, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string Slugify(string heading)
    {
        var lowered = heading.Trim().ToLowerInvariant();
        var withoutSpecialChars = SlugNonAlphaNumericPattern().Replace(lowered, string.Empty);
        return SlugWhitespacePattern().Replace(withoutSpecialChars, "-");
    }

    private static LinkIssue BuildIssue(LinkReference reference, string runId, string app, string issue) => new()
    {
        RunId = runId,
        App = app,
        SourceFile = reference.SourceFile,
        Link = reference.Target,
        LinkType = LinkType.Internal,
        HttpStatusCode = null,
        Issue = issue,
        ValidationDate = DateTimeOffset.UtcNow
    };

    [GeneratedRegex(@"^#{1,6}\s+(?<heading>.+)$", RegexOptions.Multiline)]
    private static partial Regex HeadingPattern();

    [GeneratedRegex(@"[^a-z0-9\s-]")]
    private static partial Regex SlugNonAlphaNumericPattern();

    [GeneratedRegex(@"\s")]
    private static partial Regex SlugWhitespacePattern();
}
