namespace ValidationEngine.Agent.Tests;

public class AgentClientFactoryBuilderTests : IDisposable
{
    private readonly string _repositoryRoot;
    private readonly List<string> _envVarsToClear = [];

    public AgentClientFactoryBuilderTests()
    {
        _repositoryRoot = Directory.CreateTempSubdirectory("ValidationEngineAgentTests").FullName;
    }

    public void Dispose()
    {
        foreach (var name in _envVarsToClear)
        {
            Environment.SetEnvironmentVariable(name, null);
        }

        if (Directory.Exists(_repositoryRoot))
        {
            Directory.Delete(_repositoryRoot, recursive: true);
        }

        GC.SuppressFinalize(this);
    }

    [Fact]
    public void Build_ReturnsNull_WhenNoProvidersConfigured()
    {
        WriteAppSettings("""{ "ValidationAgent": { "Providers": [] } }""");

        var factory = AgentClientFactoryBuilder.Build(_repositoryRoot);

        Assert.Null(factory);
    }

    [Fact]
    public void Build_ReturnsFactoryWithNoActiveValidationClients_WhenAllProvidersInactive()
    {
        WriteAppSettings("""
            {
              "ValidationAgent": {
                "Providers": [
                  { "Name": "ProviderA", "Type": "AzureOpenAI", "Purpose": "Validation", "IsActive": false, "Endpoint": "https://a.openai.azure.com" }
                ]
              }
            }
            """);

        var factory = AgentClientFactoryBuilder.Build(_repositoryRoot);

        Assert.Null(factory);
    }

    [Fact]
    public void Build_CreatesDistinctClientsForEachActiveAzureOpenAiProvider()
    {
        SetApiKeyEnvVar("ProviderA", "key-a");
        SetApiKeyEnvVar("ProviderB", "key-b");

        WriteAppSettings("""
            {
              "ValidationAgent": {
                "Providers": [
                  { "Name": "ProviderA", "Type": "AzureOpenAI", "Purpose": "Validation", "IsActive": true, "Endpoint": "https://a.openai.azure.com", "DeploymentName": "gpt-a" },
                  { "Name": "ProviderB", "Type": "AzureOpenAI", "Purpose": "Validation", "IsActive": true, "Endpoint": "https://b.openai.azure.com", "DeploymentName": "gpt-b" }
                ]
              }
            }
            """);

        var factory = AgentClientFactoryBuilder.Build(_repositoryRoot);

        Assert.NotNull(factory);

        var validationClients = factory.CreateActiveValidationClients();

        Assert.Equal(2, validationClients.Count);
        Assert.Contains(validationClients, c => c.Name == "ProviderA");
        Assert.Contains(validationClients, c => c.Name == "ProviderB");
    }

    [Fact]
    public void Build_ThrowsForUnsupportedProviderType_WhenClientIsRequested()
    {
        WriteAppSettings("""
            {
              "ValidationAgent": {
                "Providers": [
                  { "Name": "ProviderC", "Type": "Unsupported", "Purpose": "Validation", "IsActive": true, "Endpoint": "https://c.example.com" }
                ]
              }
            }
            """);

        var factory = AgentClientFactoryBuilder.Build(_repositoryRoot);

        Assert.NotNull(factory);
        Assert.Throws<NotSupportedException>(() => factory.CreateActiveValidationClients());
    }

    private void WriteAppSettings(string json) =>
        File.WriteAllText(Path.Combine(_repositoryRoot, "appsettings.json"), json);

    private void SetApiKeyEnvVar(string providerName, string apiKey)
    {
        var variableName = $"{providerName}_API_KEY";
        Environment.SetEnvironmentVariable(variableName, apiKey);
        _envVarsToClear.Add(variableName);
    }
}
