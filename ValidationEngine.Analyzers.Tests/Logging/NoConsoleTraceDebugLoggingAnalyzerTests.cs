using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Logging.Tests
{
    public class NoConsoleTraceDebugLoggingAnalyzerTests
    {
        private static CSharpAnalyzerTest<NoConsoleTraceDebugLoggingAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            var test = new CSharpAnalyzerTest<NoConsoleTraceDebugLoggingAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.AdditionalReferences.Add(typeof(Microsoft.Extensions.Logging.ILogger<>).Assembly);

            return test;
        }

        [Fact]
        public async Task WhenConsoleWriteLineIsCalledThenLog001IsReported()
        {
            const string source = @"
namespace TestApp
{
    public class ExpenseService
    {
        public void Process()
        {
            {|#0:System.Console.WriteLine|}(""Processing"");
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(NoConsoleTraceDebugLoggingAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("Console", "WriteLine"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenILoggerIsUsedThenNoDiagnosticIsReported()
        {
            const string source = @"
using Microsoft.Extensions.Logging;

namespace TestApp
{
    public class ExpenseService
    {
        private readonly ILogger<ExpenseService> _logger;

        public ExpenseService(ILogger<ExpenseService> logger)
        {
            _logger = logger;
        }

        public void Process()
        {
            _logger.LogInformation(""Processing"");
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
