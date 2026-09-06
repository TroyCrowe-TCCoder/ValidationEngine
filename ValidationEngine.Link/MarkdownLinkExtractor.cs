using System.Text.RegularExpressions;
using ValidationEngine.Link.Models;

namespace ValidationEngine.Link;

/// <summary>
/// Parses Markdown link occurrences out of a file's text content. Handles inline
/// <c>[text](target)</c> links and bare autolinks (<c>&lt;https://...&gt;</c> or raw
/// <c>https://...</c> URLs). Reference-style link definitions (<c>[text][ref]</c> /
/// <c>[ref]: target</c>) are intentionally out of scope for the initial version.
/// </summary>
public static partial class MarkdownLinkExtractor
{
    public static IReadOnlyList<LinkReference> Extract(string sourceFile, string content)
    {
        var references = new List<LinkReference>();
        var lines = content.Split('\n');
        var inFencedCodeBlock = false;

        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var rawLine = lines[lineIndex];
            var lineNumber = lineIndex + 1;

            if (FencedCodeBlockDelimiterPattern().IsMatch(rawLine))
            {
                inFencedCodeBlock = !inFencedCodeBlock;
                continue;
            }

            if (inFencedCodeBlock)
            {
                continue;
            }

            // Blank out inline code spans (`...`) so example syntax such as
            // `[Section N](#n-section-heading)` isn't mistaken for a real link.
            var line = InlineCodeSpanPattern().Replace(rawLine, match => new string(' ', match.Length));

            foreach (Match match in InlineLinkPattern().Matches(line))
            {
                references.Add(new LinkReference
                {
                    SourceFile = sourceFile,
                    RawText = match.Value,
                    Target = match.Groups["target"].Value.Trim(),
                    LineNumber = lineNumber
                });
            }

            foreach (Match match in AutolinkPattern().Matches(line))
            {
                references.Add(new LinkReference
                {
                    SourceFile = sourceFile,
                    RawText = match.Value,
                    Target = match.Groups["url"].Value.Trim(),
                    LineNumber = lineNumber
                });
            }
        }

        return references;
    }

    // Matches [text](target) — target excludes ')' and whitespace to avoid overreaching into
    // trailing prose; a leading '!' (image syntax) is permitted and included in the match.
    [GeneratedRegex(@"!?\[[^\]]*\]\((?<target>[^)\s]+)(?:\s+""[^""]*"")?\)")]
    private static partial Regex InlineLinkPattern();

    // Matches inline code spans (`...`) so their contents can be blanked out before link
    // extraction runs, preventing example syntax from being treated as real links.
    [GeneratedRegex(@"`[^`\n]*`")]
    private static partial Regex InlineCodeSpanPattern();

    // Matches a fenced code block delimiter line (``` or ~~~, optionally indented).
    [GeneratedRegex(@"^\s*(```|~~~)")]
    private static partial Regex FencedCodeBlockDelimiterPattern();

    // Matches bare autolinks: <https://...> or a raw http(s):// URL not already part of an
    // inline link (best-effort — does not attempt full CommonMark autolink grammar). Excludes
    // matches immediately followed by a backtick, since `https://` used as inline-code prose
    // (e.g. illustrating a scheme change) is not an actual link target.
    [GeneratedRegex(@"<(?<url>https?://[^\s>]+)>|(?<![(\[])(?<url>https?://[^\s)>\]`]+)(?!`)")]
    private static partial Regex AutolinkPattern();
}
