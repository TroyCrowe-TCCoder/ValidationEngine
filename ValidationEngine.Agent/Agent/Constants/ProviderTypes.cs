namespace ValidationEngine.Agent.Constants;

/// <summary>
/// Named provider <c>Type</c> values used in <see cref="ValidationEngine.Agent.Configuration.ModelProviderOptions.Type"/>.
/// Defined once here per GlobalCodingStandards.md coding.3.14 (Constants and Magic Values) since the value
/// is used both when dispatching a client type and when configuring its dependencies.
/// </summary>
public static class ProviderTypes
{
    public const string AzureOpenAI = "AzureOpenAI";
}
