using System.Text.Json;
using Azure.AI.OpenAI;
using OpenAI.Chat;
using ValidationEngine.Agent.Configuration;

namespace ValidationEngine.Agent.Providers;

/// <summary>
/// First concrete provider (AGT-003), using the "gpt-5.5" deployment in the
/// "Single Source Management" subscription.
/// </summary>
public sealed class AzureOpenAIValidationAgentClient : IValidationAgentClient
{
    private static readonly BinaryData ResponseJsonSchema = BinaryData.FromBytes("""
        {
            "type": "object",
            "properties": {
                "outcome": { "type": "string", "enum": ["Pass", "Violation", "Uncertain"] },
                "explanation": { "type": "string" },
                "suggestedFinding": { "type": "string" }
            },
            "required": ["outcome", "explanation", "suggestedFinding"],
            "additionalProperties": false
        }
        """u8.ToArray());

    public string Purpose => AgentPurpose.Validation;

    public string Name => _options.Name;

    private readonly ModelProviderOptions _options;
    private readonly AzureOpenAIClient _client;

    public AzureOpenAIValidationAgentClient(ModelProviderOptions options, AzureOpenAIClient client)
    {
        _options = options;
        _client = client;
    }

    public async Task<ModelValidationResponse> EvaluateAsync(ModelValidationRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var chatClient = _client.GetChatClient(_options.DeploymentName);

        var systemMessage = new SystemChatMessage(
            "You are a standards-compliance validation agent. You are given a single standards rule and " +
            "the content of a single changed file. Decide whether the file violates the rule. " +
            "Respond with \"Pass\" if the rule does not apply or is satisfied, \"Violation\" if the rule is " +
            "clearly broken, or \"Uncertain\" if you cannot determine compliance with confidence. " +
            "Always include a concise explanation.");

        var userMessage = new UserChatMessage(
            $"""
            Rule ID: {request.RuleId}
            Rule: {request.RuleText}

            File: {request.FilePath}
            File content:
            {request.FileContent}
            """);

        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: "validation_result",
                jsonSchema: ResponseJsonSchema,
                jsonSchemaIsStrict: true)
        };

        ChatCompletion completion = await chatClient.CompleteChatAsync([systemMessage, userMessage], options, ct);

        using var document = JsonDocument.Parse(completion.Content[0].Text);
        var root = document.RootElement;

        var outcomeText = root.GetProperty("outcome").GetString();
        var outcome = Enum.TryParse<ValidationOutcome>(outcomeText, ignoreCase: true, out var parsedOutcome)
            ? parsedOutcome
            : ValidationOutcome.Uncertain;

        var explanation = root.GetProperty("explanation").GetString();
        var suggestedFinding = root.TryGetProperty("suggestedFinding", out var suggestedFindingElement)
            ? suggestedFindingElement.GetString()
            : null;

        return new ModelValidationResponse(outcome, explanation, suggestedFinding);
    }
}
