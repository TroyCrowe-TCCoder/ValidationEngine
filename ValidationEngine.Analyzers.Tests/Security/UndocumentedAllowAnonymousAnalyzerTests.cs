using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Security.Tests
{
    public class UndocumentedAllowAnonymousAnalyzerTests
    {
        private static CSharpAnalyzerTest<UndocumentedAllowAnonymousAnalyzer, DefaultVerifier> CreateTest(
            string source,
            string fileName)
        {
            var test = new CSharpAnalyzerTest<UndocumentedAllowAnonymousAnalyzer, DefaultVerifier>
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.Sources.Add((fileName, source));

            return test;
        }

        private const string AllowAnonymousStub = @"
namespace TestApp
{
    public class AllowAnonymousAttribute : System.Attribute
    {
    }
}
";

        [Fact]
        public async Task WhenAllowAnonymousHasNoCommentThenSec002IsReported()
        {
            string source = AllowAnonymousStub + @"
namespace TestApp
{
    public class HealthController
    {
        [{|#0:AllowAnonymous|}]
        public void Health()
        {
        }
    }
}
";

            var test = CreateTest(source, "HealthController.cs");
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(UndocumentedAllowAnonymousAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("Health"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenAllowAnonymousHasTrailingCommentThenNoDiagnosticIsReported()
        {
            string source = AllowAnonymousStub + @"
namespace TestApp
{
    public class HealthController
    {
        [AllowAnonymous] // Public endpoint: unauthenticated health check required by load balancer
        public void Health()
        {
        }
    }
}
";

            var test = CreateTest(source, "HealthController.cs");

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenAllowAnonymousHasLeadingCommentThenNoDiagnosticIsReported()
        {
            string source = AllowAnonymousStub + @"
namespace TestApp
{
    public class HealthController
    {
        // Public endpoint: unauthenticated health check required by load balancer
        [AllowAnonymous]
        public void Health()
        {
        }
    }
}
";

            var test = CreateTest(source, "HealthController.cs");

            await test.RunAsync();
        }
    }
}
