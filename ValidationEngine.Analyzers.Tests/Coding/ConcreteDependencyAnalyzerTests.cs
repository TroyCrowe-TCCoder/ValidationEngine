using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Coding.Tests
{
    public class ConcreteDependencyAnalyzerTests
    {
        private static CSharpAnalyzerTest<ConcreteDependencyAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            return new CSharpAnalyzerTest<ConcreteDependencyAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };
        }

        [Fact]
        public async Task WhenServiceConstructorDependsOnConcreteRepositoryAndInterfaceExistsThenCode014IsReported()
        {
            const string source = @"
namespace TestApp
{
    public interface IExpenseRepository
    {
    }

    public class ExpenseRepository : IExpenseRepository
    {
    }

    public class ExpenseService
    {
        public ExpenseService({|#0:ExpenseRepository|} repository)
        {
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(ConcreteDependencyAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("ExpenseService", "ExpenseRepository", "IExpenseRepository"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenServiceConstructorDependsOnInterfaceAbstractionThenNoDiagnosticIsReported()
        {
            const string source = @"
namespace TestApp
{
    public interface IExpenseRepository
    {
    }

    public class ExpenseRepository : IExpenseRepository
    {
    }

    public class ExpenseService
    {
        public ExpenseService(IExpenseRepository repository)
        {
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenNoMatchingInterfaceIsDeclaredThenNoDiagnosticIsReported()
        {
            const string source = @"
namespace TestApp
{
    public class ExpenseRepository
    {
    }

    public class ExpenseService
    {
        public ExpenseService(ExpenseRepository repository)
        {
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
