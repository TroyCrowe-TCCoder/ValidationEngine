using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Coding.Tests
{
    public class CancellationTokenForwardingAnalyzerTests
    {
        private const string MissingParameterMessage =
            "must accept a CancellationToken parameter and forward it to awaited calls";

        private const string SubstitutedNoneMessage =
            "must not substitute CancellationToken.None for its own CancellationToken parameter when calling other async methods";

        private static CSharpAnalyzerTest<CancellationTokenForwardingAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            return new CSharpAnalyzerTest<CancellationTokenForwardingAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };
        }

        [Fact]
        public async Task WhenAsyncMethodHasNoCancellationTokenParameterThenCode022IsReported()
        {
            const string source = @"
using System.Threading.Tasks;

namespace TestApp
{
    public class InvoiceService
    {
        public async Task {|#0:ProcessAsync|}(int id)
        {
            await Task.Delay(1);
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(CancellationTokenForwardingAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("ProcessAsync", MissingParameterMessage));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenCancellationTokenNoneIsSubstitutedThenCode022IsReported()
        {
            const string source = @"
using System.Threading;
using System.Threading.Tasks;

namespace TestApp
{
    public class InvoiceService
    {
        public async Task ProcessAsync(int id, CancellationToken cancellationToken)
        {
            await Task.Delay(1, {|#0:CancellationToken.None|});
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(CancellationTokenForwardingAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("ProcessAsync", SubstitutedNoneMessage));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenCancellationTokenIsForwardedThenNoDiagnosticIsReported()
        {
            const string source = @"
using System.Threading;
using System.Threading.Tasks;

namespace TestApp
{
    public class InvoiceService
    {
        public async Task ProcessAsync(int id, CancellationToken cancellationToken)
        {
            await Task.Delay(1, cancellationToken);
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
