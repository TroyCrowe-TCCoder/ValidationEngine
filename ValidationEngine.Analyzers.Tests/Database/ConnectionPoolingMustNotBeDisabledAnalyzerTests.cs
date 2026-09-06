using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Database.Tests;

public class ConnectionPoolingMustNotBeDisabledAnalyzerTests
{
    private static CSharpAnalyzerTest<ConnectionPoolingMustNotBeDisabledAnalyzer, DefaultVerifier> CreateTest(string source)
    {
        var test = new CSharpAnalyzerTest<ConnectionPoolingMustNotBeDisabledAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.TestState.Sources.Add(("Repository.cs", source));
        return test;
    }

    [Fact]
    public async Task WhenConnectionStringDisablesPoolingThenDb004IsReported()
    {
        string source = SqlStubs.SqlCommandStub + @"
namespace TestApp
{
    public class InvoiceRepository
    {
        public void Connect()
        {
            var connection = new System.Data.SqlClient.SqlConnection({|#0:""Server=.;Database=Invoices;Pooling=false;""|});
        }
    }
}
";

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(ConnectionPoolingMustNotBeDisabledAnalyzer.Rule)
                .WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task WhenConnectionStringDoesNotDisablePoolingThenNoDiagnosticIsReported()
    {
        string source = SqlStubs.SqlCommandStub + @"
namespace TestApp
{
    public class InvoiceRepository
    {
        public void Connect()
        {
            var connection = new System.Data.SqlClient.SqlConnection(""Server=.;Database=Invoices;"");
        }
    }
}
";

        var test = CreateTest(source);
        await test.RunAsync();
    }
}
