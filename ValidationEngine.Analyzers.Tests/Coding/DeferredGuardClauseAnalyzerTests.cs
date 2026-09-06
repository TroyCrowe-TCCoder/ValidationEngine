using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Coding.Tests
{
    public class DeferredGuardClauseAnalyzerTests
    {
        private static CSharpAnalyzerTest<DeferredGuardClauseAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            return new CSharpAnalyzerTest<DeferredGuardClauseAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };
        }

        [Fact]
        public async Task WhenGuardClauseAppearsAfterOtherStatementsThenCode008IsReported()
        {
            const string source = @"
using System;

namespace TestApp
{
    public class ExpenseService
    {
        public void Process(object invoice)
        {
            var total = 0;
            {|#0:if (invoice == null)
            {
                throw new ArgumentNullException(nameof(invoice));
            }|}
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(DeferredGuardClauseAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("Process"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenGuardClauseIsAtTopOfMethodThenNoDiagnosticIsReported()
        {
            const string source = @"
using System;

namespace TestApp
{
    public class ExpenseService
    {
        public void Process(object invoice)
        {
            if (invoice == null)
            {
                throw new ArgumentNullException(nameof(invoice));
            }

            var total = 0;
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
