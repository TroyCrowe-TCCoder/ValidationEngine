using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Coding.Tests
{
    public class OneTypePerFileAnalyzerTests
    {
        private static CSharpAnalyzerTest<OneTypePerFileAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            var test = new CSharpAnalyzerTest<OneTypePerFileAnalyzer, DefaultVerifier>
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.Sources.Add(source);

            return test;
        }

        [Fact]
        public async Task WhenFileDeclaresTwoTopLevelClassesThenCode024IsReported()
        {
            const string source = @"
namespace TestApp
{
    public class {|#0:FirstType|}
    {
    }

    public class SecondType
    {
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(OneTypePerFileAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments(2, "FirstType', 'SecondType"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenFileDeclaresOnlyOneTypeThenNoDiagnosticIsReported()
        {
            const string source = @"
namespace TestApp
{
    public class SingleType
    {
        private class NestedHelper
        {
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenFileDeclaresPartialTypeAcrossMultipleDeclarationsThenNoDiagnosticIsReported()
        {
            const string source = @"
namespace TestApp
{
    public partial class SharedType
    {
    }

    public partial class SharedType
    {
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
