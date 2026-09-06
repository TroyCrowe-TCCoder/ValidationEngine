namespace ValidationEngine.Infrastructure;

/// <summary>
/// Describes a standalone validation-tool executable to launch as an independent OS process
/// (see <see cref="ExternalToolRunner"/> and VAL-011 / AGT-009).
/// </summary>
public sealed record ExternalToolInvocation
{
    /// <summary>
    /// Default timeout applied when <see cref="TimeoutSeconds"/> is not overridden. Pre-commit
    /// hooks must stay fast, so a hung sibling process (e.g. a stalled external HTTP link check)
    /// is killed rather than allowed to block a commit indefinitely.
    /// </summary>
    public const int DefaultTimeoutSeconds = 30;

    public required string ToolName { get; init; }

    public required string ExecutablePath { get; init; }

    public string Arguments { get; init; } = string.Empty;

    public string? WorkingDirectory { get; init; }

    /// <summary>
    /// Maximum time to wait for this tool before it is killed and reported as a timed-out
    /// failure. Keep this tight for pre-commit-invoked tools; a slower, exhaustive pass belongs
    /// in <see cref="ValidationEngine.Infrastructure.ValidationRunMode.Manual"/>/CI, not the hook.
    /// </summary>
    public int TimeoutSeconds { get; init; } = DefaultTimeoutSeconds;
}
