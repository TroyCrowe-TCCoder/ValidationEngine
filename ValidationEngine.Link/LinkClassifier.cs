using ValidationEngine.Link.Models;

namespace ValidationEngine.Link;

public static class LinkClassifier
{
    public static LinkType Classify(string target)
    {
        if (target.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            target.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return LinkType.External;
        }

        // mailto:, tel:, and other non-http schemes are treated as external (unresolvable via
        // relative-path resolution) but are skipped by the external validator since they aren't
        // HTTP-checkable; anchor-only (#heading) and relative-path targets are internal.
        if (target.Contains(':') && !target.StartsWith('#') && !IsWindowsDriveLetterPath(target))
        {
            return LinkType.External;
        }

        return LinkType.Internal;
    }

    private static bool IsWindowsDriveLetterPath(string target) =>
        target.Length >= 2 && char.IsAsciiLetter(target[0]) && target[1] == ':';
}
