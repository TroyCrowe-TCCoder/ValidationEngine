namespace ValidationEngine.Link.Models;

public enum LinkType
{
    Internal,
    External
}

/// <summary>
/// A validated link failure, emitted for reporting. Only failures are recorded — passing links
/// produce no output, mirroring the ValidationEngine convention of reporting findings, not
/// successes.
/// </summary>
public sealed record LinkIssue
{
    public required string RunId { get; init; }

    public required string App { get; init; }

    public required string SourceFile { get; init; }

    public required string Link { get; init; }

    public required LinkType LinkType { get; init; }

    public int? HttpStatusCode { get; init; }

    public required string Issue { get; init; }

    public required DateTimeOffset ValidationDate { get; init; }
}
