using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Caching.Tests
{
    public class NoInlineJsonSerializerForCacheAnalyzerTests
    {
        private static CSharpAnalyzerTest<NoInlineJsonSerializerForCacheAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            var test = new CSharpAnalyzerTest<NoInlineJsonSerializerForCacheAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.AdditionalReferences.Add(typeof(Microsoft.Extensions.Caching.Distributed.IDistributedCache).Assembly);

            return test;
        }

        [Fact]
        public async Task WhenServiceInjectingIDistributedCacheCallsJsonSerializerDirectlyThenCache012IsReported()
        {
            const string source = @"
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace TestApp
{
    public class UserProfileService
    {
        private readonly IDistributedCache _cache;

        public UserProfileService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public byte[] SerializeProfile(object profile)
            => {|#0:JsonSerializer.SerializeToUtf8Bytes(profile)|};
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(NoInlineJsonSerializerForCacheAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("UserProfileService"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenCacheSerializerClassCallsJsonSerializerThenNoDiagnosticIsReported()
        {
            const string source = @"
using System.Text.Json;

namespace TestApp
{
    public static class CacheSerializer
    {
        public static byte[] Serialize<T>(T value)
            => JsonSerializer.SerializeToUtf8Bytes(value);

        public static T? Deserialize<T>(byte[]? data)
            => data is null ? default : JsonSerializer.Deserialize<T>(data);
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenJsonSerializerUsedOutsideCacheContextThenNoDiagnosticIsReported()
        {
            const string source = @"
using System.Text.Json;

namespace TestApp
{
    public class ReportExportService
    {
        public string SerializeReport(object report)
            => JsonSerializer.Serialize(report);
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
