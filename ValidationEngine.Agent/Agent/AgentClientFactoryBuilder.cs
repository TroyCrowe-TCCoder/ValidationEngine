using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ValidationEngine.Agent.Configuration;
using ValidationEngine.Agent.Constants;

namespace ValidationEngine.Agent;

/// <summary>
/// Builds an <see cref="AgentClientFactory"/> from "ValidationAgent" configuration (AGT-002/AGT-004).
/// Kept out of Program.cs per GlobalCodingStandards.md coding.2.1 — the entry point performs only
/// wiring, not the configuration-binding and credential-resolution behavior implemented here.
/// </summary>
/// <remarks>
/// <c>appsettings.json</c> itself is required at <paramref name="repositoryRoot"/> — every consuming
/// repository must ship one (see README.md "Configuring AI-Assisted Review") — but its contents are
/// allowed to resolve to zero active providers (e.g. every entry has <c>IsActive: false</c>, or the
/// "ValidationAgent" section is empty/absent). That is a supported, deliberate opt-out reported as
/// AGT-001 by the caller, not an error. A genuinely missing file is a distinct setup problem and is
/// surfaced as an engine error instead of being silently treated the same as "no active provider".
/// </remarks>
public static class AgentClientFactoryBuilder
{
    public static AgentClientFactory? Build(string repositoryRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repositoryRoot);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(repositoryRoot)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();

        var validationAgentOptions = new ValidationAgentOptions();
        configuration.GetSection(ValidationAgentOptions.SectionName).Bind(validationAgentOptions);

        var activeValidationProviders = validationAgentOptions.GetActiveByPurpose(AgentPurpose.Validation);
        var activeExplainProviders = validationAgentOptions.GetActiveByPurpose(AgentPurpose.Explain);
        if (activeValidationProviders.Count == 0 && activeExplainProviders.Count == 0)
        {
            return null;
        }

        var services = new ServiceCollection();
        services.AddSingleton(validationAgentOptions);
        RegisterProviderDependencies(services, [.. activeValidationProviders, .. activeExplainProviders]);

        return new AgentClientFactory(validationAgentOptions, services.BuildServiceProvider());
    }

    // Secrets (API keys) are never stored in appsettings.json — each is resolved from an
    // environment variable keyed by the provider's Name.
    //
    // Every active Azure OpenAI provider gets its own AzureOpenAIClient (distinct providers can
    // point at different endpoints/credentials), keyed by provider Name so AgentClientFactory can
    // resolve the correct client per provider instead of all providers sharing a single client.
    private static void RegisterProviderDependencies(ServiceCollection services, IReadOnlyList<ModelProviderOptions> activeProviders)
    {
        var azureOpenAiClientsByProviderName = new Dictionary<string, AzureOpenAIClient>(StringComparer.Ordinal);

        foreach (var provider in activeProviders)
        {
            if (!string.Equals(provider.Type, ProviderTypes.AzureOpenAI, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(provider.Endpoint) ||
                azureOpenAiClientsByProviderName.ContainsKey(provider.Name))
            {
                continue;
            }

            azureOpenAiClientsByProviderName[provider.Name] = CreateAzureOpenAiClient(provider);
        }

        services.AddSingleton<IReadOnlyDictionary<string, AzureOpenAIClient>>(azureOpenAiClientsByProviderName);
    }

    private static AzureOpenAIClient CreateAzureOpenAiClient(ModelProviderOptions provider)
    {
        var apiKey = Environment.GetEnvironmentVariable($"{provider.Name}_API_KEY");
        var endpoint = new Uri(provider.Endpoint!);

        return string.IsNullOrWhiteSpace(apiKey)
            ? new AzureOpenAIClient(endpoint, new DefaultAzureCredential())
            : new AzureOpenAIClient(endpoint, new AzureKeyCredential(apiKey));
    }
}
