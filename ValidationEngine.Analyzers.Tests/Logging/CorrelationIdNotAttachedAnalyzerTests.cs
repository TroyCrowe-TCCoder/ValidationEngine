using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Logging.Tests
{
    public class CorrelationIdNotAttachedAnalyzerTests
    {
        private static CSharpAnalyzerTest<CorrelationIdNotAttachedAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            var test = new CSharpAnalyzerTest<CorrelationIdNotAttachedAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.AdditionalReferences.Add(typeof(Microsoft.AspNetCore.Http.HttpContext).Assembly);

            return test;
        }

        [Fact]
        public async Task WhenHttpContextIsReferencedButTraceIdentifierIsNeverAssignedThenLog008IsReported()
        {
            const string source = @"
using Microsoft.AspNetCore.Http;

namespace TestApp
{
    public class CorrelationMiddleware
    {
        public void Invoke(HttpContext context)
        {
            var path = context.Request.Path;
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(new DiagnosticResult(CorrelationIdNotAttachedAnalyzer.Rule));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenTraceIdentifierIsAssignedThenNoDiagnosticIsReported()
        {
            const string source = @"
using Microsoft.AspNetCore.Http;

namespace TestApp
{
    public class CorrelationMiddleware
    {
        public void Invoke(HttpContext context)
        {
            context.TraceIdentifier = ""abc-123"";
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
