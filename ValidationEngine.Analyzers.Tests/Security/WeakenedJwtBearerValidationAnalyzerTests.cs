using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Security.Tests
{
    public class WeakenedJwtBearerValidationAnalyzerTests
    {
        private static CSharpAnalyzerTest<WeakenedJwtBearerValidationAnalyzer, DefaultVerifier> CreateTest(
            string source,
            string fileName)
        {
            var test = new CSharpAnalyzerTest<WeakenedJwtBearerValidationAnalyzer, DefaultVerifier>
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            test.TestState.Sources.Add((fileName, source));

            return test;
        }

        private const string JwtBearerOptionsStub = @"
namespace TestApp
{
    public class TokenValidationParameters
    {
        public bool ValidateIssuer { get; set; }
        public bool ValidateAudience { get; set; }
    }

    public class JwtBearerOptions
    {
        public bool MapInboundClaims { get; set; }
        public bool RequireHttpsMetadata { get; set; }
        public TokenValidationParameters TokenValidationParameters { get; set; }
    }

    public static class AuthBuilderExtensions
    {
        public static void AddJwtBearer(this object builder, System.Action<JwtBearerOptions> configure)
        {
        }
    }
}
";

        [Fact]
        public async Task WhenRequireHttpsMetadataIsFalseThenSec003IsReported()
        {
            string source = JwtBearerOptionsStub + @"
namespace TestApp
{
    public class Startup
    {
        public void Configure(object services)
        {
            services.AddJwtBearer(options =>
            {
                {|#0:options.RequireHttpsMetadata = false|};
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true
                };
            });
        }
    }
}
";

            var test = CreateTest(source, "Startup.cs");
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(WeakenedJwtBearerValidationAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("RequireHttpsMetadata", "false", "true"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenMapInboundClaimsIsTrueThenSec003IsReported()
        {
            string source = JwtBearerOptionsStub + @"
namespace TestApp
{
    public class Startup
    {
        public void Configure(object services)
        {
            services.AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;
                {|#0:options.MapInboundClaims = true|};
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true
                };
            });
        }
    }
}
";

            var test = CreateTest(source, "Startup.cs");
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(WeakenedJwtBearerValidationAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("MapInboundClaims", "true", "false"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenAllFourParametersAreCorrectThenNoDiagnosticIsReported()
        {
            string source = JwtBearerOptionsStub + @"
namespace TestApp
{
    public class Startup
    {
        public void Configure(object services)
        {
            services.AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true
                };
            });
        }
    }
}
";

            var test = CreateTest(source, "Startup.cs");

            await test.RunAsync();
        }
    }
}
