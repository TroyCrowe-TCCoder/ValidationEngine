using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Coding.Tests
{
    public class NonDescriptiveNamingAnalyzerTests
    {
        private static CSharpAnalyzerTest<NonDescriptiveNamingAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            return new CSharpAnalyzerTest<NonDescriptiveNamingAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };
        }

        [Fact]
        public async Task WhenParameterNameIsAbbreviatedThenCode006IsReported()
        {
            const string source = @"
namespace TestApp
{
    public class ExpenseService
    {
        public void Process(int {|#0:tmp|})
        {
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(NonDescriptiveNamingAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("Parameter", "tmp"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenVariableNameIsTooShortThenCode006IsReported()
        {
            const string source = @"
namespace TestApp
{
    public class ExpenseService
    {
        public void Process()
        {
            var {|#0:ab|} = 1;
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(NonDescriptiveNamingAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("Variable", "ab"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenLoopCounterIsUsedThenNoDiagnosticIsReported()
        {
            const string source = @"
namespace TestApp
{
    public class ExpenseService
    {
        public void Process()
        {
            for (int i = 0; i < 10; i++)
            {
            }
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenLambdaParameterIsShortThenNoDiagnosticIsReported()
        {
            const string source = @"
using System;

namespace TestApp
{
    public class ExpenseService
    {
        public void Process()
        {
            Func<int, int> square = x => x * x;
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenNameIsDescriptiveThenNoDiagnosticIsReported()
        {
            const string source = @"
namespace TestApp
{
    public class ExpenseService
    {
        public void ProcessExpenseReport(int clientId)
        {
            var expenseTotal = clientId;
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
