using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Logging.Tests
{
    public class CorrelationIdNotInLogScopeAnalyzerTests
    {
        private static CSharpAnalyzerTest<CorrelationIdNotInLogScopeAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            var test = new CSharpAnalyzerTest<CorrelationIdNotInLogScopeAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.AdditionalReferences.Add(typeof(Microsoft.AspNetCore.Http.HttpContext).Assembly);
            test.TestState.AdditionalReferences.Add(typeof(Microsoft.Extensions.Logging.ILogger<>).Assembly);

            return test;
        }

        [Fact]
        public async Task WhenTraceIdentifierIsAssignedButBeginScopeIsNeverCalledThenLog009IsReported()
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
            test.ExpectedDiagnostics.Add(new DiagnosticResult(CorrelationIdNotInLogScopeAnalyzer.Rule));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenBeginScopeIsCalledThenNoDiagnosticIsReported()
        {
            const string source = @"
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace TestApp
{
    public class CorrelationMiddleware
    {
        private readonly ILogger<CorrelationMiddleware> _logger;

        public CorrelationMiddleware(ILogger<CorrelationMiddleware> logger)
        {
            _logger = logger;
        }

        public void Invoke(HttpContext context)
        {
            context.TraceIdentifier = ""abc-123"";

            using (_logger.BeginScope(new Dictionary<string, object> { [""CorrelationId""] = context.TraceIdentifier }))
            {
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
