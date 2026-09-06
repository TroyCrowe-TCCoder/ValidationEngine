using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Logging.Tests
{
    public class NoProviderSpecificLoggingApiAnalyzerTests
    {
        private static CSharpAnalyzerTest<NoProviderSpecificLoggingApiAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            var test = new CSharpAnalyzerTest<NoProviderSpecificLoggingApiAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.AdditionalReferences.Add(typeof(Microsoft.ApplicationInsights.TelemetryClient).Assembly);
            test.TestState.AdditionalReferences.Add(typeof(Microsoft.Extensions.Logging.ILogger<>).Assembly);

            return test;
        }

        [Fact]
        public async Task WhenTelemetryClientTrackTraceIsCalledThenLog002IsReported()
        {
            const string source = @"
using Microsoft.ApplicationInsights;

namespace TestApp
{
    public class ExpenseService
    {
        private readonly TelemetryClient _telemetryClient;

        public ExpenseService(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
        }

        public void Process()
        {
            _telemetryClient.{|#0:TrackTrace|}(""Processing"");
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(NoProviderSpecificLoggingApiAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("Process", "TrackTrace"));

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
