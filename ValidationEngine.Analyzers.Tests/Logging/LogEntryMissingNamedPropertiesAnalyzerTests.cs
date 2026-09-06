using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Logging.Tests
{
    public class LogEntryMissingNamedPropertiesAnalyzerTests
    {
        private static CSharpAnalyzerTest<LogEntryMissingNamedPropertiesAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            var test = new CSharpAnalyzerTest<LogEntryMissingNamedPropertiesAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.AdditionalReferences.Add(typeof(Microsoft.Extensions.Logging.ILogger<>).Assembly);

            return test;
        }

        [Fact]
        public async Task WhenInformationLogHasNoNamedPlaceholdersThenLog007IsReported()
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
            _logger.{|#0:LogInformation|}(""Processing started"");
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(LogEntryMissingNamedPropertiesAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("Process", "LogInformation"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenInformationLogHasNamedPlaceholdersThenNoDiagnosticIsReported()
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

        public void Process(int documentId)
        {
            _logger.LogInformation(""Document uploaded. {DocumentId}"", documentId);
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
