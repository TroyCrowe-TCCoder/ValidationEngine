namespace ValidationEngine.Agent;

/// <summary>
/// Base contract shared by every purpose-scoped agent client (AGT-001).
/// The orchestrator never depends on a specific model or vendor SDK directly —
/// it only ever depends on <see cref="IAgentClient"/> derivatives.
/// </summary>
public interface IAgentClient
{
    string Purpose { get; }

    /// <summary>
    /// The configured provider name (<see cref="Agent.Configuration.ModelProviderOptions.Name"/>),
    /// e.g. "azure-openai-gpt-5.5". Distinct from <see cref="Purpose"/>, which only identifies the
    /// role the client serves (Validation/Fix). Used so findings stay auditable when
    /// multiple providers share the same purpose (AGT-005/AGT-006).
    /// </summary>
    string Name { get; }
}
