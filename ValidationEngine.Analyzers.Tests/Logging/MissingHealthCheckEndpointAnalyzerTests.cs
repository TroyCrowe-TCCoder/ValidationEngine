using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Logging.Tests
{
    public class MissingHealthCheckEndpointAnalyzerTests
    {
        private static CSharpAnalyzerTest<MissingHealthCheckEndpointAnalyzer, DefaultVerifier> CreateTest(string programSource)
        {
            var test = new CSharpAnalyzerTest<MissingHealthCheckEndpointAnalyzer, DefaultVerifier>
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.Sources.Add(("Program.cs", programSource));

            return test;
        }

        [Fact]
        public async Task WhenProgramCsDoesNotMapHealthChecksThenLog014IsReported()
        {
            const string programSource = @"
namespace TestApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = new object();
        }
    }
}
";

            var test = CreateTest(programSource);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(MissingHealthCheckEndpointAnalyzer.Rule)
                    .WithArguments("Program.cs"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenProgramCsMapsHealthChecksThenNoDiagnosticIsReported()
        {
            const string programSource = @"
namespace TestApp
{
    public class HealthCheckResult
    {
        public static HealthCheckResult Healthy() => new HealthCheckResult();
    }

    public static class AppExtensions
    {
        public static void MapHealthChecks(this object app, string pattern)
        {
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var app = new object();
            app.MapHealthChecks(""/healthcheck"");
        }
    }
}
";

            var test = CreateTest(programSource);

            await test.RunAsync();
        }
    }
}
