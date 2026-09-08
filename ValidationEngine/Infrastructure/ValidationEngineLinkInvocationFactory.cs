namespace ValidationEngine.Infrastructure;

/// <summary>
/// Builds the <see cref="ExternalToolInvocation"/> used to launch <c>ValidationEngine.Link</c> as
/// a sibling standalone process (see <see cref="ExternalToolRunner"/>). Extracted from
/// <c>ValidationEngine.Program</c> so the path-resolution and argument-building logic can be
/// unit tested without depending on the running process's actual <see cref="AppContext.BaseDirectory"/>.
/// </summary>
public static class ValidationEngineLinkInvocationFactory
{
    private const string ToolName = "ValidationEngine.Link";

    // Per-tool timeout override surface, mirroring VALIDATIONENGINE_AGENT_TIMEOUT_SECONDS. Named
    // per tool so a buyer with multiple slow sibling tools (e.g. Agent model latency vs.
    // link-check network latency) can tune each independently without recompiling.
    private const string TimeoutSecondsEnvironmentVariableName = "VALIDATIONENGINE_LINK_TIMEOUT_SECONDS";

    /// <summary>
    /// Resolves the sibling <c>ValidationEngine.Link</c> executable relative to this process's own
    /// build output (configuration/TFM), mirroring how it was built, and returns the invocation to
    /// launch it. Returns an empty list if the sibling executable isn't built yet, so
    /// <c>ValidationEngine</c> never fails or blocks on a missing sibling tool.
    /// </summary>
    public static IReadOnlyList<ExternalToolInvocation> Build(
        string repositoryRoot, ValidationRunMode mode, string? targetBranch, string ownBinDirectory)
    {
        var linkEnginePath = ResolveLinkEnginePath(ownBinDirectory);

        if (!File.Exists(linkEnginePath))
        {
            return [];
        }

        return
        [
            new ExternalToolInvocation
            {
                ToolName = ToolName,
                ExecutablePath = linkEnginePath,
                Arguments = BuildArguments(repositoryRoot, mode, targetBranch),
                WorkingDirectory = repositoryRoot,
                TimeoutSeconds = ResolveTimeoutSeconds()
            }
        ];
    }

    /// <summary>
    /// Resolves this tool's timeout from <c>VALIDATIONENGINE_LINK_TIMEOUT_SECONDS</c> if set to a
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
    /// Resolves the expected path to the sibling <c>ValidationEngine.Link</c> executable given this
    /// process's own bin output directory, without checking for file existence. Tries the
    /// packaged/distributed layout first — the link executable sitting flat alongside this one
    /// (e.g. both tools copied into a single <c>tools/</c> folder for distribution) — falling back
    /// to the in-repo sibling-project bin layout (<c>&lt;solutionRoot&gt;/ValidationEngine.Link/bin/&lt;Config&gt;/&lt;TFM&gt;/...</c>)
    /// used when building and running directly from source.
    /// </summary>
    public static string ResolveLinkEnginePath(string ownBinDirectory)
    {
        var trimmedBinDirectory = ownBinDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var executableName = OperatingSystem.IsWindows() ? "ValidationEngine.Link.exe" : "ValidationEngine.Link";

        var packagedPath = Path.Combine(trimmedBinDirectory, executableName);
        if (File.Exists(packagedPath))
        {
            return packagedPath;
        }

        var tfmDirectory = Path.GetFileName(trimmedBinDirectory);
        var configurationDirectory = Path.GetFileName(Path.GetDirectoryName(trimmedBinDirectory) ?? string.Empty);
        var solutionRoot = Path.GetFullPath(Path.Combine(trimmedBinDirectory, "..", "..", "..", ".."));

        return Path.Combine(solutionRoot, "ValidationEngine.Link", "bin", configurationDirectory, tfmDirectory, executableName);
    }

    /// <summary>
    /// Builds the CLI argument string passed to the sibling <c>ValidationEngine.Link</c> process.
    /// Manual runs audit every tracked Markdown file (<c>--full-audit</c>); System runs scope to
    /// files changed against <paramref name="targetBranch"/>, mirroring how ValidationEngine
    /// itself resolves its changeset for each mode.
    /// </summary>
    public static string BuildArguments(string repositoryRoot, ValidationRunMode mode, string? targetBranch)
    {
        var argumentsBuilder = $"-RepositoryRoot \"{repositoryRoot}\"";

        if (mode == ValidationRunMode.Manual)
        {
            argumentsBuilder += " --full-audit";
        }
        else if (!string.IsNullOrWhiteSpace(targetBranch))
        {
            argumentsBuilder += $" --base-branch \"{targetBranch}\"";
        }

        return argumentsBuilder;
    }
}
