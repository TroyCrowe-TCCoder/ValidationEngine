using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Testing.Tests;

public class NoRawStopwatchInPerformanceTestAnalyzerTests
{
    private static CSharpAnalyzerTest<NoRawStopwatchInPerformanceTestAnalyzer, DefaultVerifier> CreateTest(string source)
    {
        var test = new CSharpAnalyzerTest<NoRawStopwatchInPerformanceTestAnalyzer, DefaultVerifier>
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
        };

        test.TestState.Sources.Add(("BenchmarkDotNetStub.cs", TestingStubs.BenchmarkDotNetStub));
        test.TestState.Sources.Add(("Tests.cs", source));
        return test;
    }

    [Fact]
    public async Task WhenStopwatchUsedDirectlyInTestClassThenTest003IsReported()
    {
        string source = @"
namespace TestApp
{
    public class ClientServiceTests
    {
        public void MeasurePerformance()
        {
            var stopwatch = {|#0:new System.Diagnostics.Stopwatch()|};
            stopwatch.Start();
        }
    }
}
";

        var test = CreateTest(source);
        test.ExpectedDiagnostics.Add(
            new DiagnosticResult(NoRawStopwatchInPerformanceTestAnalyzer.Rule)
                .WithLocation(0)
                .WithArguments("ClientServiceTests"));

        await test.RunAsync();
    }

    [Fact]
    public async Task WhenClassHasBenchmarkMethodThenNoDiagnosticIsReported()
    {
        string source = @"
namespace TestApp
{
    public class GetAllClientsTests
    {
        [BenchmarkDotNet.Attributes.Benchmark]
        public void GetAllAsync()
        {
        }

        public void MeasurePerformance()
        {
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
        }
    }
}
";

        var test = CreateTest(source);
        await test.RunAsync();
    }
}
