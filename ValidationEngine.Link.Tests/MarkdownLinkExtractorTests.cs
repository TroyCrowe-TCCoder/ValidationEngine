using ValidationEngine.Link;

namespace ValidationEngine.Link.Tests;

public class MarkdownLinkExtractorTests
{
    [Fact]
    public void Extract_FindsInlineLink()
    {
        var content = "See [the docs](./Docs/Foo.md) for details.";

        var references = MarkdownLinkExtractor.Extract("Readme.md", content);

        var reference = Assert.Single(references);
        Assert.Equal("./Docs/Foo.md", reference.Target);
        Assert.Equal(1, reference.LineNumber);
    }

    [Fact]
    public void Extract_FindsAnchorOnlyLink()
    {
        var content = "See [section](#some-heading) below.";

        var references = MarkdownLinkExtractor.Extract("Readme.md", content);

        var reference = Assert.Single(references);
        Assert.Equal("#some-heading", reference.Target);
    }

    [Fact]
    public void Extract_FindsBareAutolink()
    {
        var content = "Visit <https://example.com/page> now.";

        var references = MarkdownLinkExtractor.Extract("Readme.md", content);

        var reference = Assert.Single(references);
        Assert.Equal("https://example.com/page", reference.Target);
    }

    [Fact]
    public void Extract_FindsRawUrl()
    {
        var content = "Raw link: https://example.com/raw";

        var references = MarkdownLinkExtractor.Extract("Readme.md", content);

        var reference = Assert.Single(references);
        Assert.Equal("https://example.com/raw", reference.Target);
    }

    [Fact]
    public void Extract_TracksLineNumberAcrossMultipleLines()
    {
        var content = "Line one has no links.\nLine two has [a link](./target.md).";

        var references = MarkdownLinkExtractor.Extract("Readme.md", content);

        var reference = Assert.Single(references);
        Assert.Equal(2, reference.LineNumber);
    }
}
