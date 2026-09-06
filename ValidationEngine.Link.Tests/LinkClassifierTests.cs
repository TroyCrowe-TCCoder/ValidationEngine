using ValidationEngine.Link;
using ValidationEngine.Link.Models;

namespace ValidationEngine.Link.Tests;

public class LinkClassifierTests
{
    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://example.com")]
    [InlineData("HTTPS://Example.com")]
    public void Classify_HttpSchemes_AreExternal(string target)
    {
        Assert.Equal(LinkType.External, LinkClassifier.Classify(target));
    }

    [Theory]
    [InlineData("./Docs/Foo.md")]
    [InlineData("Foo.md")]
    [InlineData("#some-heading")]
    [InlineData("../Foo.md#anchor")]
    public void Classify_RelativeAndAnchorTargets_AreInternal(string target)
    {
        Assert.Equal(LinkType.Internal, LinkClassifier.Classify(target));
    }

    [Fact]
    public void Classify_MailtoScheme_IsExternal()
    {
        Assert.Equal(LinkType.External, LinkClassifier.Classify("mailto:someone@example.com"));
    }

    [Fact]
    public void Classify_WindowsDriveLetterPath_IsInternal()
    {
        Assert.Equal(LinkType.Internal, LinkClassifier.Classify(@"C:\repo\Docs\Foo.md"));
    }
}
