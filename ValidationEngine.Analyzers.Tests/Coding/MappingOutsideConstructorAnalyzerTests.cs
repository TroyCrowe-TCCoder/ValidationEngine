using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Coding.Tests
{
    public class MappingOutsideConstructorAnalyzerTests
    {
        private static CSharpAnalyzerTest<MappingOutsideConstructorAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            return new CSharpAnalyzerTest<MappingOutsideConstructorAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };
        }

        [Fact]
        public async Task WhenServiceMapsPropertiesInObjectInitializerThenCode010IsReported()
        {
            const string source = @"
namespace TestApp
{
    public class Invoice
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
    }

    public class InvoiceDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
    }

    public class InvoiceService
    {
        public InvoiceDto ToDto(Invoice invoice)
        {
            return {|#0:new InvoiceDto
            {
                Id = invoice.Id,
                Amount = invoice.Amount,
            }|};
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(MappingOutsideConstructorAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("InvoiceDto", 2, "invoice"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenMappingOccursInsideTargetConstructorThenNoDiagnosticIsReported()
        {
            const string source = @"
namespace TestApp
{
    public class Invoice
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
    }

    public class InvoiceDto
    {
        public int Id { get; }
        public decimal Amount { get; }

        public InvoiceDto(Invoice invoice)
        {
            Id = invoice.Id;
            Amount = invoice.Amount;
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenOnlyOnePropertyIsMappedThenNoDiagnosticIsReported()
        {
            const string source = @"
namespace TestApp
{
    public class Invoice
    {
        public int Id { get; set; }
    }

    public class InvoiceDto
    {
        public int Id { get; set; }
    }

    public class InvoiceService
    {
        public InvoiceDto ToDto(Invoice invoice)
        {
            return new InvoiceDto
            {
                Id = invoice.Id,
            };
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
