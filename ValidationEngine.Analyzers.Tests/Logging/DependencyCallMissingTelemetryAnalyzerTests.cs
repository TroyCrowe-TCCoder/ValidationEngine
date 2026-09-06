using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Logging.Tests
{
    public class DependencyCallMissingTelemetryAnalyzerTests
    {
        private static CSharpAnalyzerTest<DependencyCallMissingTelemetryAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            var test = new CSharpAnalyzerTest<DependencyCallMissingTelemetryAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.AdditionalReferences.Add(typeof(Microsoft.Extensions.Logging.ILogger<>).Assembly);

            return test;
        }

        [Fact]
        public async Task WhenDependencyCallHasNoLoggingThenLog013IsReported()
        {
            const string source = @"
using System.Net.Http;
using System.Threading.Tasks;

namespace TestApp
{
    public class ExpenseService
    {
        private readonly HttpClient _httpClient;

        public ExpenseService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task ProcessAsync()
        {
            await _httpClient.{|#0:GetAsync|}(""https://example.com"");
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(DependencyCallMissingTelemetryAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("ProcessAsync", "GetAsync"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenDependencyCallIsAccompaniedByLoggingThenNoDiagnosticIsReported()
        {
            const string source = @"
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace TestApp
{
    public class ExpenseService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ExpenseService> _logger;

        public ExpenseService(HttpClient httpClient, ILogger<ExpenseService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task ProcessAsync()
        {
            var response = await _httpClient.GetAsync(""https://example.com"");
            _logger.LogInformation(""Dependency call completed. {StatusCode}"", response.StatusCode);
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
