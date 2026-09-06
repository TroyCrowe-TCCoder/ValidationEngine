using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Logging.Tests
{
    public class ProgramLoggingDestinationAnalyzerTests
    {
        private static CSharpAnalyzerTest<ProgramLoggingDestinationAnalyzer, DefaultVerifier> CreateTest(
            string programSource,
            string? otherSource = null)
        {
            var test = new CSharpAnalyzerTest<ProgramLoggingDestinationAnalyzer, DefaultVerifier>
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.Sources.Add(("Program.cs", programSource));

            if (otherSource is not null)
            {
                test.TestState.Sources.Add(("OtherFile.cs", otherSource));
            }

            return test;
        }

        [Fact]
        public async Task WhenProgramCsHasNoApplicationInsightsRegistrationThenLog004IsReported()
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
                new DiagnosticResult(ProgramLoggingDestinationAnalyzer.Rule)
                    .WithArguments("Program.cs"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenProgramCsRegistersApplicationInsightsThenNoDiagnosticIsReported()
        {
            const string programSource = @"
namespace TestApp
{
    public static class ServiceCollectionExtensions
    {
        public static void AddApplicationInsightsTelemetry(this object services)
        {
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var services = new object();
            services.AddApplicationInsightsTelemetry();
        }
    }
}
";

            var test = CreateTest(programSource);

            await test.RunAsync();
        }
    }
}
