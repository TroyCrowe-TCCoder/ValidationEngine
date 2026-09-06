using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Logging.Tests
{
    public class LogEntryInsideLoopAnalyzerTests
    {
        private static CSharpAnalyzerTest<LogEntryInsideLoopAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            var test = new CSharpAnalyzerTest<LogEntryInsideLoopAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.AdditionalReferences.Add(typeof(Microsoft.Extensions.Logging.ILogger<>).Assembly);

            return test;
        }

        [Fact]
        public async Task WhenLoggerCalledInsideForeachLoopThenLog016IsReported()
        {
            const string source = @"
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace TestApp
{
    public class ImportService
    {
        private readonly ILogger<ImportService> _logger;

        public ImportService(ILogger<ImportService> logger)
        {
            _logger = logger;
        }

        public void Process(IEnumerable<int> records)
        {
            foreach (var record in records)
            {
                _logger.{|#0:LogInformation|}(""Processed record {RecordId}"", record);
            }
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(LogEntryInsideLoopAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("Process"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenLoggerCalledOutsideLoopThenNoDiagnosticIsReported()
        {
            const string source = @"
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace TestApp
{
    public class ImportService
    {
        private readonly ILogger<ImportService> _logger;

        public ImportService(ILogger<ImportService> logger)
        {
            _logger = logger;
        }

        public void Process(IEnumerable<int> records)
        {
            int processed = 0;

            foreach (var record in records)
            {
                processed++;
            }

            _logger.LogInformation(""Batch import complete. {Processed} records processed"", processed);
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
