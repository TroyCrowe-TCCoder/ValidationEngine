using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Testing.Tests;

public class ObservabilityTestNamingAnalyzerTests
{
    private static CSharpAnalyzerTest<ObservabilityTestNamingAnalyzer, DefaultVerifier> CreateTest(string source)
    {
        var test = new CSharpAnalyzerTest<ObservabilityTestNamingAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.TestState.Sources.Add(("XunitStub.cs", TestingStubs.XunitStub));
        test.TestState.Sources.Add(("Tests.cs", source));
        return test;
    }

    [Fact]
    public async Task WhenObservabilityTestMethodNameDoesNotMatchPatternThenTest002IsReported()
    {
        string source = @"
namespace TestApp
{
    public class AccountServiceObservabilityTests
    {
        [Xunit.Fact]
        public void {|#0:AddClient_WhenRepositoryThrows_RethrowsAndLogsError|}()
        {
        }
    }
}
";

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(ObservabilityTestNamingAnalyzer.Rule)
                .WithLocation(0)
                .WithArguments("AddClient_WhenRepositoryThrows_RethrowsAndLogsError"));

        await test.RunAsync();
    }

    [Fact]
    public async Task WhenObservabilityTestMethodNameMatchesPatternThenNoDiagnosticIsReported()
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

    [Fact]
    public async Task WhenClassIsNotObservabilityTestsThenNotFlaggedByThisAnalyzer()
    {
        string source = @"
namespace TestApp
{
    public class ClientServiceTests
    {
        [Xunit.Fact]
        public void GetById_ReturnsClient()
        {
        }
    }
}
";

        var test = CreateTest(source);
        await test.RunAsync();
    }
}
