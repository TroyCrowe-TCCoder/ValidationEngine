using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Coding.Tests
{
    public class SwallowedExceptionAnalyzerTests
    {
        private static CSharpAnalyzerTest<SwallowedExceptionAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            return new CSharpAnalyzerTest<SwallowedExceptionAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };
        }

        [Fact]
        public async Task WhenCatchBlockIsEmptyThenCode018IsReported()
        {
            const string source = @"
using System;

namespace TestApp
{
    public class InvoiceService
    {
        public void Process()
        {
            try
            {
                DoWork();
            }
            {|#0:catch|} (InvalidOperationException ex)
            {
            }
        }

        private void DoWork()
        {
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(SwallowedExceptionAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("InvalidOperationException"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenCatchBlockNeitherLogsNorRethrowsThenCode018IsReported()
        {
            const string source = @"
using System;

namespace TestApp
{
    public class InvoiceService
    {
        public void Process()
        {
            try
            {
                DoWork();
            }
            {|#0:catch|} (InvalidOperationException ex)
            {
                var message = ex.Message;
            }
        }

        private void DoWork()
        {
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(SwallowedExceptionAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("InvalidOperationException"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenCatchBlockLogsExceptionThenNoDiagnosticIsReported()
        {
            const string source = @"
using System;

namespace TestApp
{
    public class Logger
    {
        public void LogError(string message, Exception ex)
        {
        }
    }

    public class InvoiceService
    {
        private readonly Logger _logger = new Logger();

        public void Process()
        {
            try
            {
                DoWork();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(""Failed to process invoice"", ex);
            }
        }

        private void DoWork()
        {
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenCatchBlockRethrowsThenNoDiagnosticIsReported()
        {
            const string source = @"
using System;

namespace TestApp
{
    public class InvoiceService
    {
        public void Process()
        {
            try
            {
                DoWork();
            }
            catch (InvalidOperationException)
            {
                throw;
            }
        }

        private void DoWork()
        {
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
