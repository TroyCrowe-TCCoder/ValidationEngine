using ValidationEngine.Resolver;

namespace ValidationEngine.Tests.Resolver;

public sealed class ApplicableStandardsResolverTests
{
    [Fact]
    public void Resolve_WhenStatusIsAppliesForAppType_IncludesMarkerBase()
    {
        var repositoryProfile = new RepositoryProfile { AppType = AppType.WebAppWebApi, HasSpaClient = false, SpaClientUsesReact = false };
        var applicabilityEntries = new List<ApplicabilityEntry>
        {
            new()
            {
                StandardFile = "GlobalCodingStandards",
                MarkerBase = "coding",
                WebAppWebApi = ApplicabilityStatus.Applies,
                Database = ApplicabilityStatus.Applies,
                ClassLibrary = ApplicabilityStatus.Applies
            }
        };

        var applicableMarkerBases = ApplicableStandardsResolver.Resolve(repositoryProfile, applicabilityEntries);

        Assert.Contains("coding", applicableMarkerBases);
    }

    [Fact]
    public void Resolve_WhenStatusIsNotApplicableForAppType_ExcludesMarkerBase()
    {
        var repositoryProfile = new RepositoryProfile { AppType = AppType.WebAppWebApi, HasSpaClient = false, SpaClientUsesReact = false };
        var applicabilityEntries = new List<ApplicabilityEntry>
        {
            new()
            {
                StandardFile = "GlobalDatabaseStandards",
                MarkerBase = "database",
                WebAppWebApi = ApplicabilityStatus.NotApplicable,
                Database = ApplicabilityStatus.Applies,
                ClassLibrary = ApplicabilityStatus.NotApplicable
            }
        };

        var applicableMarkerBases = ApplicableStandardsResolver.Resolve(repositoryProfile, applicabilityEntries);

        Assert.DoesNotContain("database", applicableMarkerBases);
    }

    [Fact]
    public void Resolve_WhenConditionalTypeScriptStandardAndNoSpaClientPresent_ExcludesMarkerBase()
    {
        var repositoryProfile = new RepositoryProfile { AppType = AppType.WebAppWebApi, HasSpaClient = false, SpaClientUsesReact = false };
        var applicabilityEntries = new List<ApplicabilityEntry>
        {
            new()
            {
                StandardFile = "GlobalTypeScriptStandards",
                MarkerBase = "typescript",
                WebAppWebApi = ApplicabilityStatus.Conditional,
                Database = ApplicabilityStatus.NotApplicable,
                ClassLibrary = ApplicabilityStatus.NotApplicable
            }
        };

        var applicableMarkerBases = ApplicableStandardsResolver.Resolve(repositoryProfile, applicabilityEntries);

        Assert.DoesNotContain("typescript", applicableMarkerBases);
    }

    [Fact]
    public void Resolve_WhenConditionalReactStandardAndSpaClientUsesReact_IncludesMarkerBase()
    {
        var repositoryProfile = new RepositoryProfile { AppType = AppType.WebAppWebApi, HasSpaClient = true, SpaClientUsesReact = true };
        var applicabilityEntries = new List<ApplicabilityEntry>
        {
            new()
            {
                StandardFile = "GlobalReactProjectStandards",
                MarkerBase = "react-project",
                WebAppWebApi = ApplicabilityStatus.Conditional,
                Database = ApplicabilityStatus.NotApplicable,
                ClassLibrary = ApplicabilityStatus.NotApplicable
            }
        };

        var applicableMarkerBases = ApplicableStandardsResolver.Resolve(repositoryProfile, applicabilityEntries);

        Assert.Contains("react-project", applicableMarkerBases);
    }

    [Fact]
    public void Resolve_WhenConditionalReactStandardAndSpaClientDoesNotUseReact_ExcludesMarkerBase()
    {
        var repositoryProfile = new RepositoryProfile { AppType = AppType.WebAppWebApi, HasSpaClient = true, SpaClientUsesReact = false };
        var applicabilityEntries = new List<ApplicabilityEntry>
        {
            new()
            {
                StandardFile = "GlobalReactProjectStandards",
                MarkerBase = "react-project",
                WebAppWebApi = ApplicabilityStatus.Conditional,
                Database = ApplicabilityStatus.NotApplicable,
                ClassLibrary = ApplicabilityStatus.NotApplicable
            }
        };

        var applicableMarkerBases = ApplicableStandardsResolver.Resolve(repositoryProfile, applicabilityEntries);

        Assert.DoesNotContain("react-project", applicableMarkerBases);
    }
}
