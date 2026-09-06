using Azure.AI.OpenAI;
using Microsoft.Extensions.DependencyInjection;
using ValidationEngine.Agent.Configuration;
using ValidationEngine.Agent.Constants;
using ValidationEngine.Agent.Providers;

namespace ValidationEngine.Agent;

/// <summary>
/// Purpose-aware client factory (AGT-004). Resolves active providers per purpose
/// and instantiates the correct client type based on <see cref="ModelProviderOptions.Type"/>.
/// </summary>
public sealed class AgentClientFactory
{
    private readonly ValidationAgentOptions _options;
    private readonly IServiceProvider _services;

    public AgentClientFactory(ValidationAgentOptions options, IServiceProvider services)
    {
        _options = options;
        _services = services;
    }

    public IReadOnlyList<IValidationAgentClient> CreateActiveValidationClients() =>
        _options.GetActiveByPurpose(AgentPurpose.Validation)
            .Select(CreateValidationClient)
            .ToList();

    public IReadOnlyList<IExplainAgentClient> CreateActiveExplainClients() =>
        _options.GetActiveByPurpose(AgentPurpose.Explain)
            .Select(CreateExplainClient)
            .ToList();

    private IValidationAgentClient CreateValidationClient(ModelProviderOptions provider) => provider.Type switch
    {
        ProviderTypes.AzureOpenAI => new AzureOpenAIValidationAgentClient(provider, ResolveAzureOpenAiClient(provider)),
        _ => throw new NotSupportedException($"Provider type '{provider.Type}' not supported for Validation.")
    };

    private IExplainAgentClient CreateExplainClient(ModelProviderOptions provider) => provider.Type switch
    {
        ProviderTypes.AzureOpenAI => new AzureOpenAIExplainAgentClient(provider, ResolveAzureOpenAiClient(provider)),
        _ => throw new NotSupportedException($"Provider type '{provider.Type}' not supported for Explain.")
    };

    // Each active Azure OpenAI provider is registered with its own client, keyed by provider
    // Name (see AgentClientFactoryBuilder.RegisterProviderDependencies), so multiple concurrently
    // active Azure OpenAI providers (e.g. different endpoints/deployments per purpose) each get
    // their own client rather than silently sharing one.
    private AzureOpenAIClient ResolveAzureOpenAiClient(ModelProviderOptions provider)
    {
        var clientsByName = _services.GetRequiredService<IReadOnlyDictionary<string, AzureOpenAIClient>>();
        return clientsByName.TryGetValue(provider.Name, out var client)
            ? client
            : throw new InvalidOperationException($"No AzureOpenAIClient was registered for provider '{provider.Name}'.");
    }

    // No Fix-purpose client/factory method exists — see AGT-010 (deferred indefinitely;
    // AgentPurpose.Fix itself was removed).
}
