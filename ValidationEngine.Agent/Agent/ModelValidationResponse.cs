namespace ValidationEngine.Agent;

public sealed record ModelValidationResponse(
    ValidationOutcome Outcome,
    string? Explanation,
    string? SuggestedFinding);
