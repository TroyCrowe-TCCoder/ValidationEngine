using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.NuGetLibrary.Tests;

public class OptionsBindingAnalyzerTests
{
    private static CSharpAnalyzerTest<OptionsBindingAnalyzer, DefaultVerifier> CreateTest(string source)
    {
        var test = new CSharpAnalyzerTest<OptionsBindingAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.TestState.Sources.Add(("Tests.cs", source));
        return test;
    }

    [Fact]
    public async Task WhenOptionsTypeHasMethodWithBodyThenNuGet004IsReported()
    {
        string source = @"
namespace TestApp
{
    public sealed class {|#0:CacheOptions|}
    {
        public int TimeoutSeconds { get; set; }

        public void Validate()
        {
        }
    }
}
";

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(OptionsBindingAnalyzer.Rule)
                .WithLocation(0)
                .WithArguments("CacheOptions"));

        await test.RunAsync();
    }

    [Fact]
    public async Task WhenOptionsTypeHasOnlyPropertiesThenNoDiagnosticIsReported()
    {
        string source = @"
namespace TestApp
{
    public sealed class CacheOptions
    {
        public int TimeoutSeconds { get; set; }
    }
}
";

        var test = CreateTest(source);
        await test.RunAsync();
    }
}
