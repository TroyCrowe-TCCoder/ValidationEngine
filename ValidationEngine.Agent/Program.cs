using ValidationEngine.Agent.Rules;
using ValidationEngine.Agent;
using ValidationEngine.Infrastructure;
using ValidationEngine.Models;
using ValidationEngine.Reporting;
using ValidationEngine.Rules;

var repositoryRoot = Path.GetFullPath(ParseArgument(args, "-RepositoryRoot") ?? Directory.GetCurrentDirectory());

var findings = new List<RuleFinding>();
var engineErrors = new List<EngineError>();

try
{
    var changedFilesArgument = ParseArgument(args, "--changed-files");
    var rulesArgument = ParseArgument(args, "--rules");

    // ValidationEngine.Agent is a pure executor: it validates only the rules it is handed and
    // never resolves its own changeset, applicable standards, or coverage matrices. Both
    // "--changed-files" and "--rules" are fuel supplied by ValidationOrchestrator when it
    // launches this tool as a sibling process (see ExternalToolFuel / ExternalToolRunner).
    // There is no independent resolution path — if the required rule-set fuel is missing (e.g.
    // this executable was run directly rather than via ValidationEngine), report that plainly
    // as a manual review item instead of guessing at scope or throwing.
    if (string.IsNullOrWhiteSpace(rulesArgument))
    {
        findings.Add(new RuleFinding
        {
            RuleId = "AGT-000",
            StandardFile = "ValidationEngine.Agent",
            Severity = ViolationSeverity.ManualReviewItem,
            Priority = RulePriority.ManualOnlyComment,
            Message = "No rule-set fuel (--rules) was supplied. ValidationEngine.Agent only validates rules it is handed by ValidationEngine and cannot resolve its own scope — run validation via ValidationEngine (e.g. the pre-commit hook or a Manual audit) instead of invoking this executable directly."
        });
    }
    else
    {
        IReadOnlyList<FileChange> changes = string.IsNullOrWhiteSpace(changedFilesArgument)
            ? []
            : changedFilesArgument
                .Split(';', StringSplitOptions.RemoveEmptyEntries)
                .Select(path => new FileChange { Status = FileChangeStatus.Modified, Path = path })
                .ToList();

        var manualOnlyRules = ParseRules(rulesArgument);

        var agentClientFactory = AgentClientFactoryBuilder.Build(repositoryRoot);
        var agentClients = agentClientFactory?.CreateActiveValidationClients() ?? [];
        var explainClients = agentClientFactory?.CreateActiveExplainClients() ?? [];

        // AGT-001: manual-only rules were handed to this run but no active Validation-purpose
        // provider is configured (no "ValidationAgent" section, or every entry has IsActive
        // false / an unsupported Type). Without this explicit signal, every manual-only rule
        // would silently go unevaluated and the run would report clean, which is
        // indistinguishable from "everything passed" \u2014 report it plainly instead.
        if (agentClients.Count == 0 && manualOnlyRules.Count > 0)
        {
            findings.Add(new RuleFinding
            {
                RuleId = "AGT-001",
                StandardFile = "ValidationEngine.Agent",
                Severity = ViolationSeverity.ManualReviewItem,
                Priority = RulePriority.ManualOnlyComment,
                Message = $"{manualOnlyRules.Count} manual-only rule(s) were supplied but no active Validation-purpose AI provider is configured. These rules were NOT evaluated \u2014 configure an active provider in the \"ValidationAgent\" appsettings.json section to enable AI-assisted review, or treat these rules as requiring separate manual review."
            });
        }

        if (agentClients.Count > 0 && manualOnlyRules.Count > 0)
        {
            var applicabilityLookup = RuleFileApplicability.BuildApplicabilityLookup(
                manualOnlyRules, changes, change => change.Path);

            foreach (var rule in manualOnlyRules)
            {
                foreach (var change in applicabilityLookup[rule])
                {
                    var request = BuildRequest(rule, change, repositoryRoot);
                    var responses = await Task.WhenAll(
                        agentClients.Select(client => client.EvaluateAsync(request, CancellationToken.None)));

                    var reconciledFinding = ReconcileResponses(rule, change, agentClients, responses);
                    if (reconciledFinding is not null)
                    {
                        if (explainClients.Count > 0)
                        {
                            reconciledFinding = await AttachRationaleAsync(reconciledFinding, rule, explainClients);
                        }

                        findings.Add(reconciledFinding);
                    }
                }
            }
        }
    }
}
catch (IOException ioException)
{
    engineErrors.Add(new EngineError { Source = "ValidationEngine.Agent", Message = ioException.Message });
}
catch (InvalidDataException invalidDataException)
{
    engineErrors.Add(new EngineError { Source = "ValidationEngine.Agent", Message = invalidDataException.Message });
}
catch (InvalidOperationException invalidOperationException)
{
    engineErrors.Add(new EngineError { Source = "ValidationEngine.Agent", Message = invalidOperationException.Message });
}

