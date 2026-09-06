using ValidationEngine.Resolver;

namespace ValidationEngine.Tests.Resolver;

public sealed class AppTypeDetectorTests : IDisposable
{
    private readonly string _fixtureRoot;

    public AppTypeDetectorTests()
    {
        _fixtureRoot = Path.Combine(Path.GetTempPath(), $"ValidationEngineTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_fixtureRoot);
    }

    [Fact]
    public void Detect_WhenRepositoryContainsSqlProj_ReturnsDatabaseAppType()
    {
        File.WriteAllText(Path.Combine(_fixtureRoot, "Sample.sqlproj"), "<Project />");

        var repositoryProfile = AppTypeDetector.Detect(_fixtureRoot);

        Assert.Equal(AppType.Database, repositoryProfile.AppType);
    }

    [Fact]
    public void Detect_WhenRepositoryContainsWebSdkProject_ReturnsWebAppWebApiAppType()
    {
        File.WriteAllText(Path.Combine(_fixtureRoot, "Sample.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk.Web\" />");

        var repositoryProfile = AppTypeDetector.Detect(_fixtureRoot);

        Assert.Equal(AppType.WebAppWebApi, repositoryProfile.AppType);
    }

    [Fact]
    public void Detect_WhenRepositoryContainsOnlyNonWebSdkProject_ReturnsClassLibraryAppType()
    {
        File.WriteAllText(Path.Combine(_fixtureRoot, "Sample.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        var repositoryProfile = AppTypeDetector.Detect(_fixtureRoot);

        Assert.Equal(AppType.ClassLibrary, repositoryProfile.AppType);
    }

    [Fact]
    public void Detect_WhenWebAppHasPackageJsonWithReact_ReturnsSpaClientUsesReactTrue()
    {
        File.WriteAllText(Path.Combine(_fixtureRoot, "Sample.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk.Web\" />");
        var spaClientDirectory = Path.Combine(_fixtureRoot, "ClientApp");
        Directory.CreateDirectory(spaClientDirectory);
        File.WriteAllText(Path.Combine(spaClientDirectory, "package.json"), "{ \"dependencies\": { \"react\": \"18.0.0\" } }");

        var repositoryProfile = AppTypeDetector.Detect(_fixtureRoot);

        Assert.True(repositoryProfile.HasSpaClient);
        Assert.True(repositoryProfile.SpaClientUsesReact);
    }

    [Fact]
    public void Detect_WhenWebAppHasNoPackageJson_ReturnsHasSpaClientFalse()
    {
        File.WriteAllText(Path.Combine(_fixtureRoot, "Sample.csproj"), "<Project Sdk=\"Microsoft.NET.Sdk.Web\" />");

        var repositoryProfile = AppTypeDetector.Detect(_fixtureRoot);

        Assert.False(repositoryProfile.HasSpaClient);
    }

    public void Dispose()
    {
        if (Directory.Exists(_fixtureRoot))
        {
            Directory.Delete(_fixtureRoot, recursive: true);
        }
    }
}
