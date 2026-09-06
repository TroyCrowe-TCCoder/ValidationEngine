namespace ValidationEngine.Agent;

/// <summary>
/// Purpose-scoped contract for AI-assisted rule validation (AGT-001).
/// </summary>
public interface IValidationAgentClient : IAgentClient
{
    Task<ModelValidationResponse> EvaluateAsync(ModelValidationRequest request, CancellationToken ct);
}
