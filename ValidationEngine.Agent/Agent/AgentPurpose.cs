namespace ValidationEngine.Agent;

/// <summary>
/// String constants identifying the purpose a given agent client/provider serves.
/// See Working/ValidationAgentBacklog.md (AGT-001/AGT-002) — do not introduce ad-hoc
/// purpose strings elsewhere; add new constants here as new purposes are implemented.
/// </summary>
public static class AgentPurpose
{
    public const string Validation = "Validation";
    public const string Explain = "Explain"; // AGT-012
}
