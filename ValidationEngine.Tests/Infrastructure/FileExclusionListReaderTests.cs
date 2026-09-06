using ValidationEngine.Infrastructure;

namespace ValidationEngine.Tests.Infrastructure;

public sealed class FileExclusionListReaderTests : IDisposable
{
    private readonly string _repositoryRoot;

    public FileExclusionListReaderTests()
    {
        _repositoryRoot = Path.Combine(Path.GetTempPath(), $"FileExclusionListReaderTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_repositoryRoot);
    }

    [Fact]
    public void Read_WhenNoConfigFileExists_ReturnsEmptyList()
    {
        var patterns = FileExclusionListReader.Read(_repositoryRoot);

        Assert.Empty(patterns);
    }

    [Fact]
    public void Read_WhenConfigFileExists_ReturnsExcludePatterns()
    {
        var standardsDirectory = Path.Combine(_repositoryRoot, "Standards");
        Directory.CreateDirectory(standardsDirectory);
        File.WriteAllText(Path.Combine(standardsDirectory, "FileExclusions.json"), """
            {
              "exclude": [ "*.generated.cs", "Vendor/**" ]
            }
            """);

        var patterns = FileExclusionListReader.Read(_repositoryRoot);

        Assert.Equal(["*.generated.cs", "Vendor/**"], patterns);
    }

    [Theory]
    [InlineData("Foo.generated.cs", true)]
    [InlineData("Foo.cs", false)]
    [InlineData("Vendor/Lib/Thing.cs", true)]
    [InlineData("Src/Thing.cs", false)]
    public void IsExcluded_MatchesGlobPatternsCaseInsensitively(string filePath, bool expectedExcluded)
    {
        IReadOnlyList<string> patterns = ["*.generated.cs", "Vendor/**"];

        var isExcluded = FileExclusionListReader.IsExcluded(filePath, patterns);

        Assert.Equal(expectedExcluded, isExcluded);
    }

    [Theory]
    [InlineData("Templates/File.md", true)]
    [InlineData("Templates/Sub/File.md", false)]
    public void IsExcluded_SingleStar_DoesNotCrossPathSeparator(string filePath, bool expectedExcluded)
    {
        IReadOnlyList<string> patterns = ["Templates/*.md"];

        var isExcluded = FileExclusionListReader.IsExcluded(filePath, patterns);

        Assert.Equal(expectedExcluded, isExcluded);
    }

    [Fact]
    public void IsExcluded_DoubleStar_CrossesPathSeparators()
    {
        IReadOnlyList<string> patterns = ["Templates/**"];

        Assert.True(FileExclusionListReader.IsExcluded("Templates/Sub/Deep/File.md", patterns));
    }

    [Fact]
    public void IsExcluded_WhenNoPatterns_ReturnsFalse()
    {
        var isExcluded = FileExclusionListReader.IsExcluded("Anything.cs", []);

        Assert.False(isExcluded);
    }

    public void Dispose()
    {
        if (Directory.Exists(_repositoryRoot))
        {
            Directory.Delete(_repositoryRoot, recursive: true);
        }
    }
}
