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
            return CacheManagerTestSupport.Create<NoInlineJsonSerializerForCacheAnalyzer>(source);
        }

        [Fact]
        public async Task WhenServiceInjectingICacheManagerCallsJsonSerializerDirectlyThenCache012IsReported()
        {
            const string source = @"
using System.Text.Json;
using RedisCacheManager.Caching;

namespace TestApp
{
    public class UserProfileService
    {
        private readonly ICacheManager _cache;

        public UserProfileService(ICacheManager cache)
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
        public async Task WhenClassDoesNotUseICacheManagerThenNoDiagnosticIsReported()
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
