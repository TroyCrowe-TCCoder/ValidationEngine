using ValidationEngine.Infrastructure;

namespace ValidationEngine.Tests.Infrastructure;

public sealed class ExclusionAddendumReaderTests : IDisposable
{
    private readonly string _fixtureRoot;

    public ExclusionAddendumReaderTests()
    {
        _fixtureRoot = Path.Combine(Path.GetTempPath(), $"ExclusionAddendumReaderTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_fixtureRoot);
    }

    [Fact]
    public void Read_SkipEntry_ExcludesMarker()
    {
        WriteAddendum("RepositoryStandards.md", """
            # Repository Standards

            ### Skip: repository.4.6

            **Marker ID:** repository.4.6
            **Reason:** Not applicable.
            **Requester:** Test
            **Approver:** Test
            **Effective Date:** 2026-01-01
            **Planned Review Date:** 2099-01-01
            """);

        var result = ExclusionAddendumReader.Read(_fixtureRoot);

        Assert.Contains("repository.4.6", result.ExcludedMarkers);
    }

    [Fact]
    public void Read_DeviationEntry_ExcludesOverriddenMarker()
    {
        // Regression test: a Deviation addendum entry (Overridden Rule + Marker ID, no "Skip:"
        // heading) must suppress the overridden global rule the same way a Skip/exclusion entry
        // does. See GlobalFileSpecificationStandards.md Section 2.15.
        WriteAddendum("RepositoryStandards.md", """
            # Repository Standards

            ### 2.1 Validation-Only `dev` to `main`

            - **Overridden Rule:** GlobalRepositoryStandards.md Section 4.6
            - **Marker ID:** repository.4.6
            - **Deviation:** `dev -> main` is validation-only for this repository.
            - **Requirement:** Reviewer approval is not required.
            - **Reason:** Documentation-only repository.
            - **Requester:** Test
            - **Approver:** Test
            - **Effective Date:** 2026-01-01
            - **Planned Review Date:** 2099-01-01
            """);

        var result = ExclusionAddendumReader.Read(_fixtureRoot);

        Assert.Contains("repository.4.6", result.ExcludedMarkers);
    }

    [Fact]
    public void Read_DeviationEntryWithMultipleMarkerIds_ExcludesAllOverriddenMarkers()
    {
        WriteAddendum("RepositoryStandards.md", """
            # Repository Standards

            ### 2.1 Validation-Only `dev` to `main`

            - **Overridden Rule:** GlobalRepositoryStandards.md Sections 4.6, 5, and 7
            - **Marker ID:** repository.4.6
            - **Marker ID:** repository.5
            - **Marker ID:** repository.7
            - **Deviation:** `dev -> main` is validation-only for this repository.
            - **Effective Date:** 2026-01-01
            - **Planned Review Date:** 2099-01-01
            """);

        var result = ExclusionAddendumReader.Read(_fixtureRoot);

        Assert.Contains("repository.4.6", result.ExcludedMarkers);
        Assert.Contains("repository.5", result.ExcludedMarkers);
        Assert.Contains("repository.7", result.ExcludedMarkers);
    }

    private void WriteAddendum(string fileName, string content)
    {
        var standardsDirectory = Path.Combine(_fixtureRoot, "Standards");
        Directory.CreateDirectory(standardsDirectory);
        File.WriteAllText(Path.Combine(standardsDirectory, fileName), content);
    }

    public void Dispose()
    {
        if (Directory.Exists(_fixtureRoot))
        {
            Directory.Delete(_fixtureRoot, recursive: true);
        }
    }
}
