using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Coding.Tests
{
    public class MutableDtoAnalyzerTests
    {
        private static CSharpAnalyzerTest<MutableDtoAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            return new CSharpAnalyzerTest<MutableDtoAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };
        }

        [Fact]
        public async Task WhenDtoHasMutableSetterThenCode009IsReported()
        {
            const string source = @"
namespace TestApp
{
    public class InvoiceDto
    {
        public int Id { get; {|#0:set;|} }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(MutableDtoAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("Id", "InvoiceDto"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenDtoUsesInitOnlyThenNoDiagnosticIsReported()
        {
            const string source = @"
namespace TestApp
{
    public class InvoiceDto
    {
        public int Id { get; init; }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenClassIsNotDtoOrModelThenNoDiagnosticIsReported()
        {
            const string source = @"
namespace TestApp
{
    public class InvoiceService
    {
        public int Id { get; set; }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
