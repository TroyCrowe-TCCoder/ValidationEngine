using ValidationEngine.Infrastructure;

namespace ValidationEngine.Tests.Infrastructure;

/// <summary>
/// Unit tests for <see cref="ValidationEngineAgentInvocationFactory"/>, covering path resolution
/// relative to the caller's own bin output directory, argument building, and the "sibling
/// executable not found" fallback used when <c>ValidationEngine.Agent</c> hasn't been built.
/// </summary>
public sealed class ValidationEngineAgentInvocationFactoryTests
{
    [Fact]
    public void ResolveAgentEnginePath_MirrorsCallersConfigurationAndTfm()
    {
        var ownBinDirectory = Path.Combine("C:", "repo", "ValidationEngine", "bin", "Debug", "net10.0");

        var resolvedPath = ValidationEngineAgentInvocationFactory.ResolveAgentEnginePath(ownBinDirectory);

        var expectedExecutableName = OperatingSystem.IsWindows() ? "ValidationEngine.Agent.exe" : "ValidationEngine.Agent";
        var expectedPath = Path.GetFullPath(Path.Combine(
            "C:", "repo", "ValidationEngine.Agent", "bin", "Debug", "net10.0", expectedExecutableName));

        Assert.Equal(expectedPath, resolvedPath);
    }

    [Fact]
    public void ResolveAgentEnginePath_UsesReleaseConfigurationWhenCallerIsRelease()
    {
        var ownBinDirectory = Path.Combine("C:", "repo", "ValidationEngine", "bin", "Release", "net10.0");

        var resolvedPath = ValidationEngineAgentInvocationFactory.ResolveAgentEnginePath(ownBinDirectory);

        Assert.Contains(Path.Combine("ValidationEngine.Agent", "bin", "Release", "net10.0"), resolvedPath);
    }

    [Fact]
    public void BuildArguments_IncludesTargetBranchWhenProvided()
    {
        var arguments = ValidationEngineAgentInvocationFactory.BuildArguments(
            "C:\\repo", "C:\\GlobalStandards", ValidationRunMode.System, "feature/x");

        Assert.Equal(
            "-RepositoryRoot \"C:\\repo\" -GlobalStandardsRoot \"C:\\GlobalStandards\" -Mode System -TargetBranch \"feature/x\"",
            arguments);
    }

    [Fact]
    public void BuildArguments_OmitsTargetBranchWhenNullOrWhitespace()
    {
        var arguments = ValidationEngineAgentInvocationFactory.BuildArguments(
            "C:\\repo", "C:\\GlobalStandards", ValidationRunMode.Manual, null);

        Assert.Equal(
            "-RepositoryRoot \"C:\\repo\" -GlobalStandardsRoot \"C:\\GlobalStandards\" -Mode Manual",
            arguments);
    }

    [Fact]
    public void Build_ReturnsEmptyList_WhenSiblingExecutableDoesNotExist()
    {
        var ownBinDirectory = Path.Combine(Path.GetTempPath(), $"AgentInvocationFactoryTest_{Guid.NewGuid():N}", "ValidationEngine", "bin", "Debug", "net10.0");

        var invocations = ValidationEngineAgentInvocationFactory.Build(
            "C:\\repo", "C:\\GlobalStandards", ValidationRunMode.System, null, ownBinDirectory);

        Assert.Empty(invocations);
    }

    [Fact]
    public void Build_ReturnsInvocation_WhenSiblingExecutableExists()
    {
        var testRoot = Path.Combine(Path.GetTempPath(), $"AgentInvocationFactoryTest_{Guid.NewGuid():N}");
        var ownBinDirectory = Path.Combine(testRoot, "ValidationEngine", "bin", "Debug", "net10.0");
        var expectedExecutableName = OperatingSystem.IsWindows() ? "ValidationEngine.Agent.exe" : "ValidationEngine.Agent";
        var agentEngineDirectory = Path.Combine(testRoot, "ValidationEngine.Agent", "bin", "Debug", "net10.0");

        Directory.CreateDirectory(agentEngineDirectory);
        File.WriteAllText(Path.Combine(agentEngineDirectory, expectedExecutableName), string.Empty);

        try
        {
            var invocations = ValidationEngineAgentInvocationFactory.Build(
                "C:\\repo", "C:\\GlobalStandards", ValidationRunMode.System, "dev", ownBinDirectory);

            var invocation = Assert.Single(invocations);
            Assert.Equal("ValidationEngine.Agent", invocation.ToolName);
            Assert.Equal("C:\\repo", invocation.WorkingDirectory);
            Assert.Equal(
                Path.Combine(agentEngineDirectory, expectedExecutableName),
                invocation.ExecutablePath);
            Assert.Contains("-TargetBranch \"dev\"", invocation.Arguments);
        }
        finally
        {
            Directory.Delete(testRoot, recursive: true);
        }
    }

    [Fact]
    public void ResolveAgentEnginePath_PrefersPackagedFlatLayout_WhenAgentExeSitsAlongsideOwnExe()
    {
        var testRoot = Path.Combine(Path.GetTempPath(), $"AgentInvocationFactoryTest_{Guid.NewGuid():N}");
        var ownBinDirectory = Path.Combine(testRoot, "tools");
        var expectedExecutableName = OperatingSystem.IsWindows() ? "ValidationEngine.Agent.exe" : "ValidationEngine.Agent";

        Directory.CreateDirectory(ownBinDirectory);
        File.WriteAllText(Path.Combine(ownBinDirectory, expectedExecutableName), string.Empty);

        try
        {
            var resolvedPath = ValidationEngineAgentInvocationFactory.ResolveAgentEnginePath(ownBinDirectory);

            Assert.Equal(Path.Combine(ownBinDirectory, expectedExecutableName), resolvedPath);
        }
        finally
        {
            Directory.Delete(testRoot, recursive: true);
        }
    }
}
