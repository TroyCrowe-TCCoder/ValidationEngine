using System.Text.Json;
using System.Text.Json.Serialization;
using ValidationEngine.Models;

namespace ValidationEngine.Reporting;

/// <summary>
/// Default plug-in implementation of <see cref="IJsonReportRenderer"/>. Renders a
/// <see cref="ValidationReport"/> as structured JSON. This is the machine-readable counterpart
/// to <see cref="MarkdownReportRenderer"/>, intended for consumption by AI agents and tooling
/// that need to programmatically read and act on validation results (e.g., an automated fix
/// loop), while the Markdown report remains the human-readable form for PR review. Consumers
/// reference this concrete type explicitly to opt into it; the core engine only depends on
/// the interface.
/// </summary>
public sealed class JsonReportRenderer : IJsonReportRenderer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    string IJsonReportRenderer.Render(ValidationReport report, int exitCode) => Render(report, exitCode);

    public static string Render(ValidationReport report, int exitCode)
    {
        ArgumentNullException.ThrowIfNull(report);

        var document = new ValidationReportDocument(report, exitCode);

        return JsonSerializer.Serialize(document, SerializerOptions);
    }
}
