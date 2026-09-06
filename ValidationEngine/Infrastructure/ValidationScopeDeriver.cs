namespace ValidationEngine.Infrastructure;

/// <summary>
/// Produces tool-specific derived sets from the single base ValidationScope. Every consumer
/// (mechanical rules, the Agent's manual-only rule set, a future Link engine, or any other
/// sibling tool) asks for its own view of the same base scope through this one mechanism,
/// instead of writing bespoke re-filtering logic against the raw changeset. Multiple tools can
/// receive an identical derived set when their filters happen to agree; that is expected.
/// </summary>
public static class ValidationScopeDeriver
{
    /// <summary>
    /// Derives the file subset of the base scope relevant to a tool, using a file-level
    /// predicate (e.g. extension/technology relevance).
    /// </summary>
    public static IReadOnlyList<FileChange> DeriveFiles(
        ValidationScope scope,
        Func<FileChange, bool>? filePredicate = null)
    {
        ArgumentNullException.ThrowIfNull(scope);

        return filePredicate is null
            ? scope.Changes
            : [.. scope.Changes.Where(filePredicate)];
    }

    /// <summary>
    /// Derives a tool-specific rule set from a candidate rule collection, excluding any rule marker
    /// already skipped for this repository (repo-local skip/exclusion addendum).
    /// </summary>
    public static IReadOnlyList<TRule> DeriveRules<TRule>(
        ValidationScope scope,
        IReadOnlyList<TRule> candidateRules,
        Func<TRule, string> ruleIdSelector,
        IReadOnlySet<string> excludedMarkers)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(candidateRules);
        ArgumentNullException.ThrowIfNull(ruleIdSelector);
        ArgumentNullException.ThrowIfNull(excludedMarkers);

        return [.. candidateRules.Where(rule => !ExclusionAddendumReader.IsMarkerExcluded(ruleIdSelector(rule), excludedMarkers))];
    }
}
