using System.Collections.Concurrent;
using ValidationEngine.Link.Models;

namespace ValidationEngine.Link;

public sealed class LinkValidationRunner(
    IInternalLinkValidator internalLinkValidator,
    IExternalLinkValidator externalLinkValidator)
{
    public async Task<IReadOnlyList<LinkIssue>> RunAsync(
        IReadOnlyList<string> filePaths,
        string repositoryRoot,
        string runId,
        string app,
        int concurrency,
        CancellationToken cancellationToken)
    {
        var issues = new ConcurrentBag<LinkIssue>();

        await Parallel.ForEachAsync(
            filePaths,
            new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, concurrency), CancellationToken = cancellationToken },
            async (relativePath, token) =>
            {
                var absolutePath = Path.Combine(repositoryRoot, relativePath);
                if (!File.Exists(absolutePath))
                {
                    return;
                }

                var content = await File.ReadAllTextAsync(absolutePath, token);
                var references = MarkdownLinkExtractor.Extract(relativePath, content);

                foreach (var reference in references)
                {
                    var linkType = LinkClassifier.Classify(reference.Target);

                    if (linkType == LinkType.Internal)
                    {
                        var issue = internalLinkValidator.Validate(reference, repositoryRoot, runId, app);
                        if (issue is not null)
                        {
                            issues.Add(issue);
                        }
                    }
                    else
                    {
                        var issue = await externalLinkValidator.ValidateAsync(reference, runId, app, token);
                        if (issue is not null)
                        {
                            issues.Add(issue);
                        }
                    }
                }
            });

        return issues.ToList();
    }
}
