using System.Text.RegularExpressions;

namespace ValidationEngine.Infrastructure;

/// <summary>
/// Reads repository-local addendum files (<c>Standards/*.md</c>) and resolves which global rule
/// markers are excluded (suppressed) for the current repository.
/// </summary>
/// <remarks>
/// Suppression happens at the point a finding would be reported (see
/// <see cref="IsMarkerExcluded(string, IReadOnlySet{string})"/>) rather than by removing the rule
/// from the run. This is intentional — see
/// <c>KnowledgeBase.md#suppression-vs-removing-a-rule-from-the-run-addendum-handling</c> for the
/// rationale (expiry enforcement, auditability, and forward compatibility with future replacement
/// addenda, https://github.com/TroyCrowe-TCCoder/ValidationEngine/issues/7).
/// </remarks>
public sealed partial class ExclusionAddendumReader
{
    public sealed record AddendumReadResult
    {
        public required IReadOnlySet<string> ExcludedMarkers { get; init; }
        public required IReadOnlyList<ExpiredExclusion> ExpiredExclusions { get; init; }
    }

    public sealed record ExpiredExclusion
    {
        public required string AddendumFileName { get; init; }
        public required string MarkerId { get; init; }
    }

    public static AddendumReadResult Read(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);

        var excludedMarkers = new HashSet<string>(StringComparer.Ordinal);
        var expiredExclusions = new List<ExpiredExclusion>();
        var addendumDirectory = Path.Combine(repositoryRoot, "Standards");

        if (!Directory.Exists(addendumDirectory))
        {
            return new AddendumReadResult { ExcludedMarkers = excludedMarkers, ExpiredExclusions = expiredExclusions };
        }

        foreach (var addendumFilePath in Directory.EnumerateFiles(addendumDirectory, "*.md"))
        {
            var addendumContent = File.ReadAllText(addendumFilePath);
            var addendumFileName = Path.GetFileName(addendumFilePath);

            foreach (Match markerMatch in MarkerIdPattern().Matches(addendumContent))
            {
                var markerId = markerMatch.Groups[1].Value;
                var remainder = addendumContent[markerMatch.Index..];
                var reviewDateMatch = PlannedReviewDatePattern().Match(remainder);

                if (reviewDateMatch.Success)
                {
                    var reviewDate = DateTime.ParseExact(reviewDateMatch.Groups[1].Value, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
                    if (reviewDate < DateTime.Now)
                    {
                        expiredExclusions.Add(new ExpiredExclusion { AddendumFileName = addendumFileName, MarkerId = markerId });
                        continue;
                    }
                }

                excludedMarkers.Add(markerId);
            }
        }

        return new AddendumReadResult { ExcludedMarkers = excludedMarkers, ExpiredExclusions = expiredExclusions };
    }

    public static bool IsMarkerExcluded(string markerId, IReadOnlySet<string> excludedMarkers)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(markerId);
        ArgumentNullException.ThrowIfNull(excludedMarkers);

        if (excludedMarkers.Contains(markerId))
        {
            return true;
        }

        var parts = markerId.Split('.');
        if (parts.Length >= 3 && excludedMarkers.Contains($"{parts[0]}.{parts[1]}"))
        {
            return true;
        }

        return excludedMarkers.Contains($"{parts[0]}.file");
    }

    [GeneratedRegex(@"(?m)^(?:[-*]\s+)?\*\*Marker ID:\*\*\s*(\S+)")]
    private static partial Regex MarkerIdPattern();

    [GeneratedRegex(@"\*\*Planned Review Date:\*\*\s*(\d{4}-\d{2}-\d{2})")]
    private static partial Regex PlannedReviewDatePattern();
}
