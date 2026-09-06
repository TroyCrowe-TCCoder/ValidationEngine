namespace ValidationEngine.Agent;

public sealed record ModelValidationRequest(
    string RuleId,
    string RuleText,
    string FileContent,
    string FilePath);
