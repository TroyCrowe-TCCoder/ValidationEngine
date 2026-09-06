using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Coding.Tests
{
    public class ImpreciseExceptionTypeAnalyzerTests
    {
        private static CSharpAnalyzerTest<ImpreciseExceptionTypeAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            return new CSharpAnalyzerTest<ImpreciseExceptionTypeAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };
        }

        [Fact]
        public async Task WhenExceptionIsThrownDirectlyThenCode017IsReported()
        {
            const string source = @"
using System;

namespace TestApp
{
    public class InvoiceService
    {
        public void Validate(int id)
        {
            if (id <= 0)
            {
                throw {|#0:new Exception(""Invalid id"")|};
            }
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(ImpreciseExceptionTypeAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("Exception"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenApplicationExceptionIsThrownDirectlyThenCode017IsReported()
        {
            const string source = @"
using System;

namespace TestApp
{
    public class InvoiceService
    {
        public void Validate(int id)
        {
            if (id <= 0)
            {
                throw {|#0:new ApplicationException(""Invalid id"")|};
            }
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(ImpreciseExceptionTypeAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("ApplicationException"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenPreciseExceptionTypeIsThrownThenNoDiagnosticIsReported()
        {
            const string source = @"
using System;

namespace TestApp
{
    public class InvoiceService
    {
        public void Validate(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
