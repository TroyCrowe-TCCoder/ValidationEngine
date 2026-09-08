using System.Diagnostics;
using ValidationEngine.Rules;

namespace ValidationEngine.Infrastructure;

/// <summary>
/// Launches sibling, standalone validation-tool executables as independent OS processes and
/// awaits all of them alongside the in-process <see cref="ValidationOrchestrator"/> pass via
/// <see cref="Task.WhenAll{TResult}(IEnumerable{Task{TResult}})"/>. This is shared
/// <c>ValidationEngine</c> infrastructure, not owned by any single backlog item — AGT-*
/// (the AI-agent layer, see <c>Working/ValidationAgentBacklog.md</c>) and VAL-* (tooling
/// categories such as the future <c>LinkValidationEngine</c>, VAL-011, see
/// <c>Working/ValidationToolingRolloutPlan.md</c>) are both just different sets of tooling under
/// the same overall validation engine, so "run sibling standalone tools concurrently" is a
/// cross-cutting concern that lives here rather than being duplicated per backlog. Project
/// separation alone only yields compile-time isolation, so true "run at the same time" behavior
/// is achieved by spawning each tool as its own OS process (true parallelism, fault isolation —
/// a crash/hang in one tool cannot block or take down another) and awaiting their exit
/// asynchronously rather than sequentially.
/// Since these tools are invoked from the pre-commit hook (<c>.githooks/pre-commit</c> →
/// the installed <c>validation-engine</c> global dotnet tool; contributors testing engine changes
/// from source use <c>Scripts/BuildAndRunFromSource.dev.ps1</c> instead), which must stay fast,
/// every invocation is bounded by
/// <see cref="ExternalToolInvocation.TimeoutSeconds"/>: a tool that exceeds its timeout is killed
/// and reported as a failed <see cref="ExternalToolResult"/> rather than allowed to hang the
/// commit indefinitely.
/// </summary>
public static class ExternalToolRunner
{
    /// <summary>
    /// Launches every provided external tool as a separate OS process and awaits them all
    /// concurrently, feeding every invocation the same resolved changeset and rule-set fuel (see
    /// <see cref="ExternalToolFuel"/>) <see cref="ValidationOrchestrator"/> already resolved once
    /// for its own in-process passes — <c>ValidationEngine</c> resolves scope and rule-set
    /// membership exactly once per run and passes them on as fuel, so every sibling tool is a
    /// pure executor that validates only the rules it is handed, with no independent resolution
    /// path of its own. Pass <paramref name="changes"/> as null/empty (e.g. <see cref="ValidationRunMode.Manual"/>)
    /// when the run has no changeset to feed. Each process's stdout/stderr is captured; a tool
    /// that fails to launch (e.g., executable not found) yields an <see cref="ExternalToolResult"/>
    /// with exit code -1 rather than throwing, so one missing/broken tool does not prevent the
    /// others from running.
    /// </summary>
    public static async Task<IReadOnlyList<ExternalToolResult>> RunAllAsync(
        IReadOnlyList<ExternalToolInvocation> invocations,
        IReadOnlyList<FileChange>? changes = null,
        IReadOnlyList<ManualOnlyRuleEntry>? rules = null,
        CancellationToken cancellationToken = default)
    {
        if (invocations.Count == 0)
        {
            return [];
        }

        var changesetArguments = ExternalToolFuel.BuildChangesetArguments(changes);
        var rulesArguments = ExternalToolFuel.BuildRulesArguments(rules ?? []);
        var tasks = invocations.Select(invocation => RunOneAsync(FeedFuel(invocation, changesetArguments, rulesArguments), cancellationToken));
        return await Task.WhenAll(tasks);
    }

    private static ExternalToolInvocation FeedFuel(ExternalToolInvocation invocation, string changesetArguments, string rulesArguments)
    {
        var fuelArguments = string.Join(' ', new[] { changesetArguments, rulesArguments }.Where(argument => !string.IsNullOrEmpty(argument)));
        if (string.IsNullOrEmpty(fuelArguments))
        {
            return invocation;
        }

        var combinedArguments = string.IsNullOrEmpty(invocation.Arguments)
            ? fuelArguments
            : $"{invocation.Arguments} {fuelArguments}";

        return invocation with { Arguments = combinedArguments };
    }

    private static async Task<ExternalToolResult> RunOneAsync(ExternalToolInvocation invocation, CancellationToken cancellationToken)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = invocation.ExecutablePath,
                    Arguments = invocation.Arguments,
                    WorkingDirectory = invocation.WorkingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            var standardOutputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var standardErrorTask = process.StandardError.ReadToEndAsync(cancellationToken);

            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(invocation.TimeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try
            {
                await process.WaitForExitAsync(linkedCts.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                TryKill(process);
                return new ExternalToolResult
                {
                    ToolName = invocation.ToolName,
                    ExitCode = -1,
                    StandardOutput = string.Empty,
                    StandardError = $"Timed out after {invocation.TimeoutSeconds}s and was killed."
                };
            }

            var standardOutput = await standardOutputTask;
            var standardError = await standardErrorTask;

            return new ExternalToolResult
            {
                ToolName = invocation.ToolName,
                ExitCode = process.ExitCode,
                StandardOutput = standardOutput,
                StandardError = standardError
            };
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or IOException)
        {
            return new ExternalToolResult
            {
                ToolName = invocation.ToolName,
                ExitCode = -1,
                StandardOutput = string.Empty,
                StandardError = exception.Message
            };
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
            // Process already exited between the HasExited check and Kill call — ignore.
        }
    }
}
