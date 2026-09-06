namespace ValidationEngine.Agent.Configuration;

public sealed class ModelProviderOptions
{
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Purpose { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public string? Endpoint { get; init; }
    public string? DeploymentName { get; init; }
    public string? ApiVersion { get; init; }
}
