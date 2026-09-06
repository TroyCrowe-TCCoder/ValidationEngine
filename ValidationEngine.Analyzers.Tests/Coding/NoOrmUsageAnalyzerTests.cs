using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Coding.Tests
{
    public class NoOrmUsageAnalyzerTests
    {
        private static CSharpAnalyzerTest<NoOrmUsageAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            var test = new CSharpAnalyzerTest<NoOrmUsageAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.AdditionalReferences.Add(typeof(Microsoft.EntityFrameworkCore.DbContext).Assembly);

            return test;
        }

        [Fact]
        public async Task WhenClassDerivesFromDbContextThenCode005IsReported()
        {
            const string source = @"
using Microsoft.EntityFrameworkCore;

namespace TestApp
{
    public class {|#0:ExpenseDbContext|} : DbContext
    {
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(NoOrmUsageAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("ExpenseDbContext"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenClassDoesNotDeriveFromDbContextThenNoDiagnosticIsReported()
        {
            const string source = @"
namespace TestApp
{
    public class ExpenseRepository
    {
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
