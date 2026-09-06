using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.NuGetLibrary.Tests;

public class ServiceInterfaceRequiredAnalyzerTests
{
    private static CSharpAnalyzerTest<ServiceInterfaceRequiredAnalyzer, DefaultVerifier> CreateTest(string source)
    {
        var test = new CSharpAnalyzerTest<ServiceInterfaceRequiredAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.TestState.Sources.Add(("Tests.cs", source));
        return test;
    }

    [Fact]
    public async Task WhenServiceClassDoesNotImplementPublicInterfaceThenNuGet002IsReported()
    {
        string source = @"
namespace TestApp
{
    public sealed class {|#0:OrderService|}
    {
    }
}
";

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(ServiceInterfaceRequiredAnalyzer.Rule)
                .WithLocation(0)
                .WithArguments("OrderService"));

        await test.RunAsync();
    }

    [Fact]
    public async Task WhenServiceClassImplementsPublicInterfaceThenNoDiagnosticIsReported()
    {
        string source = @"
namespace TestApp
{
    public interface IOrderService
    {
    }

    public sealed class OrderService : IOrderService
    {
    }
}
";

        var test = CreateTest(source);
        await test.RunAsync();
    }

    [Fact]
    public async Task WhenClassDoesNotEndWithServiceThenNoDiagnosticIsReported()
    {
        string source = @"
namespace TestApp
{
    public sealed class OrderHelper
    {
    }
}
";

        var test = CreateTest(source);
        await test.RunAsync();
    }
}
