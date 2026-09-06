namespace ValidationEngine.Resolver;

public sealed class AppTypeDetector
{
    public static RepositoryProfile Detect(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);

        if (Directory.EnumerateFiles(repositoryRoot, "*.sqlproj", SearchOption.AllDirectories).Any())
        {
            return new RepositoryProfile
            {
                AppType = AppType.Database,
                HasSpaClient = false,
                SpaClientUsesReact = false
            };
        }

        var projectFiles = Directory.EnumerateFiles(repositoryRoot, "*.csproj", SearchOption.AllDirectories).ToList();
        var isWebProject = projectFiles.Any(IsWebSdkProject);

        var appType = isWebProject ? AppType.WebAppWebApi : AppType.ClassLibrary;
        var hasSpaClient = appType == AppType.WebAppWebApi && HasSpaClientFolder(repositoryRoot);
        var spaClientUsesReact = hasSpaClient && SpaClientUsesReactDependency(repositoryRoot);

        return new RepositoryProfile
        {
            AppType = appType,
            HasSpaClient = hasSpaClient,
            SpaClientUsesReact = spaClientUsesReact
        };
    }

    private static bool IsWebSdkProject(string projectFilePath)
    {
        var content = File.ReadAllText(projectFilePath);
        return content.Contains("Microsoft.NET.Sdk.Web", StringComparison.Ordinal);
    }

    private static bool HasSpaClientFolder(string repositoryRoot)
    {
        return Directory.EnumerateFiles(repositoryRoot, "package.json", SearchOption.AllDirectories)
            .Any(path => !path.Contains($"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
    }

    private static bool SpaClientUsesReactDependency(string repositoryRoot)
    {
        var packageJsonFiles = Directory.EnumerateFiles(repositoryRoot, "package.json", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}node_modules{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

        foreach (var packageJsonFile in packageJsonFiles)
        {
            var content = File.ReadAllText(packageJsonFile);
            if (content.Contains("\"react\"", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
