using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Coding.Tests
{
    public class WrapperOnlyInterfaceAnalyzerTests
    {
        private static CSharpAnalyzerTest<WrapperOnlyInterfaceAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            return new CSharpAnalyzerTest<WrapperOnlyInterfaceAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };
        }

        [Fact]
        public async Task WhenInterfaceHasSingleImplementationAndNoConstraintUsageThenCode003IsReported()
        {
            const string source = @"
namespace TestApp
{
    public interface {|#0:IExpenseFormatter|}
    {
        string Format();
    }

    public class ExpenseFormatter : IExpenseFormatter
    {
        public string Format() => string.Empty;
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(WrapperOnlyInterfaceAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("IExpenseFormatter", "ExpenseFormatter"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenInterfaceHasMultipleImplementationsThenNoDiagnosticIsReported()
        {
            const string source = @"
namespace TestApp
{
    public interface IExpenseFormatter
    {
        string Format();
    }

    public class CsvExpenseFormatter : IExpenseFormatter
    {
        public string Format() => string.Empty;
    }

    public class JsonExpenseFormatter : IExpenseFormatter
    {
        public string Format() => string.Empty;
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenInterfaceIsUsedAsGenericConstraintThenNoDiagnosticIsReported()
        {
            const string source = @"
namespace TestApp
{
    public interface IExpenseFormatter
    {
        string Format();
    }

    public class ExpenseFormatter : IExpenseFormatter
    {
        public string Format() => string.Empty;
    }

    public class FormatterHost<T> where T : IExpenseFormatter
    {
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
