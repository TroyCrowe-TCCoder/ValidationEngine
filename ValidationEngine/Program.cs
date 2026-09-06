using ValidationEngine.Infrastructure;
using ValidationEngine.Models;
using ValidationEngine.Reporting;

const string ConfigFileName = "validationengine.config.json";
var repositoryRoot = Path.GetFullPath(ParseArgument(args, "-RepositoryRoot") ?? Directory.GetCurrentDirectory());
var engineConfig = ValidationEngineConfigReader.Read(repositoryRoot);
var globalStandardsRoot = ResolveGlobalStandardsRoot(repositoryRoot, args, engineConfig);

if (string.IsNullOrWhiteSpace(globalStandardsRoot) || !Directory.Exists(globalStandardsRoot))
{
    Console.Error.WriteLine($"Standards root could not be resolved (looked for '{globalStandardsRoot}'). Set it via -GlobalStandardsRoot, the GLOBALSTANDARDS_ROOT environment variable, or 'standardsPath' in {ConfigFileName} at the repository root.");
    return 1;
}

var mode = ParseRunMode(args);
var targetBranch = ParseArgument(args, "-TargetBranch");

var externalToolInvocations = BuildExternalToolInvocations(repositoryRoot, globalStandardsRoot, mode, targetBranch);
var orchestrator = new ValidationOrchestrator(externalToolInvocations);
var report = await orchestrator.RunAsync(repositoryRoot, Path.GetFullPath(globalStandardsRoot), mode, targetBranch);

var discrepanciesFilePath = Path.Combine(repositoryRoot, "Working", "ValidationDiscrepancies.md");
var discrepanciesJsonFilePath = Path.Combine(repositoryRoot, "Working", "ValidationDiscrepancies.json");
var historyDirectoryPath = Path.Combine(repositoryRoot, "Working", "ValidationHistory");
const int MaxHistoryEntries = 20;

var exitCode = report.HasEngineErrors ? 2 : (report.HasViolations ? 1 : 0);

if (report.HasEngineErrors || report.HasViolations || report.Findings.Any(finding => finding.Severity == ViolationSeverity.ManualReviewItem))
{
    var renderedReport = MarkdownReportRenderer.Render(report);
    var renderedJsonReport = JsonReportRenderer.Render(report, exitCode);

    Directory.CreateDirectory(Path.Combine(repositoryRoot, "Working"));
    File.WriteAllText(discrepanciesFilePath, renderedReport);
    File.WriteAllText(discrepanciesJsonFilePath, renderedJsonReport);
    Console.WriteLine(renderedReport);

    Directory.CreateDirectory(historyDirectoryPath);
    var historyBaseName = $"{DateTime.UtcNow:yyyyMMdd-HHmmss}_{mode}";
    File.WriteAllText(Path.Combine(historyDirectoryPath, $"{historyBaseName}.md"), renderedReport);
    File.WriteAllText(Path.Combine(historyDirectoryPath, $"{historyBaseName}.json"), renderedJsonReport);
    PruneHistory(historyDirectoryPath, MaxHistoryEntries);
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

static ValidationRunMode ParseRunMode(string[] commandLineArgs)
{
    var modeArgument = ParseArgument(commandLineArgs, "-Mode");
    if (string.IsNullOrWhiteSpace(modeArgument))
    {
        return ValidationRunMode.Manual;
    }

    return Enum.Parse<ValidationRunMode>(modeArgument, ignoreCase: true);
}

static string? ResolveGlobalStandardsRoot(
    string repositoryRoot, string[] commandLineArgs, ValidationEngineConfigReader.ValidationEngineConfig? config)
{
    var explicitRoot = ParseArgument(commandLineArgs, "-GlobalStandardsRoot");
    if (!string.IsNullOrWhiteSpace(explicitRoot))
    {
        return explicitRoot;
    }

    var environmentRoot = Environment.GetEnvironmentVariable("GLOBALSTANDARDS_ROOT");
    if (!string.IsNullOrWhiteSpace(environmentRoot))
    {
        return environmentRoot;
    }

    var configuredRoot = ValidationEngineConfigReader.ResolveStandardsPath(repositoryRoot, config);
    if (!string.IsNullOrWhiteSpace(configuredRoot))
    {
        return configuredRoot;
    }

    // Self-contained fallback: ValidationEngine ships its own Docs/Standards alongside the
    // published binary, so a zero-config run against any repository still resolves standards
    // content without requiring a sibling directory or explicit configuration. Callers treat the
    // returned value as a root that itself contains a Docs/Standards subfolder, so we return
    // AppContext.BaseDirectory rather than the Docs/Standards path itself.
    var engineOwnStandardsRoot = AppContext.BaseDirectory;
    return Directory.Exists(Path.Combine(engineOwnStandardsRoot, "Docs", "Standards")) ? engineOwnStandardsRoot : null;
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

static void PruneHistory(string historyDirectoryPath, int maxHistoryEntries)
{
    var historyFiles = new DirectoryInfo(historyDirectoryPath)
        .GetFiles("*.md")
        .OrderByDescending(file => file.Name, StringComparer.Ordinal)
        .ToList();

    foreach (var staleFile in historyFiles.Skip(maxHistoryEntries))
    {
        staleFile.Delete();

        var pairedJsonPath = Path.ChangeExtension(staleFile.FullName, ".json");
        if (File.Exists(pairedJsonPath))
        {
            File.Delete(pairedJsonPath);
        }
    }
}

static IReadOnlyList<ExternalToolInvocation> BuildExternalToolInvocations(
    string repositoryRoot, string globalStandardsRoot, ValidationRunMode mode, string? targetBranch)
{
    return ValidationEngineAgentInvocationFactory.Build(
        repositoryRoot, globalStandardsRoot, mode, targetBranch, AppContext.BaseDirectory);
}
