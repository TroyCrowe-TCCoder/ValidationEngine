using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Caching.Tests
{
    public class CacheKeyTenantIdSegmentAnalyzerTests
    {
        private static CSharpAnalyzerTest<CacheKeyTenantIdSegmentAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            return new CSharpAnalyzerTest<CacheKeyTenantIdSegmentAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };
        }

        [Fact]
        public async Task WhenCacheKeyMethodHasNoTenantIdParameterThenCache005IsReported()
        {
            const string source = @"
namespace TestApp
{
    public static class CacheKeys
    {
        public static string {|#0:ItemTypes|}(int clientId)
            => $""{clientId}:item-types"";
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(CacheKeyTenantIdSegmentAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("ItemTypes"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenTenantIdIsNotTheFirstKeySegmentThenCache005IsReported()
        {
            const string source = @"
namespace TestApp
{
    public static class CacheKeys
    {
        public static string ClientConfig(int tenantId, int clientId)
            => {|#0:$""{clientId}:{tenantId}:config""|};
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(CacheKeyTenantIdSegmentAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("ClientConfig"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenTenantIdIsTheFirstKeySegmentThenNoDiagnosticIsReported()
        {
            const string source = @"
namespace TestApp
{
    public static class CacheKeys
    {
        public static string ClientConfig(int tenantId, int clientId)
            => $""{tenantId}:{clientId}:config"";
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
