using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Coding.Tests
{
    public class ControllerDirectRepositoryAccessAnalyzerTests
    {
        private static CSharpAnalyzerTest<ControllerDirectRepositoryAccessAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            return new CSharpAnalyzerTest<ControllerDirectRepositoryAccessAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };
        }

        [Fact]
        public async Task WhenControllerHasRepositoryFieldThenCode002IsReported()
        {
            const string source = @"
namespace TestApp
{
    public class ExpenseRepository
    {
    }

    public class ExpenseController
    {
        private readonly {|#0:ExpenseRepository|} _expenseRepository;
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(ControllerDirectRepositoryAccessAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("ExpenseController", "ExpenseRepository"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenControllerHasRepositoryConstructorParameterThenCode002IsReported()
        {
            const string source = @"
namespace TestApp
{
    public class ExpenseRepository
    {
    }

    public class ExpenseController
    {
        public ExpenseController({|#0:ExpenseRepository|} expenseRepository)
        {
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(ControllerDirectRepositoryAccessAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("ExpenseController", "ExpenseRepository"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenControllerReferencesServiceOnlyThenNoDiagnosticIsReported()
        {
            const string source = @"
namespace TestApp
{
    public class ExpenseService
    {
    }

    public class ExpenseController
    {
        private readonly ExpenseService _expenseService;

        public ExpenseController(ExpenseService expenseService)
        {
            _expenseService = expenseService;
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
