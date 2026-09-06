namespace ValidationEngine.Link.Models;

/// <summary>
/// A single link occurrence parsed out of a Markdown file, prior to classification/validation.
/// </summary>
public sealed record LinkReference
{
    public required string SourceFile { get; init; }

    public required string RawText { get; init; }

    public required string Target { get; init; }

    public required int LineNumber { get; init; }
}
