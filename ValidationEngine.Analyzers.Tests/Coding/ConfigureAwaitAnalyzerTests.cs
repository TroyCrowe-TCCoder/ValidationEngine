using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Coding.Tests
{
    public class ConfigureAwaitAnalyzerTests
    {
        private static CSharpAnalyzerTest<ConfigureAwaitAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            return new CSharpAnalyzerTest<ConfigureAwaitAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };
        }

        [Fact]
        public async Task WhenAwaitDoesNotCallConfigureAwaitThenCode023IsReported()
        {
            const string source = @"
using System.Threading.Tasks;

namespace TestApp
{
    public class InvoiceService
    {
        public async Task ProcessAsync()
        {
            {|#0:await Task.Delay(1)|};
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(ConfigureAwaitAnalyzer.Rule)
                    .WithLocation(0));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenAwaitCallsConfigureAwaitFalseThenNoDiagnosticIsReported()
        {
            const string source = @"
using System.Threading.Tasks;

namespace TestApp
{
    public class InvoiceService
    {
        public async Task ProcessAsync()
        {
            await Task.Delay(1).ConfigureAwait(false);
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
