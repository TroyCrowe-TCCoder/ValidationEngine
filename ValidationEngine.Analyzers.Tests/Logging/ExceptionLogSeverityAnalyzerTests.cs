using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Logging.Tests
{
    public class ExceptionLogSeverityAnalyzerTests
    {
        private static CSharpAnalyzerTest<ExceptionLogSeverityAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            var test = new CSharpAnalyzerTest<ExceptionLogSeverityAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.AdditionalReferences.Add(typeof(Microsoft.Extensions.Logging.ILogger<>).Assembly);

            return test;
        }

        [Fact]
        public async Task WhenCaughtExceptionIsLoggedAsWarningThenLog006IsReported()
        {
            const string source = @"
using System;
using Microsoft.Extensions.Logging;

namespace TestApp
{
    public class ExpenseService
    {
        private readonly ILogger<ExpenseService> _logger;

        public ExpenseService(ILogger<ExpenseService> logger)
        {
            _logger = logger;
        }

        public void Process()
        {
            try
            {
                DoWork();
            }
            catch (Exception ex)
            {
                _logger.{|#0:LogWarning|}(ex, ""Processing failed. {DocumentId}"", 1);
            }
        }

        private void DoWork() { }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(ExceptionLogSeverityAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("Process", "ex", "LogWarning"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenCaughtExceptionIsLoggedAsErrorThenNoDiagnosticIsReported()
        {
            const string source = @"
using System;
using Microsoft.Extensions.Logging;

namespace TestApp
{
    public class ExpenseService
    {
        private readonly ILogger<ExpenseService> _logger;

        public ExpenseService(ILogger<ExpenseService> logger)
        {
            _logger = logger;
        }

        public void Process()
        {
            try
            {
                DoWork();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ""Processing failed. {DocumentId}"", 1);
            }
        }

        private void DoWork() { }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
