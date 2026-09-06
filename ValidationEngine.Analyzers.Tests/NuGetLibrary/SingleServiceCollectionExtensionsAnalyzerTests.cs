using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.NuGetLibrary.Tests;

public class SingleServiceCollectionExtensionsAnalyzerTests
{
    private static CSharpAnalyzerTest<SingleServiceCollectionExtensionsAnalyzer, DefaultVerifier> CreateTest(string source)
    {
        var test = new CSharpAnalyzerTest<SingleServiceCollectionExtensionsAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.TestState.Sources.Add(("Tests.cs", source));
        return test;
    }

    [Fact]
    public async Task WhenSingleServiceCollectionExtensionsClassExistsThenNoDiagnosticIsReported()
    {
        string source = @"
namespace TestApp
{
    public static class ServiceCollectionExtensions
    {
    }
}
";

        var test = CreateTest(source);
        await test.RunAsync();
    }

    [Fact]
    public async Task WhenMultipleServiceCollectionExtensionsClassesExistThenNuGet003IsReportedOnExtras()
    {
        string source = @"
namespace TestApp.One
{
    public static class ServiceCollectionExtensions
    {
    }
}

namespace TestApp.Two
{
    public static class {|#0:ServiceCollectionExtensions|}
    {
    }
}
";

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(SingleServiceCollectionExtensionsAnalyzer.Rule)
                .WithLocation(0)
                .WithArguments("ServiceCollectionExtensions"));

        await test.RunAsync();
    }
}
