namespace ValidationEngine.Agent;

/// <summary>
/// Purpose-scoped contract for AI-assisted rationale/explanation generation (AGT-012).
/// Given a rule and an existing finding, produces a human-readable rationale (and
/// optionally a doc snippet) for richer report context/onboarding. Does not participate
/// in the Pass/Violation/Uncertain decision — that remains the sole responsibility of
/// <see cref="IValidationAgentClient"/>.
/// </summary>
public interface IExplainAgentClient : IAgentClient
{
    Task<ModelExplainResponse> ExplainAsync(ModelExplainRequest request, CancellationToken ct);
}
