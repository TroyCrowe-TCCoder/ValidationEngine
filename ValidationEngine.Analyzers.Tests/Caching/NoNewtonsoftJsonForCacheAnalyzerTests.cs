using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Caching.Tests
{
    public class NoNewtonsoftJsonForCacheAnalyzerTests
    {
        private static CSharpAnalyzerTest<NoNewtonsoftJsonForCacheAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            var test = new CSharpAnalyzerTest<NoNewtonsoftJsonForCacheAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.AdditionalReferences.Add(typeof(Microsoft.Extensions.Caching.Distributed.IDistributedCache).Assembly);
            test.TestState.AdditionalReferences.Add(typeof(Newtonsoft.Json.JsonConvert).Assembly);

            return test;
        }

        [Fact]
        public async Task WhenCacheSerializerClassUsesJsonConvertThenCache008IsReported()
        {
            const string source = @"
using Newtonsoft.Json;

namespace TestApp
{
    public static class CacheSerializer
    {
        public static byte[] Serialize<T>(T value)
            => System.Text.Encoding.UTF8.GetBytes({|#0:JsonConvert.SerializeObject(value)|});
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(NoNewtonsoftJsonForCacheAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("CacheSerializer"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenServiceInjectingIDistributedCacheUsesJsonConvertThenCache008IsReported()
        {
            const string source = @"
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace TestApp
{
    public class UserProfileService
    {
        private readonly IDistributedCache _cache;

        public UserProfileService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public string SerializeProfile(object profile)
            => {|#0:JsonConvert.SerializeObject(profile)|};
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(NoNewtonsoftJsonForCacheAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("UserProfileService"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenCacheSerializerClassUsesSystemTextJsonThenNoDiagnosticIsReported()
        {
            const string source = @"
using System.Text.Json;

namespace TestApp
{
    public static class CacheSerializer
    {
        public static byte[] Serialize<T>(T value)
            => JsonSerializer.SerializeToUtf8Bytes(value);
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenJsonConvertUsedOutsideCacheContextThenNoDiagnosticIsReported()
        {
            const string source = @"
using Newtonsoft.Json;

namespace TestApp
{
    public class ReportExportService
    {
        public string SerializeReport(object report)
            => JsonConvert.SerializeObject(report);
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
