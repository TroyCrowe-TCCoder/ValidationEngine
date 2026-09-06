namespace ValidationEngine.Resolver;

public sealed class ApplicableStandardsResolver
{
    public static IReadOnlyList<string> Resolve(RepositoryProfile repositoryProfile, IReadOnlyList<ApplicabilityEntry> applicabilityEntries)
    {
        ArgumentNullException.ThrowIfNull(repositoryProfile);
        ArgumentNullException.ThrowIfNull(applicabilityEntries);

        var exclusionLookup = BuildExclusionLookup(applicabilityEntries, repositoryProfile.AppType, out var conditionalMarkerBases);

        var applicableMarkerBases = new List<string>();

        foreach (var entry in applicabilityEntries)
        {
            if (exclusionLookup.Contains(entry.MarkerBase))
            {
                continue;
            }

            if (conditionalMarkerBases.Contains(entry.MarkerBase) && !IsConditionalStandardApplicable(entry, repositoryProfile))
            {
                continue;
            }

            applicableMarkerBases.Add(entry.MarkerBase);
        }

        return applicableMarkerBases;
    }

    /// <summary>
    /// Builds an O(1)-lookup exclusion set for the given app type: a standard's <c>MarkerBase</c> is added
    /// to the set when its status is <see cref="ApplicabilityStatus.NotApplicable"/> for that app type,
    /// treating the matrix as "applicable by default, excluded when marked N/A" rather than scanning each
    /// entry's per-app-type status column on every resolution.
    /// </summary>
    private static HashSet<string> BuildExclusionLookup(
        IReadOnlyList<ApplicabilityEntry> applicabilityEntries,
        AppType appType,
        out HashSet<string> conditionalMarkerBases)
    {
        var exclusionLookup = new HashSet<string>(StringComparer.Ordinal);
        conditionalMarkerBases = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in applicabilityEntries)
        {
            var status = SelectStatus(entry, appType);

            if (status == ApplicabilityStatus.NotApplicable)
            {
                exclusionLookup.Add(entry.MarkerBase);
            }
            else if (status == ApplicabilityStatus.Conditional)
            {
                conditionalMarkerBases.Add(entry.MarkerBase);
            }
        }

        return exclusionLookup;
    }

    private static ApplicabilityStatus SelectStatus(ApplicabilityEntry entry, AppType appType)
    {
        return appType switch
        {
            AppType.WebAppWebApi => entry.WebAppWebApi,
            AppType.Database => entry.Database,
            AppType.ClassLibrary => entry.ClassLibrary,
            _ => throw new ArgumentOutOfRangeException(nameof(appType), appType, "Unrecognized app type.")
        };
    }

    private static bool IsConditionalStandardApplicable(ApplicabilityEntry entry, RepositoryProfile repositoryProfile)
    {
        if (!repositoryProfile.HasSpaClient)
        {
            return false;
        }

        if (string.Equals(entry.MarkerBase, "react-project", StringComparison.Ordinal))
        {
            return repositoryProfile.SpaClientUsesReact;
        }

        return true;
    }
}
