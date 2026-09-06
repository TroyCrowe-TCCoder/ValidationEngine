using System.Diagnostics;

namespace ValidationEngine.Infrastructure;

public sealed class GitChangeResolver
{
    public static string GetCurrentBranch(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);

        var output = RunGitCommand(repositoryRoot, "rev-parse --abbrev-ref HEAD");
        return output.Trim();
    }

    /// <summary>
    /// Returns the fetch URL configured for the given remote (e.g., "origin"), or null if no
    /// such remote is configured. Used to identify which repository a validation run targeted.
    /// </summary>
    public static string? TryGetRemoteUrl(string repositoryRoot, string remoteName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(remoteName);

        var output = RunGitCommand(repositoryRoot, $"config --get remote.{remoteName}.url").Trim();
        return string.IsNullOrEmpty(output) ? null : output;
    }

    public static IReadOnlyList<FileChange> GetStagedChanges(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);

        var output = RunGitCommand(repositoryRoot, "diff --cached --name-status");
        return ParseNameStatusOutput(output);
    }

    /// <summary>
    /// Resolves the cumulative diff between a target branch's merge-base and the current HEAD —
    /// i.e., everything that will actually be merged, across every commit made on the current
    /// branch so far. Intended for pipeline/PR-gate runs, where there is no staged index to
    /// inspect (a CI checkout's index always matches HEAD).
    /// </summary>
    public static IReadOnlyList<FileChange> GetChangesAgainstTargetBranch(string repositoryRoot, string targetBranch)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetBranch);

        var mergeBase = RunGitCommand(repositoryRoot, $"merge-base {targetBranch} HEAD").Trim();
        if (string.IsNullOrEmpty(mergeBase))
        {
            throw new InvalidOperationException($"Could not resolve merge-base between '{targetBranch}' and HEAD.");
        }

        var output = RunGitCommand(repositoryRoot, $"diff {mergeBase} HEAD --name-status");
        return ParseNameStatusOutput(output);
    }

    public static IReadOnlyList<FileChange> GetAllTrackedFiles(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);

        var output = RunGitCommand(repositoryRoot, "ls-files");
        var changes = new List<FileChange>();

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            changes.Add(new FileChange { Status = FileChangeStatus.Added, Path = line.Trim() });
        }

        return changes;
    }

    private static List<FileChange> ParseNameStatusOutput(string output)
    {
        var changes = new List<FileChange>();

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var fields = line.Split('\t');
            if (fields.Length < 2)
            {
                continue;
            }

            var statusField = fields[0].Trim();

            if (statusField.StartsWith('R') && fields.Length >= 3)
            {
                changes.Add(new FileChange { Status = FileChangeStatus.Renamed, OldPath = fields[1], Path = fields[2] });
            }
            else if (statusField == "A")
            {
                changes.Add(new FileChange { Status = FileChangeStatus.Added, Path = fields[1] });
            }
            else if (statusField == "M")
            {
                changes.Add(new FileChange { Status = FileChangeStatus.Modified, Path = fields[1] });
            }
            else if (statusField == "D")
            {
                changes.Add(new FileChange { Status = FileChangeStatus.Deleted, Path = fields[1] });
            }
        }

        return changes;
    }

    private static string RunGitCommand(string repositoryRoot, string arguments)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(processStartInfo) ?? throw new InvalidOperationException("Failed to start git process.");
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return output;
    }
}
