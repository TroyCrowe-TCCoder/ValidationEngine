namespace ValidationEngine.Agent.Providers;

/// <summary>
/// Azure OpenAI provider for the Explain purpose (AGT-012). Produces a human-readable
/// rationale (and optional doc snippet) for an already-determined finding, without
/// re-deciding Pass/Violation/Uncertain.
/// </summary>
public sealed class AzureOpenAIExplainAgentClient : IExplainAgentClient
{
    public string Purpose => AgentPurpose.Explain;

    public string Name => _options.Name;

    private readonly Configuration.ModelProviderOptions _options;
    private readonly Azure.AI.OpenAI.AzureOpenAIClient _client;

    public AzureOpenAIExplainAgentClient(Configuration.ModelProviderOptions options, Azure.AI.OpenAI.AzureOpenAIClient client)
    {
        _options = options;
        _client = client;
    }

    public async Task<ModelExplainResponse> ExplainAsync(ModelExplainRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var chatClient = _client.GetChatClient(_options.DeploymentName);

        var systemMessage = new OpenAI.Chat.SystemChatMessage(
            "You are a standards-compliance explanation agent. You are given a rule and a finding " +
            "that has already been determined to violate that rule. Produce a concise, human-readable " +
            "rationale explaining why this matters, suitable for a pull request comment or onboarding " +
            "material. Do not re-evaluate whether the rule was violated. Optionally include a short " +
            "documentation snippet illustrating the correct pattern.");

        var userMessage = new OpenAI.Chat.UserChatMessage(
            $"""
            Rule ID: {request.RuleId}
            Rule: {request.RuleText}

            File: {request.FilePath}
            Violation: {request.ViolationMessage}
            """);

        var options = new OpenAI.Chat.ChatCompletionOptions
        {
            ResponseFormat = OpenAI.Chat.ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: "explain_result",
                jsonSchema: ResponseJsonSchema,
                jsonSchemaIsStrict: true)
        };

        OpenAI.Chat.ChatCompletion completion = await chatClient.CompleteChatAsync([systemMessage, userMessage], options, ct);

        using var document = System.Text.Json.JsonDocument.Parse(completion.Content[0].Text);
        var root = document.RootElement;

        var rationale = root.GetProperty("rationale").GetString() ?? string.Empty;
        var docSnippet = root.TryGetProperty("docSnippet", out var docSnippetElement)
            ? docSnippetElement.GetString()
            : null;

        return new ModelExplainResponse(rationale, docSnippet);
    }

    private static readonly BinaryData ResponseJsonSchema = BinaryData.FromBytes("""
        {
            "type": "object",
            "properties": {
                "rationale": { "type": "string" },
                "docSnippet": { "type": "string" }
            },
            "required": ["rationale", "docSnippet"],
            "additionalProperties": false
        }
        """u8.ToArray());
}
