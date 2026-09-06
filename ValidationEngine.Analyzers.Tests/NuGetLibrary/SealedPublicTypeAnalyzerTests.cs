using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.NuGetLibrary.Tests;

public class SealedPublicTypeAnalyzerTests
{
    private static CSharpAnalyzerTest<SealedPublicTypeAnalyzer, DefaultVerifier> CreateTest(string source)
    {
        var test = new CSharpAnalyzerTest<SealedPublicTypeAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.TestState.Sources.Add(("Tests.cs", source));
        return test;
    }

    [Fact]
    public async Task WhenPublicClassIsNotSealedThenNuGet001IsReported()
    {
        string source = @"
namespace TestApp
{
    public class {|#0:OrderService|}
    {
    }
}
";

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(SealedPublicTypeAnalyzer.Rule)
                .WithLocation(0)
                .WithArguments("OrderService"));

        await test.RunAsync();
    }

    [Fact]
    public async Task WhenPublicClassIsSealedThenNoDiagnosticIsReported()
    {
        string source = @"
namespace TestApp
{
    public sealed class OrderService
    {
    }
}
";

        var test = CreateTest(source);
        await test.RunAsync();
    }

    [Fact]
    public async Task WhenPublicClassIsAbstractThenNoDiagnosticIsReported()
    {
        string source = @"
namespace TestApp
{
    public abstract class OrderServiceBase
    {
    }
}
";

        var test = CreateTest(source);
        await test.RunAsync();
    }

    [Fact]
    public async Task WhenPublicClassIsStaticThenNoDiagnosticIsReported()
    {
        string source = @"
namespace TestApp
{
    public static class OrderHelpers
    {
    }
}
";

        var test = CreateTest(source);
        await test.RunAsync();
    }

    [Fact]
    public async Task WhenClassIsInternalThenNoDiagnosticIsReported()
    {
        string source = @"
namespace TestApp
{
    internal class OrderService
    {
    }
}
";

        var test = CreateTest(source);
        await test.RunAsync();
    }
}
