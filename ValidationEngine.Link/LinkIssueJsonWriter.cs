using System.Text.Json;
using ValidationEngine.Link.Models;

namespace ValidationEngine.Link;

/// <summary>
/// Default plug-in implementation of <see cref="ILinkIssueWriter"/>. Consumers reference this
/// concrete type explicitly to opt into it; nothing else in ValidationEngine.Link depends on it.
/// </summary>
public sealed class LinkIssueJsonWriter : ILinkIssueWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    Task ILinkIssueWriter.WriteAsync(string outputPath, IReadOnlyList<LinkIssue> issues, CancellationToken cancellationToken)
        => WriteAsync(outputPath, issues, cancellationToken);

    public static async Task WriteAsync(string outputPath, IReadOnlyList<LinkIssue> issues, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(outputPath);
        await JsonSerializer.SerializeAsync(stream, issues, SerializerOptions, cancellationToken);
    }
}
