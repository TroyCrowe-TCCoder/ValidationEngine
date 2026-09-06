using ValidationEngine.Rules;

namespace ValidationEngine.Infrastructure;

/// <summary>
/// Formalizes the "fuel" every sibling standalone validation engine (see
/// <see cref="ExternalToolRunner"/>) is fed: the resolved changeset plus the resolved rule set
/// this run already computed. This is shared infrastructure, not owned by any single engine —
/// <see cref="ValidationOrchestrator"/> resolves scope and rule-set membership exactly once per
/// run, and every sibling tool is a pure executor: it receives its metadata (changeset) and its
/// rule-set collection as fuel and validates only those rules, with no independent resolution
/// path of its own. There is no standalone/no-fuel fallback — a sibling tool is only ever invoked
/// by <see cref="ValidationOrchestrator"/>, which always supplies this fuel.
/// </summary>
public static class ExternalToolFuel
{
    /// <summary>
    /// Builds the CLI argument fragment a sibling standalone engine should receive to be fed the
    /// same changeset fuel this run resolved. Returns an empty string when <paramref name="changes"/>
    /// is null or empty (e.g. a Manual run, which audits the full tracked-file set rather than a
    /// changeset).
    /// </summary>
    public static string BuildChangesetArguments(IReadOnlyList<FileChange>? changes)
    {
        if (changes is null || changes.Count == 0)
        {
            return string.Empty;
        }

        var changedPaths = changes
            .Where(change => change.Status != FileChangeStatus.Deleted)
            .Select(change => change.Path);

        return $"--changed-files \"{string.Join(';', changedPaths)}\"";
    }

    /// <summary>
    /// Builds the CLI argument fragment carrying the resolved manual-only/missing rule set this
    /// run already computed (via <c>CoverageMatrixReader</c> + <see cref="ValidationScopeDeriver"/>),
    /// so the sibling AI-agent engine only ever validates the rules it is handed rather than
    /// re-reading coverage matrices or re-resolving applicable standards itself. Each rule is
    /// serialized as <c>RuleId|RuleSummary|StandardFile|Technique|Priority</c>, with entries
    /// separated by <c>;;</c> (rule summaries may themselves contain semicolons).
    /// </summary>
    public static string BuildRulesArguments(IReadOnlyList<ManualOnlyRuleEntry> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        if (rules.Count == 0)
        {
            return string.Empty;
        }

        var serializedRules = rules.Select(rule =>
            $"{rule.RuleId}|{rule.RuleSummary}|{rule.StandardFile}|{rule.Technique}|{rule.Priority}");

        return $"--rules \"{string.Join(";;", serializedRules)}\"";
    }
}
