using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Database.Tests;

public class NullParameterMustUseDbNullAnalyzerTests
{
    private static CSharpAnalyzerTest<NullParameterMustUseDbNullAnalyzer, DefaultVerifier> CreateTest(string source)
    {
        var test = new CSharpAnalyzerTest<NullParameterMustUseDbNullAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.TestState.Sources.Add(("Repository.cs", source));
        return test;
    }

    [Fact]
    public async Task WhenNullLiteralIsPassedToAddWithValueThenDb002IsReported()
    {
        string source = SqlStubs.SqlCommandStub + @"
namespace TestApp
{
    public class InvoiceRepository
    {
        public void GetInvoices()
        {
            var command = new System.Data.SqlClient.SqlCommand(""Invoice_Get"");
            {|#0:command.Parameters.AddWithValue(""@ClientId"", null)|};
        }
    }
}
";

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(NullParameterMustUseDbNullAnalyzer.Rule)
                .WithLocation(0)
                .WithArguments("@ClientId"));

        await test.RunAsync();
    }

    [Fact]
    public async Task WhenDbNullValueIsPassedThenNoDiagnosticIsReported()
    {
        string source = SqlStubs.SqlCommandStub + @"
namespace TestApp
{
    public class InvoiceRepository
    {
        public void GetInvoices()
        {
            var command = new System.Data.SqlClient.SqlCommand(""Invoice_Get"");
            command.Parameters.AddWithValue(""@ClientId"", System.DBNull.Value);
        }
    }
}
";

        var test = CreateTest(source);
        await test.RunAsync();
    }
}
