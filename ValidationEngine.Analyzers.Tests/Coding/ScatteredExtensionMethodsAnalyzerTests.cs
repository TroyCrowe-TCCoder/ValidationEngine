using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Coding.Tests
{
    public class ScatteredExtensionMethodsAnalyzerTests
    {
        private static CSharpAnalyzerTest<ScatteredExtensionMethodsAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            return new CSharpAnalyzerTest<ScatteredExtensionMethodsAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };
        }

        [Fact]
        public async Task WhenExtensionMethodsForSameTypeAreSplitAcrossClassesThenCode012IsReported()
        {
            const string source = @"
namespace TestApp
{
    public class Client
    {
        public string Name { get; set; }
    }

    public static class ClientExtensions
    {
        public static string {|#1:ToDisplayName|}(this Client client) => client.Name;
    }

    public static class ClientHelpers
    {
        public static string {|#0:ToUpperName|}(this Client client) => client.Name.ToUpperInvariant();
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(ScatteredExtensionMethodsAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("ToUpperName", "Client", "ClientExtensions, ClientHelpers"));
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(ScatteredExtensionMethodsAnalyzer.Rule)
                    .WithLocation(1)
                    .WithArguments("ToDisplayName", "Client", "ClientExtensions, ClientHelpers"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenExtensionMethodsForSameTypeAreInOneClassThenNoDiagnosticIsReported()
        {
            const string source = @"
namespace TestApp
{
    public class Client
    {
        public string Name { get; set; }
    }

    public static class ClientExtensions
    {
        public static string ToDisplayName(this Client client) => client.Name;
        public static string ToUpperName(this Client client) => client.Name.ToUpperInvariant();
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
