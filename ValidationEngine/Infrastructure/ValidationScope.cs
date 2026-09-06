namespace ValidationEngine.Infrastructure;

/// <summary>
/// The single, canonical in-scope set for a validation run: the resolved changeset (Step 3),
/// carried alongside the applicable standards resolved from this repository's AppType
/// (ApplicableStandardsResolver, Step 5) \u2014 the only file/standards-relevance filter that exists.
/// Rule-marker exclusions (the repo-local skip/exclusion addendum, Step 4, ExclusionAddendumReader)
/// are optional \u2014 a repository may or may not have one \u2014 and are applied per-tool via
/// ValidationScopeDeriver, not against this base set directly.
///
/// Every tool-specific rule/file set (mechanical rules, the Agent, a future Link engine, or any
/// other sibling engine) is derived FROM this one set via ValidationScopeDeriver, rather than
/// each tool independently re-filtering the raw changeset. Many tools may end up with identical
/// derived sets; that is expected and is not something to prevent.
/// </summary>
public sealed record ValidationScope
{
    public required IReadOnlyList<FileChange> Changes { get; init; }
    public required IReadOnlyList<string> ApplicableStandards { get; init; }
}
