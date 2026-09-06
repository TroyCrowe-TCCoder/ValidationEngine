using System.Text.Json;

namespace ValidationEngine.Infrastructure;

/// <summary>
/// Reads the optional <c>validationengine.config.json</c> file from a repository root. This is
/// the single configuration surface a consuming repository uses to tell the engine where its
/// standards content lives and what name to report itself as, so nothing in the engine or its
/// sibling tools needs to hardcode a specific repository name or path.
///
/// Configuration lives at <c>validationengine.config.json</c> in the repository root. Absence of
/// the file means no overrides are configured — every value falls back to its own explicit
/// resolution rule (CLI argument, environment variable, or an explicit error), never to a
/// hardcoded literal.
/// </summary>
public static class ValidationEngineConfigReader
{
    private const string ConfigFileName = "validationengine.config.json";

    public sealed record ValidationEngineConfig
    {
        /// <summary>
        /// Absolute or repository-root-relative path to the directory containing this
        /// repository's standards source (expected to contain a <c>Docs/Standards</c>
        /// subfolder). Overrides the <c>-GlobalStandardsRoot</c> CLI argument's default when set.
        /// </summary>
        public string? StandardsPath { get; init; }

        /// <summary>
        /// The name this repository/solution should be reported as (used, for example, as the
        /// sibling-tool <c>--app</c> label). Overrides auto-detection when set.
        /// </summary>
        public string? SolutionName { get; init; }
    }

    public static ValidationEngineConfig? Read(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);

        var configFilePath = Path.Combine(repositoryRoot, ConfigFileName);
        if (!File.Exists(configFilePath))
        {
            return null;
        }

        var jsonContent = File.ReadAllText(configFilePath);
        return JsonSerializer.Deserialize<ValidationEngineConfig>(jsonContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    /// <summary>
    /// Resolves <see cref="ValidationEngineConfig.StandardsPath"/> to an absolute path relative
    /// to <paramref name="repositoryRoot"/> when it is a relative path.
    /// </summary>
    public static string? ResolveStandardsPath(string repositoryRoot, ValidationEngineConfig? config)
    {
        if (string.IsNullOrWhiteSpace(config?.StandardsPath))
        {
            return null;
        }

        return Path.IsPathRooted(config.StandardsPath)
            ? config.StandardsPath
            : Path.Combine(repositoryRoot, config.StandardsPath);
    }
}
