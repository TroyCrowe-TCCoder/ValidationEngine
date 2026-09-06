using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Coding.Tests
{
    public class RepeatedLiteralAnalyzerTests
    {
        private static CSharpAnalyzerTest<RepeatedLiteralAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            return new CSharpAnalyzerTest<RepeatedLiteralAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };
        }

        [Fact]
        public async Task WhenStringLiteralRepeatsInSameTypeThenCode013IsReported()
        {
            const string source = @"
namespace TestApp
{
    public class PolicyProvider
    {
        public string GetFirstPolicy() => {|#0:""RequireClientAccess""|};

        public string GetSecondPolicy() => {|#1:""RequireClientAccess""|};
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(RepeatedLiteralAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("\"RequireClientAccess\"", 2, "PolicyProvider"));
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(RepeatedLiteralAnalyzer.Rule)
                    .WithLocation(1)
                    .WithArguments("\"RequireClientAccess\"", 2, "PolicyProvider"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenLiteralAppearsOnceThenNoDiagnosticIsReported()
        {
            const string source = @"
namespace TestApp
{
    public class PolicyProvider
    {
        public string GetPolicy() => ""RequireClientAccess"";
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenZeroOrOneNumericLiteralRepeatsThenNoDiagnosticIsReported()
        {
            const string source = @"
namespace TestApp
{
    public class Counter
    {
        public int First() => 1;
        public int Second() => 1;
        public int Zero() => 0;
        public int AlsoZero() => 0;
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
