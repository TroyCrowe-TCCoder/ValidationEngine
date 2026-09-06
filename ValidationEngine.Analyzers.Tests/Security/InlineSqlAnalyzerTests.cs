using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Security.Tests
{
    public class InlineSqlAnalyzerTests
    {
        private static CSharpAnalyzerTest<InlineSqlAnalyzer, DefaultVerifier> CreateTest(
            string source,
            string fileName)
        {
            var test = new CSharpAnalyzerTest<InlineSqlAnalyzer, DefaultVerifier>
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.Sources.Add((fileName, source));

            return test;
        }

        private const string SqlCommandStub = @"
namespace System.Data.SqlClient
{
    public class SqlCommand
    {
        public SqlCommand(string commandText)
        {
        }

        public SqlCommand()
        {
        }

        public string CommandText { get; set; }
    }
}
";

        private const string DapperStub = @"
namespace TestApp
{
    public class SqlConnection
    {
        public System.Threading.Tasks.Task<int> QueryAsync(string sql, object parameters)
            => System.Threading.Tasks.Task.FromResult(0);
    }
}
";

        [Fact]
        public async Task WhenSqlCommandIsConstructedWithInlineSqlThenSec005IsReported()
        {
            string source = SqlCommandStub + @"
namespace TestApp
{
    public class InvoiceRepository
    {
        public void GetInvoices()
        {
            var command = new System.Data.SqlClient.SqlCommand({|#0:""SELECT * FROM Invoices""|});
        }
    }
}
";

            var test = CreateTest(source, "InvoiceRepository.cs");
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(InlineSqlAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("SqlCommand"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenCommandTextIsAssignedInlineSqlThenSec005IsReported()
        {
            string source = SqlCommandStub + @"
namespace TestApp
{
    public class InvoiceRepository
    {
        public void GetInvoices()
        {
            var command = new System.Data.SqlClient.SqlCommand();
            command.CommandText = {|#0:""SELECT * FROM Invoices""|};
        }
    }
}
";

            var test = CreateTest(source, "InvoiceRepository.cs");
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(InlineSqlAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("CommandText"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenDapperQueryAsyncIsCalledWithInlineSqlVariableThenSec005IsReported()
        {
            string source = DapperStub + @"
namespace TestApp
{
    public class InvoiceRepository
    {
        public async System.Threading.Tasks.Task GetInvoicesAsync()
        {
            var connection = new SqlConnection();
            var clientId = 1;
            var sql = {|#0:""SELECT * FROM Invoices WHERE ClientId = @clientId""|};
            await connection.QueryAsync(sql, new { clientId });
        }
    }
}
";

            var test = CreateTest(source, "InvoiceRepository.cs");
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(InlineSqlAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("QueryAsync"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenRepositoryCallsStoredProcedureThenNoDiagnosticIsReported()
        {
            const string source = @"
namespace TestApp
{
    public interface IInvoiceRepository
    {
        System.Threading.Tasks.Task GetInvoicesByClientAsync(int clientId, System.Threading.CancellationToken cancellationToken);
    }

    public class InvoiceService
    {
        private readonly IInvoiceRepository _repository;

        public InvoiceService(IInvoiceRepository repository)
        {
            _repository = repository;
        }

        public System.Threading.Tasks.Task GetInvoicesAsync(int clientId, System.Threading.CancellationToken cancellationToken)
            => _repository.GetInvoicesByClientAsync(clientId, cancellationToken);
    }
}
";

            var test = CreateTest(source, "InvoiceService.cs");

            await test.RunAsync();
        }
    }
}
