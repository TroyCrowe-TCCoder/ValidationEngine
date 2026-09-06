using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Testing.Tests;

public class NoMoqUsageAnalyzerTests
{
    private static CSharpAnalyzerTest<NoMoqUsageAnalyzer, DefaultVerifier> CreateTest(string source)
    {
        var test = new CSharpAnalyzerTest<NoMoqUsageAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.TestState.Sources.Add(("MoqStub.cs", TestingStubs.MoqStub));
        test.TestState.Sources.Add(("Tests.cs", source));
        return test;
    }

    [Fact]
    public async Task WhenMoqUsingDirectiveIsPresentThenTest004IsReported()
    {
        string source = @"
{|#0:using Moq;|}

namespace TestApp
{
    public class ClientServiceTests
    {
        public void Test()
        {
        }
    }
}
";

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(NoMoqUsageAnalyzer.Rule)
                .WithLocation(0)
                .WithArguments("Moq"));

        await test.RunAsync();
    }

    [Fact]
    public async Task WhenMoqMockTypeIsUsedThenTest004IsReported()
    {
        string source = @"
namespace TestApp
{
    public interface IClientRepository { }

    public class ClientServiceTests
    {
        public void Test()
        {
            var mock = new {|#0:Moq.Mock<IClientRepository>|}();
        }
    }
}
";

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(NoMoqUsageAnalyzer.Rule)
                .WithLocation(0)
                .WithArguments("Moq.Mock<IClientRepository>"));

        await test.RunAsync();
    }

    [Fact]
    public async Task WhenNoMoqUsageThenNoDiagnosticIsReported()
    {
        string source = @"
namespace TestApp
{
    public class ClientServiceTests
    {
        public void Test()
        {
        }
    }
}
";

        var test = CreateTest(source);
        await test.RunAsync();
    }
}
