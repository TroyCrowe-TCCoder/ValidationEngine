using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Coding.Tests
{
    public class GlobalErrorHandlerAnalyzerTests
    {
        private static CSharpAnalyzerTest<GlobalErrorHandlerAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            var test = new CSharpAnalyzerTest<GlobalErrorHandlerAnalyzer, DefaultVerifier>
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.Sources.Add(("Program.cs", source));

            return test;
        }

        [Fact]
        public async Task WhenFirstMiddlewareIsNotUseExceptionHandlerThenCode016IsReported()
        {
            const string source = @"
namespace TestApp
{
    public class WebApplication
    {
        public void UseRouting()
        {
        }

        public void UseExceptionHandler()
        {
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var app = new WebApplication();
            {|#0:app.UseRouting()|};
            app.UseExceptionHandler();
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(GlobalErrorHandlerAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("UseRouting"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenUseExceptionHandlerIsRegisteredFirstThenNoDiagnosticIsReported()
        {
            const string source = @"
namespace TestApp
{
    public class WebApplication
    {
        public void UseRouting()
        {
        }

        public void UseExceptionHandler()
        {
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var app = new WebApplication();
            app.UseExceptionHandler();
            app.UseRouting();
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
