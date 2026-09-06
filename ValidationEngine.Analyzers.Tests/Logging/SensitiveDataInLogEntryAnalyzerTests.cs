using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Logging.Tests
{
    public class SensitiveDataInLogEntryAnalyzerTests
    {
        private static CSharpAnalyzerTest<SensitiveDataInLogEntryAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            var test = new CSharpAnalyzerTest<SensitiveDataInLogEntryAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.AdditionalReferences.Add(typeof(Microsoft.Extensions.Logging.ILogger<>).Assembly);

            return test;
        }

        [Fact]
        public async Task WhenMessageTemplateHasPasswordPlaceholderThenLog015IsReported()
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

        public void Process(string password)
        {
            _logger.LogInformation({|#0:""User authenticated. {Password}""|}, password);
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(SensitiveDataInLogEntryAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("Process", "Password"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenMessageTemplateHasOnlySafeFieldsThenNoDiagnosticIsReported()
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

        public void Process(int userId, int tenantId)
        {
            _logger.LogInformation(""User authenticated. {UserId} {TenantId}"", userId, tenantId);
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
