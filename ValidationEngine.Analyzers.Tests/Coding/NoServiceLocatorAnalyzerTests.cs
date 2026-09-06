using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Coding.Tests
{
    public class NoServiceLocatorAnalyzerTests
    {
        private static CSharpAnalyzerTest<NoServiceLocatorAnalyzer, DefaultVerifier> CreateTest(
            string source,
            string fileName)
        {
            var test = new CSharpAnalyzerTest<NoServiceLocatorAnalyzer, DefaultVerifier>
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.Sources.Add((fileName, source));
            test.TestState.AdditionalReferences.Add(
                typeof(Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions).Assembly);

            return test;
        }

        [Fact]
        public async Task WhenServiceResolvesDependencyViaGetServiceThenCode015IsReported()
        {
            const string source = @"
using System;

namespace TestApp
{
    public interface IExpenseRepository
    {
    }

    public class ExpenseService
    {
        public void Process(IServiceProvider serviceProvider)
        {
            {|#0:serviceProvider.GetService(typeof(IExpenseRepository))|};
        }
    }
}
";

            var test = CreateTest(source, "ExpenseService.cs");
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(NoServiceLocatorAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("ExpenseService", "GetService"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenServiceResolvesDependencyViaGetRequiredServiceThenCode015IsReported()
        {
            const string source = @"
using System;
using Microsoft.Extensions.DependencyInjection;

namespace TestApp
{
    public interface IExpenseRepository
    {
    }

    public class ExpenseService
    {
        public void Process(IServiceProvider serviceProvider)
        {
            {|#0:serviceProvider.GetRequiredService<IExpenseRepository>()|};
        }
    }
}
";

            var test = CreateTest(source, "ExpenseService.cs");
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(NoServiceLocatorAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("ExpenseService", "GetRequiredService"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenGetRequiredServiceIsCalledFromProgramCsThenNoDiagnosticIsReported()
        {
            const string source = @"
using System;
using Microsoft.Extensions.DependencyInjection;

namespace TestApp
{
    public interface IExpenseRepository
    {
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            IServiceProvider serviceProvider = null;
            serviceProvider.GetRequiredService<IExpenseRepository>();
        }
    }
}
";

            var test = CreateTest(source, "Program.cs");

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenDependencyIsInjectedViaConstructorThenNoDiagnosticIsReported()
        {
            const string source = @"
namespace TestApp
{
    public interface IExpenseRepository
    {
    }

    public class ExpenseService
    {
        private readonly IExpenseRepository _repository;

        public ExpenseService(IExpenseRepository repository)
        {
            _repository = repository;
        }
    }
}
";

            var test = CreateTest(source, "ExpenseService.cs");

            await test.RunAsync();
        }
    }
}
