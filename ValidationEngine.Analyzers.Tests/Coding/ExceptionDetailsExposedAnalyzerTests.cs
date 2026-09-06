using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Coding.Tests
{
    public class ExceptionDetailsExposedAnalyzerTests
    {
        private static CSharpAnalyzerTest<ExceptionDetailsExposedAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            return new CSharpAnalyzerTest<ExceptionDetailsExposedAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };
        }

        [Fact]
        public async Task WhenControllerCatchBlockReturnsExceptionMessageThenCode020IsReported()
        {
            const string source = @"
using System;

namespace TestApp
{
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
                return new ActionResult({|#0:ex.Message|});
            }
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(ExceptionDetailsExposedAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("InvoiceController", "ex.Message"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenControllerCatchBlockReturnsExceptionToStringThenCode020IsReported()
        {
            const string source = @"
using System;

namespace TestApp
{
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
                return new ActionResult({|#0:ex.ToString|}());
            }
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(ExceptionDetailsExposedAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("InvoiceController", "ex.ToString"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenControllerCatchBlockReturnsGenericMessageThenNoDiagnosticIsReported()
        {
            const string source = @"
using System;

namespace TestApp
{
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
                return new ActionResult(""An unexpected error occurred."");
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
