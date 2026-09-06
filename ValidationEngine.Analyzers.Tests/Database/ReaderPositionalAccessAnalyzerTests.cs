using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Database.Tests;

public class ReaderPositionalAccessAnalyzerTests
{
    private static CSharpAnalyzerTest<ReaderPositionalAccessAnalyzer, DefaultVerifier> CreateTest(string source)
    {
        var test = new CSharpAnalyzerTest<ReaderPositionalAccessAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.TestState.Sources.Add(("Model.cs", source));
        return test;
    }

    [Fact]
    public async Task WhenIndexerIsAccessedWithLiteralIndexThenDb003IsReported()
    {
        string source = SqlStubs.SqlDataReaderStub + @"
namespace TestApp
{
    public class InvoiceListItem
    {
        public InvoiceListItem(System.Data.SqlClient.SqlDataReader reader)
        {
            var id = {|#0:reader[0]|};
        }
    }
}
";

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(ReaderPositionalAccessAnalyzer.Rule)
                .WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task WhenGetInt32IsCalledWithLiteralIndexThenDb003IsReported()
    {
        string source = SqlStubs.SqlDataReaderStub + @"
namespace TestApp
{
    public class InvoiceListItem
    {
        public InvoiceListItem(System.Data.SqlClient.SqlDataReader reader)
        {
            var id = {|#0:reader.GetInt32(0)|};
        }
    }
}
";

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(ReaderPositionalAccessAnalyzer.Rule)
                .WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task WhenGetOrdinalIsUsedThenNoDiagnosticIsReported()
    {
        string source = SqlStubs.SqlDataReaderStub + @"
namespace TestApp
{
    public class InvoiceListItem
    {
        public InvoiceListItem(System.Data.SqlClient.SqlDataReader reader)
        {
            var idOrdinal = reader.GetOrdinal(""Id"");
            var id = reader.GetInt32(idOrdinal);
        }
    }
}
";

        var test = CreateTest(source);
        await test.RunAsync();
    }
}
