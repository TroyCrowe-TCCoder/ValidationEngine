using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Security.Tests
{
    public class DirectHttpClientInstantiationAnalyzerTests
    {
        private static CSharpAnalyzerTest<DirectHttpClientInstantiationAnalyzer, DefaultVerifier> CreateTest(
            string source,
            string fileName)
        {
            var test = new CSharpAnalyzerTest<DirectHttpClientInstantiationAnalyzer, DefaultVerifier>
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.Sources.Add((fileName, source));

            return test;
        }

        [Fact]
        public async Task WhenHttpClientIsInstantiatedDirectlyThenSec007IsReported()
        {
            const string source = @"
namespace TestApp
{
    public class InvoiceClient
    {
        public void CallDownstreamApi()
        {
            var client = {|#0:new System.Net.Http.HttpClient()|};
        }
    }
}
";

            var test = CreateTest(source, "InvoiceClient.cs");
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(DirectHttpClientInstantiationAnalyzer.Rule)
                    .WithLocation(0));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenHttpClientManagerIsUsedThenNoDiagnosticIsReported()
        {
            const string source = @"
namespace TestApp
{
    public interface IHttpClientBuilder
    {
        System.Net.Http.HttpClient CreateOAuthClient(string clientType, string basePath, string token);
    }

    public class InvoiceClient
    {
        private readonly IHttpClientBuilder _httpClientBuilder;

        public InvoiceClient(IHttpClientBuilder httpClientBuilder)
        {
            _httpClientBuilder = httpClientBuilder;
        }

        public void CallDownstreamApi()
        {
            var client = _httpClientBuilder.CreateOAuthClient(""Invoices"", ""https://api.example.com"", ""token"");
        }
    }
}
";

            var test = CreateTest(source, "InvoiceClient.cs");

            await test.RunAsync();
        }
    }
}
