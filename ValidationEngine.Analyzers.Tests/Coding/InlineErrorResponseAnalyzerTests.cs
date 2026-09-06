using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Coding.Tests
{
    public class InlineErrorResponseAnalyzerTests
    {
        private static CSharpAnalyzerTest<InlineErrorResponseAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            return new CSharpAnalyzerTest<InlineErrorResponseAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };
        }

        [Fact]
        public async Task WhenControllerCatchBlockReturnsManuallyConstructedErrorObjectThenCode019IsReported()
        {
            const string source = @"
using System;

namespace TestApp
{
    public class ErrorResponse
    {
        public string Message { get; set; }
    }

    public class ActionResult
    {
        public ActionResult(object value)
        {
        }
    }

    public class InvoiceController
    {
        public ActionResult Get(int id)
        {
            try
            {
                return new ActionResult(id);
            }
            catch (Exception ex)
            {
                return new ActionResult({|#0:new ErrorResponse { Message = ex.Message }|});
            }
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(InlineErrorResponseAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("InvoiceController"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenControllerCatchBlockReturnsProblemDetailsThenNoDiagnosticIsReported()
        {
            const string source = @"
using System;

namespace TestApp
{
    public class ProblemDetails
    {
        public string Title { get; set; }
    }

    public class ActionResult
    {
        public ActionResult(object value)
        {
        }
    }

    public class InvoiceController
    {
        public ActionResult Get(int id)
        {
            try
            {
                return new ActionResult(id);
            }
            catch (Exception ex)
            {
                return new ActionResult(new ProblemDetails { Title = ""An error occurred"" });
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
