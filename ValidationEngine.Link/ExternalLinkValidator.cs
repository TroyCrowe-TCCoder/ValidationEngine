using System.Net;
using ValidationEngine.Link.Models;

namespace ValidationEngine.Link;

public interface IExternalLinkValidator
{
    Task<LinkIssue?> ValidateAsync(LinkReference reference, string runId, string app, CancellationToken cancellationToken);
}

/// <summary>
/// Validates external (http/https) links by issuing an HTTP request. Prefers HEAD to avoid
/// downloading full response bodies, falling back to GET when the server doesn't support HEAD
/// (405) or when HEAD itself fails unexpectedly. Redirects are followed automatically by
/// <see cref="HttpClient"/>; the final status code is what gets classified.
/// </summary>
public sealed class ExternalLinkValidator(HttpClient httpClient) : IExternalLinkValidator
{
    // Azure DevOps organization/project URLs are excluded from validation: they require
    // authentication to resolve (routinely return 400/401 for anonymous requests), and they
    // are not meaningful documentation or in-app navigation links — only application URLs,
    // standards-document links, and public external websites need to be validated. Covers both
    // the modern (dev.azure.com/<org>) and legacy (<org>.visualstudio.com) URL formats.
    private static bool IsExcludedHost(string host) =>
        host.Equals("dev.azure.com", StringComparison.OrdinalIgnoreCase) ||
        host.EndsWith(".visualstudio.com", StringComparison.OrdinalIgnoreCase);

    public async Task<LinkIssue?> ValidateAsync(
        LinkReference reference, string runId, string app, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(reference.Target, UriKind.Absolute, out var uri))
        {
            return BuildIssue(reference, runId, app, null, "Malformed external URL.");
        }

        if (IsExcludedHost(uri.Host))
        {
            return null;
        }

        try
        {
            var response = await SendAsync(uri, HttpMethod.Head, cancellationToken);

            if (response.StatusCode == HttpStatusCode.MethodNotAllowed || response.StatusCode == HttpStatusCode.NotImplemented)
            {
                response.Dispose();
                response = await SendAsync(uri, HttpMethod.Get, cancellationToken);
            }

            using (response)
            {
                if ((int)response.StatusCode is >= 200 and < 400)
                {
                    return null;
                }

                return BuildIssue(reference, runId, app, (int)response.StatusCode, $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
            }
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return BuildIssue(reference, runId, app, null, "Request timed out.");
        }
        catch (HttpRequestException httpRequestException)
        {
            return BuildIssue(reference, runId, app, null, $"Request failed: {httpRequestException.Message}");
        }
    }

    private async Task<HttpResponseMessage> SendAsync(Uri uri, HttpMethod method, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, uri);
        return await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }

    private static LinkIssue BuildIssue(LinkReference reference, string runId, string app, int? statusCode, string issue) => new()
    {
        RunId = runId,
        App = app,
        SourceFile = reference.SourceFile,
        Link = reference.Target,
        LinkType = LinkType.External,
        HttpStatusCode = statusCode,
        Issue = issue,
        ValidationDate = DateTimeOffset.UtcNow
    };
}
