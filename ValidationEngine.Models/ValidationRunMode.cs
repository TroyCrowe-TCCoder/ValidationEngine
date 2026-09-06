namespace ValidationEngine.Infrastructure;

/// <summary>
/// Determines how the validation engine resolves its change set and whether hook-oriented
/// gates (such as the branch guard) apply.
/// </summary>
public enum ValidationRunMode
{
    /// <summary>
    /// A developer/agent-initiated audit of the entire solution's tracked files. No parameters
    /// required; the branch guard does not apply since this is not a pre-commit gate.
    /// </summary>
    Manual,

    /// <summary>
    /// A hook/CI-invoked check of only the staged change set. The branch guard applies.
    /// </summary>
    System
}
