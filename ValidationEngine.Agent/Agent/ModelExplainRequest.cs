namespace ValidationEngine.Agent;

/// <summary>
/// Request payload for the Explain purpose (AGT-012). Carries the same rule identity as
/// <see cref="ModelValidationRequest"/> plus the already-determined violation message, so the
/// explain client is not responsible for re-deciding compliance — only for narrating it.
/// </summary>
public sealed record ModelExplainRequest(
    string RuleId,
    string RuleText,
    string FilePath,
    string ViolationMessage);
