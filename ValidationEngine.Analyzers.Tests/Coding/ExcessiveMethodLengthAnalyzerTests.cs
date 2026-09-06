using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Coding.Tests
{
    public class ExcessiveMethodLengthAnalyzerTests
    {
        private static CSharpAnalyzerTest<ExcessiveMethodLengthAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            return new CSharpAnalyzerTest<ExcessiveMethodLengthAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };
        }

        private static string BuildLongMethodBody(int statementCount)
        {
            return string.Join("\n            ", Enumerable.Range(0, statementCount).Select(i => $"var value{i} = {i};"));
        }

        [Fact]
        public async Task WhenMethodExceedsLineThresholdThenCode007IsReported()
        {
            string longBody = BuildLongMethodBody(45);

            string source = $@"
namespace TestApp
{{
    public class ExpenseService
    {{
        public void {{|#0:ProcessExpenses|}}()
        {{
            {longBody}
        }}
    }}
}}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(ExcessiveMethodLengthAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("ProcessExpenses", 47, 40));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenMethodIsShortThenNoDiagnosticIsReported()
        {
            const string source = @"
namespace TestApp
{
    public class ExpenseService
    {
        public void ProcessExpenses()
        {
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
