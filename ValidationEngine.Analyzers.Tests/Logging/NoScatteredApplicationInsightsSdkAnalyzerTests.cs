using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Logging.Tests
{
    public class NoScatteredApplicationInsightsSdkAnalyzerTests
    {
        private static CSharpAnalyzerTest<NoScatteredApplicationInsightsSdkAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            var test = new CSharpAnalyzerTest<NoScatteredApplicationInsightsSdkAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.AdditionalReferences.Add(typeof(Microsoft.ApplicationInsights.TelemetryClient).Assembly);

            return test;
        }

        [Fact]
        public async Task WhenTelemetryClientIsReferencedInServiceClassThenLog003IsReported()
        {
            const string source = @"
using Microsoft.ApplicationInsights;

namespace TestApp
{
    public class ExpenseService
    {
        private readonly {|#0:TelemetryClient|} _telemetryClient;

        public ExpenseService(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(NoScatteredApplicationInsightsSdkAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("TelemetryClient", "ExpenseService"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenTelemetryClientIsReferencedInInfrastructureClassThenNoDiagnosticIsReported()
        {
            const string source = @"
using Microsoft.ApplicationInsights;

namespace TestApp.Infrastructure
{
    public class TelemetryInfrastructure
    {
        private readonly TelemetryClient _telemetryClient;

        public TelemetryInfrastructure(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
