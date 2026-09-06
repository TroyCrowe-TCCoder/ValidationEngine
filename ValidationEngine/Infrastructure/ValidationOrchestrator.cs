using ValidationEngine.Models;
using ValidationEngine.Resolver;
using ValidationEngine.Rules;

namespace ValidationEngine.Infrastructure;

public sealed class ValidationOrchestrator
{
    private static readonly IReadOnlyList<string> InScopeStandardsFileNames =
    [
        "GlobalFileSpecificationStandards.md",
        "GlobalRepositoryStandards.md",
        "GlobalSolutionStructureStandards.md"
    ];

    private readonly IReadOnlyList<ExternalToolInvocation> _externalToolInvocations;

    public ValidationOrchestrator(IReadOnlyList<ExternalToolInvocation>? externalToolInvocations = null)
    {
        _externalToolInvocations = externalToolInvocations ?? [];
    }

    public Task<ValidationReport> RunAsync(string repositoryRoot, string globalStandardsRoot, ValidationRunMode mode, string? targetBranch = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(globalStandardsRoot);

        return RunCoreAsync(repositoryRoot, globalStandardsRoot, mode, targetBranch, cancellationToken);
    }

    private async Task<ValidationReport> RunCoreAsync(string repositoryRoot, string globalStandardsRoot, ValidationRunMode mode, string? targetBranch, CancellationToken cancellationToken)
    {
        var findings = new List<RuleFinding>();
        var engineErrors = new List<EngineError>();

        try
        {
            var standardsDirectory = Path.Combine(globalStandardsRoot, "Docs", "Standards");

            // Step 1: Meta-check — a stale checklist invalidates everything downstream.
            var metaCheckFindings = ChecklistMetaCheckRule.Validate(standardsDirectory, InScopeStandardsFileNames);
            if (metaCheckFindings.Count > 0)
            {
                findings.AddRange(metaCheckFindings);
                return new ValidationReport { Findings = findings, EngineErrors = engineErrors };
            }

            // Step 2: Branch guard — block direct commits to dev or main. Only applies to
            // System (hook/CI) runs; a Manual audit must be runnable on any branch, including
            // dev/main, without being treated as an attempted direct commit.
            if (mode == ValidationRunMode.System)
            {
                var currentBranch = GitChangeResolver.GetCurrentBranch(repositoryRoot);
                if (currentBranch is "dev" or "main")
                {
                    engineErrors.Add(new EngineError { Source = nameof(ValidationOrchestrator), Message = "Feature branch required, cannot check directly into Dev or Main" });
                    return new ValidationReport { Findings = findings, EngineErrors = engineErrors };
                }
            }

            // Step 3: Resolve the change set.
            // - System mode with a target branch (pipeline/PR-gate runs): diffs the merge-base
            //   of targetBranch against HEAD, capturing every commit made on this branch so far
            //   — i.e., everything that will actually be merged. A CI checkout has no staged
            //   index to inspect (the index always matches HEAD right after checkout), so this
            //   is the only meaningful change set in that context.
            // - System mode without a target branch (local pre-commit hook): checks only the
            //   staged diff, for fast, incremental, per-commit feedback.
            // - Manual mode: audits every tracked file so a developer/agent can see the
            //   solution's full current standards-compliance state.
            var changes = mode == ValidationRunMode.System
                ? (string.IsNullOrWhiteSpace(targetBranch)
                    ? GitChangeResolver.GetStagedChanges(repositoryRoot)
                    : GitChangeResolver.GetChangesAgainstTargetBranch(repositoryRoot, targetBranch))
                : GitChangeResolver.GetAllTrackedFiles(repositoryRoot);

            if (mode == ValidationRunMode.System && changes.Count == 0)
            {
                var guidance = string.IsNullOrWhiteSpace(targetBranch)
                    ? "No staged changes found. Stage changes before running a System validation, or run with no arguments for a Manual full-solution audit."
                    : $"No changes found between '{targetBranch}' and HEAD. Nothing to validate for this branch.";
                engineErrors.Add(new EngineError { Source = nameof(ValidationOrchestrator), Message = guidance });
                return new ValidationReport { Findings = findings, EngineErrors = engineErrors };
            }

            // Step 4: Load repository-local skip/exclusion addendum.
            var addendumReadResult = ExclusionAddendumReader.Read(repositoryRoot);
            foreach (var expiredExclusion in addendumReadResult.ExpiredExclusions)
            {
                findings.Add(new RuleFinding
                {
                    RuleId = expiredExclusion.MarkerId,
                    StandardFile = expiredExclusion.AddendumFileName,
                    Severity = ViolationSeverity.Violation,
                    Priority = RulePriority.HardStop,
                    Message = "Skip/exclusion past its Planned Review Date — exclusion must be renewed or removed"
                });
            }

            // Step 5: Resolve applicable standards for this repository's AppType. This is the
            // only file/standards-relevance filter that exists \u2014 there is no separate
            // file-level exclusion list; the repo-local addendum below (Step 4) excludes rule
            // markers, not files.
            var repositoryProfile = AppTypeDetector.Detect(repositoryRoot);
            var applicabilityMatrixPath = Path.Combine(standardsDirectory, "StandardsApplicabilityMatrix.csv");
            var applicabilityEntries = ApplicabilityMatrixReader.Read(applicabilityMatrixPath);
            var applicableStandards = ApplicableStandardsResolver.Resolve(repositoryProfile, applicabilityEntries);

            // Step 5a: Apply the optional, solution-wide blanket per-file exclusion list
            // (Standards/FileExclusions.json). This is a file-level exclusion \u2014 distinct from
            // the AppType relevance resolver (whole-standards-file granularity, Step 5) and the
            // rule-marker exclusion addendum (Step 4) \u2014 and is applied here, before the base
            // ValidationScope is built, so every downstream tool is shielded uniformly without
            // needing its own file-exclusion logic.
            var fileExclusionPatterns = FileExclusionListReader.Read(repositoryRoot);
            var scopedChanges = fileExclusionPatterns.Count == 0
                ? changes
                : [.. changes.Where(change => !FileExclusionListReader.IsExcluded(change.Path, fileExclusionPatterns))];

            // Step 5b: The single base in-scope set every tool derives its own subset from (see
            // ValidationScope / ValidationScopeDeriver) \u2014 the resolved changeset (Step 3), with
            // Step 5a's file-level exclusions already applied, plus the AppType-resolved
            // applicable standards (Step 5). Rule-marker exclusions (Step 4, optional \u2014 no
            // addendum means nothing is excluded) are applied per-tool via
            // ValidationScopeDeriver.DeriveRules, not against this base set directly.
            var validationScope = new ValidationScope
            {
                Changes = scopedChanges,
                ApplicableStandards = applicableStandards
            };

            var runContext = new RunContext
            {
                RepositoryName = ResolveRepositoryName(repositoryRoot),
                RepositoryRoot = Path.GetFullPath(repositoryRoot),
                Mode = mode,
                TargetBranch = targetBranch,
                DetectedAppType = repositoryProfile.AppType.ToString(),
                ApplicableStandards = applicableStandards,
                ChangesEvaluated = validationScope.Changes.Count
            };

            // Step 5c: Resolve the AI-agent's manual-only/missing rule set once here, from the
            // same coverage matrices + base scope every other rule set is derived from. This is
            // owned by ValidationOrchestrator, not the ValidationEngine.Agent sibling tool — the
            // sibling is a pure executor and is only ever fed this already-resolved rule set as
            // fuel; it has no independent path to read coverage matrices or resolve standards
            // itself.
            var coverageMatricesDirectory = Path.Combine(AppContext.BaseDirectory, "CoverageMatrices");
            var candidateManualOnlyRules = CoverageMatrixReader.ReadManualOnlyAndMissingRules(
                coverageMatricesDirectory, validationScope.ApplicableStandards);
            var manualOnlyRules = ValidationScopeDeriver.DeriveRules(
                validationScope, candidateManualOnlyRules, rule => rule.RuleId, addendumReadResult.ExcludedMarkers);

            // Step 6: Execute mechanical rule checks against this tool's own derived subset of
            // the base in-scope set (mechanical rules need every file, including deletions, so
            // no predicate narrows it further \u2014 but it is still requested through the same
            // ValidationScopeDeriver mechanism every other tool uses, not the base set directly).
            var mechanicalFiles = ValidationScopeDeriver.DeriveFiles(validationScope);
            var ruleFindings = MechanicalValidationRules.Validate(mechanicalFiles, repositoryRoot, addendumReadResult.ExcludedMarkers);
            findings.AddRange(ruleFindings);

            // Step 7: Run any configured sibling standalone engines. Every sibling tool (e.g.
            // ValidationEngine.Agent, and a future LinkValidationEngine) is a pure executor: it
            // receives its metadata (changeset fuel) and its already-resolved rule-set
            // collection (manualOnlyRules, Step 5c) as fuel, and validates only those rules — it
            // has no independent path to resolve its own changeset, applicable standards, or
            // rule set. In Manual mode the derived set is the full tracked-file set, not a
            // changeset, so no changeset fuel is passed. No invocations are configured yet (no
            // sibling standalone engine exists), so this call is currently a no-op.
            var externalToolFiles = ValidationScopeDeriver.DeriveFiles(validationScope);
            var changesetFuel = mode == ValidationRunMode.System ? externalToolFiles : null;
            var externalToolResults = await ExternalToolRunner.RunAllAsync(_externalToolInvocations, changesetFuel, manualOnlyRules, cancellationToken);
            foreach (var externalToolResult in externalToolResults.Where(result => !result.Succeeded))
            {
                engineErrors.Add(new EngineError { Source = externalToolResult.ToolName, Message = externalToolResult.StandardError });
            }

            return new ValidationReport { Findings = findings, EngineErrors = engineErrors, RunContext = runContext };
        }
        catch (IOException ioException)
        {
            engineErrors.Add(new EngineError { Source = nameof(ValidationOrchestrator), Message = ioException.Message });
        }
        catch (InvalidDataException invalidDataException)
        {
            engineErrors.Add(new EngineError { Source = nameof(ValidationOrchestrator), Message = invalidDataException.Message });
        }
        catch (InvalidOperationException invalidOperationException)
        {
            engineErrors.Add(new EngineError { Source = nameof(ValidationOrchestrator), Message = invalidOperationException.Message });
        }

        return new ValidationReport
        {
            Findings = findings,
            EngineErrors = engineErrors
        };
    }

    private static string ResolveRepositoryName(string repositoryRoot)
    {
        var remoteUrl = GitChangeResolver.TryGetRemoteUrl(repositoryRoot, "origin");
        if (!string.IsNullOrWhiteSpace(remoteUrl))
        {
            var trimmed = remoteUrl.TrimEnd('/');
            if (trimmed.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed[..^".git".Length];
            }

            var lastSegment = trimmed.Split('/', '\\').LastOrDefault(segment => !string.IsNullOrWhiteSpace(segment));
            if (!string.IsNullOrWhiteSpace(lastSegment))
            {
                return lastSegment;
            }
        }

        return new DirectoryInfo(Path.GetFullPath(repositoryRoot)).Name;
    }
}

