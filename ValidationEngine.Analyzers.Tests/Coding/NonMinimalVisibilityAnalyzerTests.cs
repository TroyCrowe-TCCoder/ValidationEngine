using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Coding.Tests
{
    public class NonMinimalVisibilityAnalyzerTests
    {
        private static CSharpAnalyzerTest<NonMinimalVisibilityAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            return new CSharpAnalyzerTest<NonMinimalVisibilityAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };
        }

        [Fact]
        public async Task WhenPublicMethodDeclaredInsideInternalClassThenCode011IsReported()
        {
            const string source = @"
namespace TestApp
{
    internal class ExpenseCalculator
    {
        public decimal {|#0:Calculate|}()
        {
            return 0m;
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(NonMinimalVisibilityAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("Calculate", "ExpenseCalculator"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenPublicMethodImplementsInterfaceMemberThenNoDiagnosticIsReported()
        {
            const string source = @"
namespace TestApp
{
    public interface IExpenseCalculator
    {
        decimal Calculate();
    }

    internal class ExpenseCalculator : IExpenseCalculator
    {
        public decimal Calculate()
        {
            return 0m;
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenMethodDeclaredInsidePublicClassThenNoDiagnosticIsReported()
        {
            const string source = @"
namespace TestApp
{
    public class ExpenseCalculator
    {
        public decimal Calculate()
        {
            return 0m;
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
