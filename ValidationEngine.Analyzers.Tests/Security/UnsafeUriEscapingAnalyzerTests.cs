using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Security.Tests
{
    public class UnsafeUriEscapingAnalyzerTests
    {
        private static CSharpAnalyzerTest<UnsafeUriEscapingAnalyzer, DefaultVerifier> CreateTest(
            string source,
            string fileName)
        {
            var test = new CSharpAnalyzerTest<UnsafeUriEscapingAnalyzer, DefaultVerifier>
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.Sources.Add((fileName, source));

            return test;
        }

        [Fact]
        public async Task WhenEscapeUriStringIsCalledThenSec006IsReported()
        {
            const string source = @"
namespace TestApp
{
    public class InvoiceService
    {
        public string BuildFilter(string filter)
        {
            return {|#0:System.Uri.EscapeUriString(filter)|};
        }
    }
}
";

            var test = CreateTest(source, "InvoiceService.cs");
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(UnsafeUriEscapingAnalyzer.Rule)
                    .WithLocation(0));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenEscapeDataStringIsCalledThenNoDiagnosticIsReported()
        {
            const string source = @"
namespace TestApp
{
    public class InvoiceService
    {
        public string BuildFilter(string filter)
        {
            return System.Uri.EscapeDataString(filter);
        }
    }
}
";

            var test = CreateTest(source, "InvoiceService.cs");

            await test.RunAsync();
        }
    }
}
