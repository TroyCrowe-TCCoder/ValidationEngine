using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Caching
{
    /// <summary>
    /// Enforces GlobalCachingStandards.md caching.3: the cache must be registered with
    /// <c>AddRedisCacheManager(builder.Configuration)</c>. Registering Redis through
    /// <c>AddStackExchangeRedisCache</c> is not permitted; the TCCoder.RedisCacheManager package owns the
    /// connection, retry and options.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class RedisRegistrationOptionsAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CACHE002";

        private const string RegistrationMethodName = "AddStackExchangeRedisCache";

        private static readonly LocalizableString Title =
            "Redis must be registered with AddRedisCacheManager";

        private static readonly LocalizableString MessageFormat =
            "AddStackExchangeRedisCache is not permitted; register the cache with AddRedisCacheManager(builder.Configuration), per GlobalCachingStandards.md caching.3";

        private static readonly LocalizableString Description =
            "The cache must be registered with AddRedisCacheManager(builder.Configuration) from the " +
            "TCCoder.RedisCacheManager package. Registering Redis through AddStackExchangeRedisCache is not " +
            "permitted. See GlobalCachingStandards.md Section 3.";

        private const string Category = "Caching";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: "https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalCachingStandards.md");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess ||
                memberAccess.Name.Identifier.ValueText != RegistrationMethodName)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, memberAccess.Name.GetLocation()));
        }
    }
}
