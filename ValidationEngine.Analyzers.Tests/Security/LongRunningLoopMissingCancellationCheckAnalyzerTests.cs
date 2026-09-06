using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Security.Tests
{
    public class LongRunningLoopMissingCancellationCheckAnalyzerTests
    {
        private static CSharpAnalyzerTest<LongRunningLoopMissingCancellationCheckAnalyzer, DefaultVerifier> CreateTest(
            string source,
            string fileName)
        {
            var test = new CSharpAnalyzerTest<LongRunningLoopMissingCancellationCheckAnalyzer, DefaultVerifier>
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.Sources.Add((fileName, source));

            return test;
        }

        [Fact]
        public async Task WhenPollingLoopHasNoCancellationCheckThenSec008IsReported()
        {
            const string source = @"
using System.Threading;
using System.Threading.Tasks;

namespace TestApp
{
    public class BillingProcessor
    {
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            {|#0:while (true)
            {
                await ProcessNextBatchAsync(cancellationToken);
                await Task.Delay(1000, cancellationToken);
            }|}
        }

        private Task ProcessNextBatchAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
";

            var test = CreateTest(source, "BillingProcessor.cs");
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(LongRunningLoopMissingCancellationCheckAnalyzer.Rule)
                    .WithLocation(0));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenPollingLoopChecksIsCancellationRequestedThenNoDiagnosticIsReported()
        {
            const string source = @"
using System.Threading;
using System.Threading.Tasks;

namespace TestApp
{
    public class BillingProcessor
    {
        public async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await ProcessNextBatchAsync(cancellationToken);
                await Task.Delay(1000, cancellationToken);
            }
        }

        private Task ProcessNextBatchAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
";

            var test = CreateTest(source, "BillingProcessor.cs");

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenLoopHasNoTaskDelayThenNoDiagnosticIsReported()
        {
            const string source = @"
using System.Threading;
using System.Threading.Tasks;

namespace TestApp
{
    public class BillingProcessor
    {
        public void Run(int[] items)
        {
            foreach (var item in items)
            {
                System.Console.WriteLine(item);
            }
        }
    }
}
";

            var test = CreateTest(source, "BillingProcessor.cs");

            await test.RunAsync();
        }
    }
}
