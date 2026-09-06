namespace ValidationEngine.Infrastructure;

/// <summary>
/// Builds the <see cref="ExternalToolInvocation"/> used to launch <c>ValidationEngine.Agent</c> as
/// a sibling standalone process (see <see cref="ExternalToolRunner"/>). Extracted from
/// <c>ValidationEngine.Program</c> so the path-resolution and argument-building logic can be
/// unit tested without depending on the running process's actual <see cref="AppContext.BaseDirectory"/>.
/// </summary>
public static class ValidationEngineAgentInvocationFactory
{
    private const string ToolName = "ValidationEngine.Agent";

    // Per-tool timeout override surface (bug #7 in ValidationSolutionGapAnalysis.md \u2014 hardcoded
    // sibling-tool timeouts with no override). Named per tool so a buyer with multiple slow
    // sibling tools (e.g. Agent model latency vs. link-check network latency) can tune each
    // independently without recompiling.
    private const string TimeoutSecondsEnvironmentVariableName = "VALIDATIONENGINE_AGENT_TIMEOUT_SECONDS";

    /// <summary>
    /// Resolves the sibling <c>ValidationEngine.Agent</c> executable relative to this process's own
    /// build output (configuration/TFM), mirroring how it was built, and returns the invocation to
    /// launch it. Returns an empty list if the sibling executable isn't built yet, so
    /// <c>ValidationEngine</c> never fails or blocks on a missing sibling tool.
    /// </summary>
    public static IReadOnlyList<ExternalToolInvocation> Build(
        string repositoryRoot, string globalStandardsRoot, ValidationRunMode mode, string? targetBranch, string ownBinDirectory)
    {
        var agentEnginePath = ResolveAgentEnginePath(ownBinDirectory);

        if (!File.Exists(agentEnginePath))
        {
            return [];
        }

        return
        [
            new ExternalToolInvocation
            {
                ToolName = ToolName,
                ExecutablePath = agentEnginePath,
                Arguments = BuildArguments(repositoryRoot, globalStandardsRoot, mode, targetBranch),
                WorkingDirectory = repositoryRoot,
                TimeoutSeconds = ResolveTimeoutSeconds()
            }
        ];
    }

    /// <summary>
    /// Resolves this tool's timeout from <c>VALIDATIONENGINE_AGENT_TIMEOUT_SECONDS</c> if set to a
    /// valid positive integer, otherwise falls back to <see cref="ExternalToolInvocation.DefaultTimeoutSeconds"/>.
    /// </summary>
    private static int ResolveTimeoutSeconds()
    {
        var rawValue = Environment.GetEnvironmentVariable(TimeoutSecondsEnvironmentVariableName);
        return int.TryParse(rawValue, out var parsedTimeoutSeconds) && parsedTimeoutSeconds > 0
            ? parsedTimeoutSeconds
            : ExternalToolInvocation.DefaultTimeoutSeconds;
    }

    /// <summary>
    /// Resolves the expected path to the sibling <c>ValidationEngine.Agent</c> executable given this
    /// process's own bin output directory, without checking for file existence. Tries the
    /// packaged/distributed layout first \u2014 the agent executable sitting flat alongside this one
    /// (e.g. both tools copied into a single <c>tools/</c> folder for distribution) \u2014 falling back
    /// to the in-repo sibling-project bin layout (<c>&lt;solutionRoot&gt;/ValidationEngine.Agent/bin/&lt;Config&gt;/&lt;TFM&gt;/...</c>)
    /// used when building and running directly from source.
    /// </summary>
    public static string ResolveAgentEnginePath(string ownBinDirectory)
    {
        var trimmedBinDirectory = ownBinDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var executableName = OperatingSystem.IsWindows() ? "ValidationEngine.Agent.exe" : "ValidationEngine.Agent";

        var packagedPath = Path.Combine(trimmedBinDirectory, executableName);
        if (File.Exists(packagedPath))
        {
            return packagedPath;
        }

        var tfmDirectory = Path.GetFileName(trimmedBinDirectory);
        var configurationDirectory = Path.GetFileName(Path.GetDirectoryName(trimmedBinDirectory) ?? string.Empty);
        var solutionRoot = Path.GetFullPath(Path.Combine(trimmedBinDirectory, "..", "..", "..", ".."));

        return Path.Combine(solutionRoot, "ValidationEngine.Agent", "bin", configurationDirectory, tfmDirectory, executableName);
    }

    /// <summary>
    /// Builds the CLI argument string passed to the sibling <c>ValidationEngine.Agent</c> process.
    /// </summary>
    public static string BuildArguments(string repositoryRoot, string globalStandardsRoot, ValidationRunMode mode, string? targetBranch)
    {
        var argumentsBuilder = $"-RepositoryRoot \"{repositoryRoot}\" -GlobalStandardsRoot \"{globalStandardsRoot}\" -Mode {mode}";
        if (!string.IsNullOrWhiteSpace(targetBranch))
        {
            argumentsBuilder += $" -TargetBranch \"{targetBranch}\"";
        }

        return argumentsBuilder;
    }
}
