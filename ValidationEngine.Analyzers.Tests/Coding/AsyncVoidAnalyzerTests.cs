using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Coding.Tests
{
    public class AsyncVoidAnalyzerTests
    {
        private static CSharpAnalyzerTest<AsyncVoidAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            return new CSharpAnalyzerTest<AsyncVoidAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };
        }

        [Fact]
        public async Task WhenAsyncVoidMethodIsNotAnEventHandlerThenCode021IsReported()
        {
            const string source = @"
using System.Threading.Tasks;

namespace TestApp
{
    public class InvoiceService
    {
        public async void {|#0:ProcessInvoice|}()
        {
            await Task.Delay(1);
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(AsyncVoidAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("ProcessInvoice"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenAsyncVoidEventHandlerHasNoLoggedTryCatchThenCode021IsReported()
        {
            const string source = @"
using System;
using System.Threading.Tasks;

namespace TestApp
{
    public class InvoiceButton
    {
        public async void {|#0:OnClick|}(object sender, EventArgs e)
        {
            await Task.Delay(1);
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(AsyncVoidAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("OnClick"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenAsyncVoidEventHandlerLogsAllExceptionsThenNoDiagnosticIsReported()
        {
            const string source = @"
using System;
using System.Threading.Tasks;

namespace TestApp
{
    public class Logger
    {
        public void LogError(string message, Exception ex)
        {
        }
    }

    public class InvoiceButton
    {
        private readonly Logger _logger = new Logger();

        public async void OnClick(object sender, EventArgs e)
        {
            try
            {
                await Task.Delay(1);
            }
            catch (Exception ex)
            {
                _logger.LogError(""Click handler failed"", ex);
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
