using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Logging.Tests
{
    public class DirectHttpClientCallBypassesManagerAnalyzerTests
    {
        private static CSharpAnalyzerTest<DirectHttpClientCallBypassesManagerAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            var test = new CSharpAnalyzerTest<DirectHttpClientCallBypassesManagerAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            return test;
        }

        [Fact]
        public async Task WhenHttpClientGetAsyncIsCalledDirectlyThenLog010IsReported()
        {
            const string source = @"
using System.Net.Http;
using System.Threading.Tasks;

namespace TestApp
{
    public class ExpenseService
    {
        private readonly HttpClient _httpClient;

        public ExpenseService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task ProcessAsync()
        {
            await _httpClient.{|#0:GetAsync|}(""https://example.com"");
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(DirectHttpClientCallBypassesManagerAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("ProcessAsync", "GetAsync"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenCalledFromHttpClientManagerThenNoDiagnosticIsReported()
        {
            const string source = @"
using System.Net.Http;
using System.Threading.Tasks;

namespace TestApp
{
    public class HttpClientManager
    {
        private readonly HttpClient _httpClient;

        public HttpClientManager(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<HttpResponseMessage> GetAsync(string url)
        {
            return await _httpClient.GetAsync(url);
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
