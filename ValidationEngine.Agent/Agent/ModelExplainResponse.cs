namespace ValidationEngine.Agent;

/// <summary>
/// Response payload for the Explain purpose (AGT-012).
/// </summary>
public sealed record ModelExplainResponse(
    string Rationale,
    string? DocSnippet);
