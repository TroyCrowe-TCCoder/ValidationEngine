using System.Net;
using ValidationEngine.Link;
using ValidationEngine.Link.Models;

namespace ValidationEngine.Link.Tests;

public class ExternalLinkValidatorTests
{
    [Fact]
    public async Task ValidateAsync_SuccessfulHeadResponse_ReturnsNull()
    {
        var handler = new StubHttpMessageHandler((request, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var validator = new ExternalLinkValidator(new HttpClient(handler));

        var issue = await validator.ValidateAsync(BuildReference("https://example.com"), "run-1", "TestApp", CancellationToken.None);

        Assert.Null(issue);
    }

    [Fact]
    public async Task ValidateAsync_NotFoundResponse_ReturnsIssueWithStatusCode()
    {
        var handler = new StubHttpMessageHandler((request, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var validator = new ExternalLinkValidator(new HttpClient(handler));

        var issue = await validator.ValidateAsync(BuildReference("https://example.com/missing"), "run-1", "TestApp", CancellationToken.None);

        Assert.NotNull(issue);
        Assert.Equal(404, issue!.HttpStatusCode);
        Assert.Equal(LinkType.External, issue.LinkType);
    }

    [Fact]
    public async Task ValidateAsync_HeadNotAllowed_FallsBackToGet()
    {
        var callCount = 0;
        var handler = new StubHttpMessageHandler((request, _) =>
        {
            callCount++;
            if (request.Method == HttpMethod.Head)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.MethodNotAllowed));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        var validator = new ExternalLinkValidator(new HttpClient(handler));

        var issue = await validator.ValidateAsync(BuildReference("https://example.com"), "run-1", "TestApp", CancellationToken.None);

        Assert.Null(issue);
        Assert.Equal(2, callCount);
    }

    [Fact]
    public async Task ValidateAsync_MalformedUrl_ReturnsIssue()
    {
        var handler = new StubHttpMessageHandler((request, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        var validator = new ExternalLinkValidator(new HttpClient(handler));

        var issue = await validator.ValidateAsync(BuildReference("https://"), "run-1", "TestApp", CancellationToken.None);

        Assert.NotNull(issue);
        Assert.Contains("Malformed", issue!.Issue, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("https://dev.azure.com/tcrowe0170")]
    [InlineData("https://dev.azure.com/tcrowe0170/SomeProject/_git/SomeRepo")]
    [InlineData("https://contoso.visualstudio.com")]
    [InlineData("https://contoso.visualstudio.com/DefaultCollection/SomeProject")]
    public async Task ValidateAsync_AzureDevOpsHost_ReturnsNullWithoutSendingRequest(string target)
    {
        var requestSent = false;
        var handler = new StubHttpMessageHandler((request, _) =>
        {
            requestSent = true;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        });
        var validator = new ExternalLinkValidator(new HttpClient(handler));

        var issue = await validator.ValidateAsync(BuildReference(target), "run-1", "TestApp", CancellationToken.None);

        Assert.Null(issue);
        Assert.False(requestSent);
    }

    private static LinkReference BuildReference(string target) => new()
    {
        SourceFile = "Docs/Source.md",
        RawText = target,
        Target = target,
        LineNumber = 1
    };

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            handler(request, cancellationToken);
    }
}
