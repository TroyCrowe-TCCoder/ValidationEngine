namespace ValidationEngine.Agent.Configuration;

/// <summary>
/// Array-based, multi-active provider configuration (AGT-002).
/// Bound from the "ValidationAgent" appsettings.json section. There is intentionally
/// no top-level "ActiveProvider" selector — activation is per-entry via <see cref="ModelProviderOptions.IsActive"/>,
/// so zero, one, or many providers (even across multiple purposes) can be active simultaneously.
/// Secrets (API keys) are NOT stored here — resolve via Key Vault/user-secrets/env var keyed by Name.
/// </summary>
public sealed class ValidationAgentOptions
{
    public const string SectionName = "ValidationAgent";

    public List<ModelProviderOptions> Providers { get; init; } = [];

    public IReadOnlyList<ModelProviderOptions> GetActiveByPurpose(string purpose) =>
        Providers.Where(p => p.IsActive &&
            string.Equals(p.Purpose, purpose, StringComparison.OrdinalIgnoreCase))
            .ToList();
}
