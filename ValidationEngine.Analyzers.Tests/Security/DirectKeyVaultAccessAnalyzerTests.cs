using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Security.Tests
{
    public class DirectKeyVaultAccessAnalyzerTests
    {
        private static CSharpAnalyzerTest<DirectKeyVaultAccessAnalyzer, DefaultVerifier> CreateTest(
            string source,
            string fileName)
        {
            var test = new CSharpAnalyzerTest<DirectKeyVaultAccessAnalyzer, DefaultVerifier>
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.Sources.Add((fileName, source));

            return test;
        }

        [Fact]
        public async Task WhenSecretClientIsInstantiatedOutsideProgramCsThenSec001IsReported()
        {
            const string source = @"
namespace Azure.Security.KeyVault.Secrets
{
    public class SecretClient
    {
        public SecretClient(System.Uri vaultUri, object credential)
        {
        }
    }
}

namespace TestApp
{
    using Azure.Security.KeyVault.Secrets;

    public class ExpenseService
    {
        public void Configure()
        {
            var client = {|#0:new SecretClient(new System.Uri(""https://vault""), null)|};
        }
    }
}
";

            var test = CreateTest(source, "ExpenseService.cs");
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(DirectKeyVaultAccessAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("SecretClient", "must not be instantiated outside Program.cs; Key Vault must be connected as a configuration provider at the application entry point"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenSecretClientIsInstantiatedInProgramCsThenNoDiagnosticIsReported()
        {
            const string source = @"
namespace Azure.Security.KeyVault.Secrets
{
    public class SecretClient
    {
        public SecretClient(System.Uri vaultUri, object credential)
        {
        }
    }
}

namespace TestApp
{
    using Azure.Security.KeyVault.Secrets;

    public class Program
    {
        public static void Main(string[] args)
        {
            var client = new SecretClient(new System.Uri(""https://vault""), null);
        }
    }
}
";

            var test = CreateTest(source, "Program.cs");

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenConnectionStringIsConcatenatedThenSec001IsReported()
        {
            const string source = @"
namespace TestApp
{
    public class ExpenseService
    {
        public void Configure(string host, string password)
        {
            string connectionString = {|#0:""Server="" + host + "";Password="" + password|};
        }
    }
}
";

            var test = CreateTest(source, "ExpenseService.cs");
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(DirectKeyVaultAccessAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("connectionString", "must not be constructed by string concatenation or interpolation; secrets must be read by name through IConfiguration or bound to a strongly typed options class"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenConnectionStringIsReadFromConfigurationThenNoDiagnosticIsReported()
        {
            const string source = @"
namespace TestApp
{
    public interface IConfiguration
    {
        string this[string key] { get; }
    }

    public class ExpenseService
    {
        public void Configure(IConfiguration configuration)
        {
            string connectionString = configuration[""ConnectionStrings:Default""];
        }
    }
}
";

            var test = CreateTest(source, "ExpenseService.cs");

            await test.RunAsync();
        }
    }
}
