using System.Diagnostics;
using ValidationEngine.Link;

namespace ValidationEngine.Link.Tests;

public class FileScopeResolverTests : IDisposable
{
    private readonly string _repositoryRoot = Directory.CreateTempSubdirectory("LinkValidationEngineTests_FileScope_").FullName;

    public FileScopeResolverTests()
    {
        RunGit("init -b main");
        RunGit("config user.email test@example.com");
        RunGit("config user.name Test");

        File.WriteAllText(Path.Combine(_repositoryRoot, "Readme.md"), "# Readme");
        File.WriteAllText(Path.Combine(_repositoryRoot, "notes.txt"), "not markdown");

        RunGit("add .");
        RunGit("commit -m initial");
    }

    [Fact]
    public void Resolve_FullAudit_ReturnsOnlyMarkdownFiles()
    {
        var files = FileScopeResolver.Resolve(_repositoryRoot, fullAudit: true, baseBranch: null);

        Assert.Contains("Readme.md", files);
        Assert.DoesNotContain("notes.txt", files);
    }

    [Fact]
    public void Resolve_ChangedFiles_ReturnsOnlyMarkdownChangesAgainstBaseBranch()
    {
        RunGit("checkout -b feature");
        File.WriteAllText(Path.Combine(_repositoryRoot, "New.md"), "# New");
        File.WriteAllText(Path.Combine(_repositoryRoot, "New.txt"), "new text");
        RunGit("add .");
        RunGit("commit -m feature-change");

        var files = FileScopeResolver.Resolve(_repositoryRoot, fullAudit: false, baseBranch: "main");

        Assert.Contains("New.md", files);
        Assert.DoesNotContain("New.txt", files);
    }

    private void RunGit(string arguments)
    {
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = _repositoryRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        })!;
        process.WaitForExit();
    }

    public void Dispose()
    {
        try
        {
            var directoryInfo = new DirectoryInfo(_repositoryRoot);
            foreach (var file in directoryInfo.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                file.Attributes = FileAttributes.Normal;
            }

            directoryInfo.Delete(recursive: true);
        }
        catch (IOException)
        {
            // Best-effort cleanup — ignore if files are locked (common for .git internals on Windows).
        }
    }
}
