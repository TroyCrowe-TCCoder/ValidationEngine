using ValidationEngine.Link.Models;

namespace ValidationEngine.Link;

/// <summary>
/// Contract for persisting <see cref="LinkIssue"/> results to an external sink (file, database,
/// telemetry pipeline, etc.). No layer assumes <see cref="LinkIssueJsonWriter"/> is "the" writer;
/// each consumer app is free to plug in its own implementation.
/// </summary>
public interface ILinkIssueWriter
{
    Task WriteAsync(string outputPath, IReadOnlyList<LinkIssue> issues, CancellationToken cancellationToken);
}
