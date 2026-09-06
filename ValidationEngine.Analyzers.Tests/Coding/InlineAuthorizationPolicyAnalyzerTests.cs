using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Coding.Tests
{
    public class InlineAuthorizationPolicyAnalyzerTests
    {
        private static CSharpAnalyzerTest<InlineAuthorizationPolicyAnalyzer, DefaultVerifier> CreateTest(
            string source,
            string fileName)
        {
            var test = new CSharpAnalyzerTest<InlineAuthorizationPolicyAnalyzer, DefaultVerifier>
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.Sources.Add((fileName, source));

            return test;
        }

        [Fact]
        public async Task WhenProgramCsDefinesInlineAuthorizationPolicyThenCode004IsReported()
        {
            const string source = @"
namespace TestApp
{
    public static class AuthorizationExtensions
    {
        public static void AddAuthorization(this object services, System.Action<object> configure)
        {
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var services = new object();
            {|#0:services.AddAuthorization(options =>
            {
            })|};
        }
    }
}
";

            var test = CreateTest(source, "Program.cs");
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(InlineAuthorizationPolicyAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("Program.cs"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenControllerDefinesInlineAuthorizationPolicyThenCode004IsReported()
        {
            const string source = @"
namespace TestApp
{
    public static class AuthorizationExtensions
    {
        public static void AddAuthorization(this object services, System.Action<object> configure)
        {
        }
    }

    public class ExpenseController
    {
        public void Configure(object services)
        {
            {|#0:services.AddAuthorization(options =>
            {
            })|};
        }
    }
}
";

            var test = CreateTest(source, "ExpenseController.cs");
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(InlineAuthorizationPolicyAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("ExpenseController"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenAddAuthorizationCalledFromDedicatedPolicyProviderThenNoDiagnosticIsReported()
        {
            const string source = @"
namespace TestApp
{
    public static class AuthorizationExtensions
    {
        public static void AddAuthorization(this object services, System.Action<object> configure)
        {
        }
    }

    public class AuthorizationPolicyProvider
    {
        public void Configure(object services)
        {
            services.AddAuthorization(options =>
            {
            });
        }
    }
}
";

            var test = CreateTest(source, "AuthorizationPolicyProvider.cs");

            await test.RunAsync();
        }
    }
}
