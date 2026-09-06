using System.Diagnostics;
using ValidationEngine.Infrastructure;
using ValidationEngine.Models;

namespace ValidationEngine.Tests.Infrastructure;

/// <summary>
/// End-to-end tests that exercise <see cref="ValidationOrchestrator.RunAsync"/> against a real
/// fixture git repository, covering the meta-check, branch guard, change resolution, rule
/// execution, and combined report assembly together rather than in isolation.
/// </summary>
public sealed class ValidationOrchestratorIntegrationTests : IDisposable
{
    private readonly string _repositoryRoot;
    private readonly string _globalStandardsRoot;

    public ValidationOrchestratorIntegrationTests()
    {
        _repositoryRoot = Path.Combine(Path.GetTempPath(), $"ValidationOrchestratorIT_Repo_{Guid.NewGuid():N}");
        _globalStandardsRoot = Path.Combine(Path.GetTempPath(), $"ValidationOrchestratorIT_Standards_{Guid.NewGuid():N}");

        Directory.CreateDirectory(_repositoryRoot);
        Directory.CreateDirectory(_globalStandardsRoot);

        WriteValidStandardsFiles(_globalStandardsRoot);
        RunGit(_repositoryRoot, "init -b work");
        RunGit(_repositoryRoot, "config user.email \"test@example.com\"");
        RunGit(_repositoryRoot, "config user.name \"Test\"");
    }

