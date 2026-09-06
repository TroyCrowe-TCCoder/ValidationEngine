using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Coding.Tests
{
    public class ProgramCompositionOnlyAnalyzerTests
    {
        private static CSharpAnalyzerTest<ProgramCompositionOnlyAnalyzer, DefaultVerifier> CreateTest(string programSource)
        {
            var test = new CSharpAnalyzerTest<ProgramCompositionOnlyAnalyzer, DefaultVerifier>
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.Sources.Add(("Program.cs", programSource));

            return test;
        }

        [Fact]
        public async Task WhenProgramCsContainsForLoopThenCode001IsReported()
        {
            const string programSource = @"
namespace TestApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            {|#0:for (int i = 0; i < args.Length; i++)
            {
            }|}
        }
    }
}
";

            var test = CreateTest(programSource);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(ProgramCompositionOnlyAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("for loop"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenProgramCsContainsLocalFunctionThenCode001IsReported()
        {
            const string programSource = @"
namespace TestApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            {|#0:void ProcessArgs()
            {
            }|}

            ProcessArgs();
        }
    }
}
";

            var test = CreateTest(programSource);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(ProgramCompositionOnlyAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("local function 'ProcessArgs'"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenProgramCsContainsOnlyWiringThenNoDiagnosticIsReported()
        {
            const string programSource = @"
namespace TestApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = new object();
        }
    }
}
";

            var test = CreateTest(programSource);

            await test.RunAsync();
        }
    }
}
