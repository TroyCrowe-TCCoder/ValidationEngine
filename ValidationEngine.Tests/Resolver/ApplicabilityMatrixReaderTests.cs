using ValidationEngine.Resolver;

namespace ValidationEngine.Tests.Resolver;

public sealed class ApplicabilityMatrixReaderTests : IDisposable
{
    private readonly string _fixtureFilePath;

    public ApplicabilityMatrixReaderTests()
    {
        _fixtureFilePath = Path.Combine(Path.GetTempPath(), $"ApplicabilityMatrix_{Guid.NewGuid():N}.csv");
    }

    [Fact]
    public void Read_WhenCsvContainsQuotedNotesField_ParsesRemainingFieldsCorrectly()
    {
        var csvContent = "StandardFile,MarkerBase,WebAppWebApi,Database,ClassLibrary,Notes\r\n"
            + "GlobalDatabaseStandards,database,N/A,Applies,N/A,\"Notes, with a comma inside quotes\"\r\n";
        File.WriteAllText(_fixtureFilePath, csvContent);

        var entries = ApplicabilityMatrixReader.Read(_fixtureFilePath);

        var entry = Assert.Single(entries);
        Assert.Equal("GlobalDatabaseStandards", entry.StandardFile);
        Assert.Equal("database", entry.MarkerBase);
        Assert.Equal(ApplicabilityStatus.NotApplicable, entry.WebAppWebApi);
        Assert.Equal(ApplicabilityStatus.Applies, entry.Database);
        Assert.Equal(ApplicabilityStatus.NotApplicable, entry.ClassLibrary);
    }

    [Fact]
    public void Read_WhenCsvContainsConditionalStatus_ParsesConditionalStatus()
    {
        var csvContent = "StandardFile,MarkerBase,WebAppWebApi,Database,ClassLibrary,Notes\r\n"
            + "GlobalTypeScriptStandards,typescript,Conditional,N/A,N/A,Some note\r\n";
        File.WriteAllText(_fixtureFilePath, csvContent);

        var entries = ApplicabilityMatrixReader.Read(_fixtureFilePath);

        var entry = Assert.Single(entries);
        Assert.Equal(ApplicabilityStatus.Conditional, entry.WebAppWebApi);
    }

    public void Dispose()
    {
        if (File.Exists(_fixtureFilePath))
        {
            File.Delete(_fixtureFilePath);
        }
    }
}