var report = new ValidationReport { Findings = findings, EngineErrors = engineErrors };

var discrepanciesFilePath = Path.Combine(repositoryRoot, "Working", "AgentValidationDiscrepancies.md");
var discrepanciesJsonFilePath = Path.Combine(repositoryRoot, "Working", "AgentValidationDiscrepancies.json");

var exitCode = report.HasEngineErrors ? 2 : (report.HasViolations ? 1 : 0);

if (report.HasEngineErrors || report.HasViolations || report.Findings.Any(finding => finding.Severity == ViolationSeverity.ManualReviewItem))
{
    var renderedReport = MarkdownReportRenderer.Render(report);
    var renderedJsonReport = JsonReportRenderer.Render(report, exitCode);

    Directory.CreateDirectory(Path.Combine(repositoryRoot, "Working"));
    File.WriteAllText(discrepanciesFilePath, renderedReport);
    File.WriteAllText(discrepanciesJsonFilePath, renderedJsonReport);
    Console.WriteLine(renderedReport);
}
else
{
    if (File.Exists(discrepanciesFilePath))
    {
        File.Delete(discrepanciesFilePath);
    }

    if (File.Exists(discrepanciesJsonFilePath))
    {
        File.Delete(discrepanciesJsonFilePath);
    }
}

return exitCode;

static IReadOnlyList<ManualOnlyRuleEntry> ParseRules(string rulesArgument)
{
    return [.. rulesArgument
        .Split(";;", StringSplitOptions.RemoveEmptyEntries)
        .Select(serializedRule =>
        {
            var parts = serializedRule.Split('|');
            return new ManualOnlyRuleEntry
            {
                RuleId = parts[0],
                RuleSummary = parts[1],
                StandardFile = parts[2],
                Technique = parts[3],
                Priority = Enum.Parse<RulePriority>(parts[4], ignoreCase: true)
            };
        })];
}

static string? ParseArgument(string[] commandLineArgs, string argumentName)
{
    for (var index = 0; index < commandLineArgs.Length - 1; index++)
    {
        if (string.Equals(commandLineArgs[index], argumentName, StringComparison.OrdinalIgnoreCase))
        {
            return commandLineArgs[index + 1];
        }
    }

    return null;
}

static ModelValidationRequest BuildRequest(ManualOnlyRuleEntry rule, FileChange change, string repositoryRoot)
{
    var absoluteFilePath = Path.Combine(repositoryRoot, change.Path);
    var fileContent = File.Exists(absoluteFilePath) ? File.ReadAllText(absoluteFilePath) : string.Empty;

    return new ModelValidationRequest(
        RuleId: rule.RuleId,
        RuleText: rule.RuleSummary,
        FileContent: fileContent,
        FilePath: change.Path);
}

static async Task<RuleFinding> AttachRationaleAsync(
    RuleFinding finding,
    ManualOnlyRuleEntry rule,
    IReadOnlyList<IExplainAgentClient> explainClients)
{
    // AGT-012: Explain never re-decides compliance — only the first active Explain client's
    // rationale is attached, since narration doesn't need AGT-006-style reconciliation.
    var explainClient = explainClients[0];

    var request = new ModelExplainRequest(
        RuleId: rule.RuleId,
        RuleText: rule.RuleSummary,
        FilePath: finding.FilePath ?? string.Empty,
        ViolationMessage: finding.Message);

    var response = await explainClient.ExplainAsync(request, CancellationToken.None);

    return finding with { Rationale = response.Rationale };
}

static RuleFinding? ReconcileResponses(
    ManualOnlyRuleEntry rule,
    FileChange change,
    IReadOnlyList<IValidationAgentClient> clients,
    IReadOnlyList<ModelValidationResponse> responses)
{
    // AGT-006 reconciliation: unanimous Pass -> no finding; any Violation -> Violation; disagreement -> Uncertain (ManualReviewItem).
    if (responses.All(r => r.Outcome == ValidationOutcome.Pass))
    {
        return null;
    }

    var severity = responses.Any(r => r.Outcome == ValidationOutcome.Violation)
        ? ViolationSeverity.Violation
        : ViolationSeverity.ManualReviewItem;

    return new RuleFinding
    {
        RuleId = rule.RuleId,
        StandardFile = rule.StandardFile,
        Severity = severity,
        Priority = rule.Priority,
        Message = string.Join("; ", responses.Select(r => r.Explanation).Where(e => !string.IsNullOrWhiteSpace(e))),
        FilePath = change.Path,
        Source = string.Join(", ", clients.Select(c => c.Name))
    };
}
