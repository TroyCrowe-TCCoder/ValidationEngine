using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Security.Tests
{
    public class SecretConfigurationAnalyzerTests
    {
        private static CSharpAnalyzerTest<SecretConfigurationAnalyzer, DefaultVerifier> CreateTest(string source, string fileName)
        {
            var test = new CSharpAnalyzerTest<SecretConfigurationAnalyzer, DefaultVerifier>
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.Sources.Add((fileName, source));
            return test;
        }

        [Fact]
        public async Task WhenSecretClientIsInstantiatedOutsideProgramThenDiagnosticReported()
        {
            string source = @"
namespace Azure.Security.KeyVault.Secrets { public class SecretClient { public SecretClient() {} } }
namespace TestApp
{
    public class FooService
    {
        public void Create()
        {
            var client = {|#0:new Azure.Security.KeyVault.Secrets.SecretClient()|};
        }
    }
}
";

            var test = CreateTest(source, "FooService.cs");
            test.ExpectedDiagnostics.Add(new DiagnosticResult(SecretConfigurationAnalyzer.Rule).WithLocation(0).WithArguments("SecretClient"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenSecretShapedVariableIsConcatenatedThenDiagnosticReported()
        {
            string source = @"
namespace TestApp
{
    public class FooService
    {
        public void Create()
        {
            string connectionString = {|#0:""Server="" + ""localhost""|};
        }
    }
}
";

            var test = CreateTest(source, "FooService.cs");
            test.ExpectedDiagnostics.Add(new DiagnosticResult(SecretConfigurationAnalyzer.Rule).WithLocation(0).WithArguments("connectionString"));

            await test.RunAsync();
        }
    }
}
