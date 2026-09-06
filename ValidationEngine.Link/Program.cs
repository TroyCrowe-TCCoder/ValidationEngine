using ValidationEngine.Infrastructure;
using ValidationEngine.Link;

var repositoryRoot = Path.GetFullPath(ParseArgument(args, "-RepositoryRoot") ?? Directory.GetCurrentDirectory());
var fullAudit = args.Contains("--full-audit");
var baseBranch = ParseArgument(args, "--base-branch");
var engineConfig = ValidationEngineConfigReader.Read(repositoryRoot);
var app = ParseArgument(args, "--app")
    ?? engineConfig?.SolutionName
    ?? new DirectoryInfo(repositoryRoot).Name;
var outputPath = ParseArgument(args, "--output");
var concurrency = int.TryParse(ParseArgument(args, "--concurrency"), out var parsedConcurrency)
    ? parsedConcurrency
    : Environment.ProcessorCount;
var timeoutSeconds = int.TryParse(ParseArgument(args, "--timeout"), out var parsedTimeoutSeconds)
    ? parsedTimeoutSeconds
    : 10;

var runId = Guid.NewGuid().ToString("N");

try
{
    var filePaths = FileScopeResolver.Resolve(repositoryRoot, fullAudit, baseBranch);

    using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(timeoutSeconds) };

    var runner = new LinkValidationRunner(
        new InternalLinkValidator(),
        new ExternalLinkValidator(httpClient));

    var issues = await runner.RunAsync(filePaths, repositoryRoot, runId, app, concurrency, CancellationToken.None);

    if (!string.IsNullOrEmpty(outputPath))
    {
        await LinkIssueJsonWriter.WriteAsync(outputPath, issues, CancellationToken.None);
    }

    foreach (var issue in issues)
    {
        Console.WriteLine($"[{issue.LinkType}] {issue.SourceFile}: {issue.Link} — {issue.Issue}");
    }

    Console.WriteLine($"LinkValidationEngine: {filePaths.Count} file(s) scanned, {issues.Count} issue(s) found.");

    return issues.Count == 0 ? 0 : 1;
}
catch (Exception exception) when (exception is IOException or InvalidOperationException)
{
    Console.Error.WriteLine($"LinkValidationEngine failed: {exception.Message}");
    return 2;
}

static string? ParseArgument(string[] args, string name)
{
    var index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}
