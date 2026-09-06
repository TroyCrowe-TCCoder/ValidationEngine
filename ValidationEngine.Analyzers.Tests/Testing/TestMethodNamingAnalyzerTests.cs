using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Testing.Tests;

public class TestMethodNamingAnalyzerTests
{
    private static CSharpAnalyzerTest<TestMethodNamingAnalyzer, DefaultVerifier> CreateTest(string source)
    {
        var test = new CSharpAnalyzerTest<TestMethodNamingAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.TestState.Sources.Add(("XunitStub.cs", TestingStubs.XunitStub));
        test.TestState.Sources.Add(("Tests.cs", source));
        return test;
    }

    [Fact]
    public async Task WhenFactMethodNameHasFewerThanThreeSegmentsThenTest001IsReported()
    {
        string source = @"
namespace TestApp
{
    public class ClientServiceTests
    {
        [Xunit.Fact]
        public void {|#0:GetById_ReturnsClient|}()
        {
        }
    }
}
";

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(TestMethodNamingAnalyzer.Rule)
                .WithLocation(0)
                .WithArguments("GetById_ReturnsClient"));

        await test.RunAsync();
    }

    [Fact]
    public async Task WhenFactMethodNameHasThreeSegmentsThenNoDiagnosticIsReported()
    {
        string source = @"
namespace TestApp
{
    public class ClientServiceTests
    {
        [Xunit.Fact]
        public void GetById_WhenClientExists_ReturnsClient()
        {
        }
    }
}
";

        var test = CreateTest(source);
        await test.RunAsync();
    }

    [Fact]
    public async Task WhenObservabilityTestClassThenNotFlaggedByThisAnalyzer()
    {
        string source = @"
namespace TestApp
{
    public class AccountServiceObservabilityTests
    {
        [Xunit.Fact]
        public void AddWhenRepositoryThrowsThenRethrowsAndLogsError()
        {
        }
    }
}
";

        var test = CreateTest(source);
        await test.RunAsync();
    }
}