    [Fact]
    public async Task Run_ManualMode_AuditsAllTrackedFilesAndProducesCombinedReport()
    {
        File.WriteAllText(Path.Combine(_repositoryRoot, "Sample.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk.Web\" />");
        File.WriteAllText(Path.Combine(_repositoryRoot, ".gitignore"), "bin/\nobj/\n");
        RunGit(_repositoryRoot, "add .");
        RunGit(_repositoryRoot, "commit -m \"initial\"");

        var report = await new ValidationOrchestrator().RunAsync(_repositoryRoot, _globalStandardsRoot, ValidationRunMode.Manual);

        Assert.False(report.HasEngineErrors, string.Join("; ", report.EngineErrors.Select(e => e.Message)));
        Assert.NotNull(report.RunContext);
        Assert.Equal("WebAppWebApi", report.RunContext!.DetectedAppType);
        Assert.Equal(ValidationRunMode.Manual, report.RunContext.Mode);
        Assert.Equal(new DirectoryInfo(_repositoryRoot).Name, report.RunContext.RepositoryName);
        Assert.Equal(Path.GetFullPath(_repositoryRoot), report.RunContext.RepositoryRoot);
        Assert.DoesNotContain(report.Findings, finding => finding.RuleId == "resolver.app-type");
    }

    [Fact]
    public async Task Run_SystemModeWithStagedChanges_ValidatesOnlyStagedFilesAndReportsViolation()
    {
        File.WriteAllText(Path.Combine(_repositoryRoot, "Sample.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk.Web\" />");
        RunGit(_repositoryRoot, "add Sample.csproj");
        RunGit(_repositoryRoot, "commit -m \"initial\"");

        var standardsDirectory = Path.Combine(_repositoryRoot, "Docs", "Standards");
        Directory.CreateDirectory(standardsDirectory);
        File.WriteAllText(Path.Combine(standardsDirectory, "bad_name.md"), "content");
        RunGit(_repositoryRoot, "add Docs/Standards/bad_name.md");

        var report = await new ValidationOrchestrator().RunAsync(_repositoryRoot, _globalStandardsRoot, ValidationRunMode.System);

        Assert.False(report.HasEngineErrors, string.Join("; ", report.EngineErrors.Select(e => e.Message)));
        Assert.True(report.HasViolations);
        Assert.Contains(report.Findings, finding => finding.RuleId == "file-specification.6.10" && finding.Severity == ViolationSeverity.Violation);
    }

    [Fact]
    public async Task Run_WhenFileMatchesFileExclusionListPattern_ExcludesFileFromValidation()
    {
        File.WriteAllText(Path.Combine(_repositoryRoot, "Sample.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk.Web\" />");
        RunGit(_repositoryRoot, "add Sample.csproj");
        RunGit(_repositoryRoot, "commit -m \"initial\"");

        var repoStandardsDirectory = Path.Combine(_repositoryRoot, "Docs", "Standards");
        Directory.CreateDirectory(repoStandardsDirectory);
        File.WriteAllText(Path.Combine(repoStandardsDirectory, "bad_name.md"), "content");
        RunGit(_repositoryRoot, "add Docs/Standards/bad_name.md");

        var exclusionConfigDirectory = Path.Combine(_repositoryRoot, "Standards");
        Directory.CreateDirectory(exclusionConfigDirectory);
        File.WriteAllText(Path.Combine(exclusionConfigDirectory, "FileExclusions.json"), """
            {
              "exclude": [ "Docs/Standards/bad_name.md" ]
            }
            """);

        var report = await new ValidationOrchestrator().RunAsync(_repositoryRoot, _globalStandardsRoot, ValidationRunMode.System);

        Assert.False(report.HasEngineErrors, string.Join("; ", report.EngineErrors.Select(e => e.Message)));
        Assert.DoesNotContain(report.Findings, finding => finding.RuleId == "file-specification.6.10");
    }

    [Fact]
    public async Task Run_SystemModeOnDevBranch_BlocksWithEngineErrorBeforeRunningRules()
    {
        File.WriteAllText(Path.Combine(_repositoryRoot, "Sample.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk.Web\" />");
        RunGit(_repositoryRoot, "add Sample.csproj");
        RunGit(_repositoryRoot, "commit -m \"initial\"");
        RunGit(_repositoryRoot, "checkout -b dev");

        var report = await new ValidationOrchestrator().RunAsync(_repositoryRoot, _globalStandardsRoot, ValidationRunMode.System);

        Assert.True(report.HasEngineErrors);
        Assert.Contains(report.EngineErrors, error => error.Message.Contains("Feature branch required", StringComparison.Ordinal));
        Assert.Empty(report.Findings);
    }

    [Fact]
    public async Task Run_WhenChecklistMetaCheckFails_ShortCircuitsBeforeRuleExecution()
    {
        var standardsDirectory = Path.Combine(_globalStandardsRoot, "Docs", "Standards");
        File.WriteAllText(Path.Combine(standardsDirectory, "GlobalFileSpecificationStandards.md"), "# Broken\nNo compliance section here.");

        File.WriteAllText(Path.Combine(_repositoryRoot, "Sample.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk.Web\" />");
        RunGit(_repositoryRoot, "add Sample.csproj");
        RunGit(_repositoryRoot, "commit -m \"initial\"");

        var report = await new ValidationOrchestrator().RunAsync(_repositoryRoot, _globalStandardsRoot, ValidationRunMode.Manual);

        Assert.False(report.HasEngineErrors);
        Assert.Contains(report.Findings, finding => finding.StandardFile == "GlobalFileSpecificationStandards.md");
        Assert.DoesNotContain(report.Findings, finding => finding.RuleId == "resolver.app-type");
    }

    private static void WriteValidStandardsFiles(string globalStandardsRoot)
    {
        var standardsDirectory = Path.Combine(globalStandardsRoot, "Docs", "Standards");
        Directory.CreateDirectory(standardsDirectory);

        const string checklistMarkdown = """
            # Standard
            <!-- STD-MARKER: file-specification.file -->

            ## 1. Purpose
            <!-- STD-MARKER: file-specification.1 -->
            Content.

            ## 6. Compliance Verification
            - [ ] File carries version and status header <!-- STD-MARKER: file-specification.6.8 -->
            - [ ] File name is PascalCase <!-- STD-MARKER: file-specification.6.10 -->

            ## 7. Governance
            Content.
            """;

        File.WriteAllText(Path.Combine(standardsDirectory, "GlobalFileSpecificationStandards.md"), checklistMarkdown);
        File.WriteAllText(Path.Combine(standardsDirectory, "GlobalRepositoryStandards.md"), checklistMarkdown.Replace("file-specification", "repository", StringComparison.Ordinal));
        File.WriteAllText(Path.Combine(standardsDirectory, "GlobalSolutionStructureStandards.md"), checklistMarkdown.Replace("file-specification", "solution-structure", StringComparison.Ordinal));

        File.WriteAllText(Path.Combine(standardsDirectory, "StandardsApplicabilityMatrix.csv"),
            "StandardFile,MarkerBase,WebAppWebApi,Database,ClassLibrary,Notes\n" +
            "GlobalCodingStandards,coding,Applies,Applies,Applies,Core coding rules.\n" +
            "GlobalTestingStandards,testing,Applies,Applies,Applies,Testing rules.\n" +
            "GlobalSecurityStandards,security,Applies,Applies,Applies,Security rules.\n" +
            "GlobalPerformanceStandards,performance,Applies,Applies,Conditional,Perf rules.\n" +
            "GlobalLoggingStandards,logging,Applies,Applies,Applies,Logging rules.\n" +
            "GlobalCachingStandards,caching,Applies,Applies,Conditional,Caching rules.\n" +
            "GlobalAzureDevOpsPipelineStandards,azure-devops-pipeline,Applies,Applies,Applies,Pipeline rules.\n" +
            "GlobalRepositoryStandards,repository,Applies,Applies,Applies,Repository rules.\n" +
            "GlobalSolutionStructureStandards,solution-structure,Applies,Applies,Applies,Solution structure rules.\n");
    }

    private static void RunGit(string repositoryRoot, string arguments)
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
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            var error = process.StandardError.ReadToEnd();
            throw new InvalidOperationException($"git {arguments} failed: {error}");
        }
    }

    public void Dispose()
    {
        TryDelete(_repositoryRoot);
        TryDelete(_globalStandardsRoot);
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                var directoryInfo = new DirectoryInfo(path) { Attributes = FileAttributes.Normal };
                foreach (var file in directoryInfo.GetFiles("*", SearchOption.AllDirectories))
                {
                    file.Attributes = FileAttributes.Normal;
                }

                Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup; leftover temp directories do not affect subsequent test runs.
        }
    }
}
