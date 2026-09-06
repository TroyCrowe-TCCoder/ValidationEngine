using ValidationEngine.Link;
using ValidationEngine.Link.Models;

namespace ValidationEngine.Link.Tests;

public class InternalLinkValidatorTests : IDisposable
{
    private readonly string _repositoryRoot = Directory.CreateTempSubdirectory("LinkValidationEngineTests_").FullName;
    private readonly InternalLinkValidator _validator = new();

    [Fact]
    public void Validate_ExistingRelativeFile_ReturnsNull()
    {
        WriteFile("Docs/Target.md", "# Target\nContent.");
        WriteFile("Docs/Source.md", "See [target](./Target.md).");

        var reference = new LinkReference
        {
            SourceFile = "Docs/Source.md",
            RawText = "[target](./Target.md)",
            Target = "./Target.md",
            LineNumber = 1
        };

        var issue = _validator.Validate(reference, _repositoryRoot, "run-1", "TestApp");

        Assert.Null(issue);
    }

    [Fact]
    public void Validate_MissingRelativeFile_ReturnsIssue()
    {
        WriteFile("Docs/Source.md", "See [target](./Missing.md).");

        var reference = new LinkReference
        {
            SourceFile = "Docs/Source.md",
            RawText = "[target](./Missing.md)",
            Target = "./Missing.md",
            LineNumber = 1
        };

        var issue = _validator.Validate(reference, _repositoryRoot, "run-1", "TestApp");

        Assert.NotNull(issue);
        Assert.Equal(LinkType.Internal, issue!.LinkType);
        Assert.Contains("not found", issue.Issue, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_ValidAnchorInTargetFile_ReturnsNull()
    {
        WriteFile("Docs/Target.md", "# Some Heading\nContent.");
        WriteFile("Docs/Source.md", "See [target](./Target.md#some-heading).");

        var reference = new LinkReference
        {
            SourceFile = "Docs/Source.md",
            RawText = "[target](./Target.md#some-heading)",
            Target = "./Target.md#some-heading",
            LineNumber = 1
        };

        var issue = _validator.Validate(reference, _repositoryRoot, "run-1", "TestApp");

        Assert.Null(issue);
    }

    [Fact]
    public void Validate_MissingAnchorInTargetFile_ReturnsIssue()
    {
        WriteFile("Docs/Target.md", "# Some Other Heading\nContent.");
        WriteFile("Docs/Source.md", "See [target](./Target.md#missing-heading).");

        var reference = new LinkReference
        {
            SourceFile = "Docs/Source.md",
            RawText = "[target](./Target.md#missing-heading)",
            Target = "./Target.md#missing-heading",
            LineNumber = 1
        };

        var issue = _validator.Validate(reference, _repositoryRoot, "run-1", "TestApp");

        Assert.NotNull(issue);
        Assert.Contains("Anchor", issue!.Issue, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_AnchorOnlyLink_ResolvesAgainstSourceFile()
    {
        WriteFile("Docs/Source.md", "# Some Heading\nSee [section](#some-heading).");

        var reference = new LinkReference
        {
            SourceFile = "Docs/Source.md",
            RawText = "[section](#some-heading)",
            Target = "#some-heading",
            LineNumber = 2
        };

        var issue = _validator.Validate(reference, _repositoryRoot, "run-1", "TestApp");

        Assert.Null(issue);
    }

    private void WriteFile(string relativePath, string content)
    {
        var fullPath = Path.Combine(_repositoryRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_repositoryRoot, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort cleanup — ignore if files are locked.
        }
    }
}
