using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Database.Tests;

public class CommandTypeMustBeStoredProcedureAnalyzerTests
{
    private static CSharpAnalyzerTest<CommandTypeMustBeStoredProcedureAnalyzer, DefaultVerifier> CreateTest(string source)
    {
        var test = new CSharpAnalyzerTest<CommandTypeMustBeStoredProcedureAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.TestState.Sources.Add(("Repository.cs", source));
        return test;
    }

    [Fact]
    public async Task WhenCommandTypeIsSetToTextThenDb001IsReported()
    {
        string source = SqlStubs.SqlCommandStub + @"
namespace TestApp
{
    public class InvoiceRepository
    {
        public void GetInvoices()
        {
            var command = new System.Data.SqlClient.SqlCommand(""Invoice_Get"");
            {|#0:command.CommandType = System.Data.CommandType.Text|};
        }
    }
}
";

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(CommandTypeMustBeStoredProcedureAnalyzer.Rule)
                .WithLocation(0));

        await test.RunAsync();
    }

    [Fact]
    public async Task WhenCommandTypeIsSetToStoredProcedureThenNoDiagnosticIsReported()
    {
        string source = SqlStubs.SqlCommandStub + @"
namespace TestApp
{
    public class InvoiceRepository
    {
        public void GetInvoices()
        {
            var command = new System.Data.SqlClient.SqlCommand(""Invoice_Get"");
            command.CommandType = System.Data.CommandType.StoredProcedure;
        }
    }
}
";

        var test = CreateTest(source);
        await test.RunAsync();
    }
}
