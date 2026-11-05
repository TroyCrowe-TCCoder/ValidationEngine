using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Caching
{
    /// <summary>
    /// Enforces GlobalCachingStandards.md caching.9.2: RedisCacheManager publishes the cache
    /// telemetry, so applications must not duplicate it with per-call hit/miss logging. As a static
    /// heuristic, this analyzer flags an ILogger call whose message text mentions both "cache" and
    /// "hit" or "miss" inside a method that calls ICacheManager.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CacheObservabilityAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CACHE015";

        private static readonly LocalizableString Title =
            "Cache hits and misses must not be logged; RedisCacheManager publishes the telemetry";

        private static readonly LocalizableString MessageFormat =
            "'{0}' logs a cache hit or miss around an ICacheManager call; RedisCacheManager already publishes redis_cache.hits and redis_cache.misses, so per-call hit/miss logging must be removed, per GlobalCachingStandards.md caching.9.2";

        private static readonly LocalizableString Description =
            "RedisCacheManager publishes cache hit, miss, set, removal, and error telemetry as metrics and " +
            "Warning logs. Applications must not duplicate it with per-call hit and miss logging. " +
            "See GlobalCachingStandards.md Section 9.2.";

        private const string Category = "Caching";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: "https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalCachingStandards.md");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(compilationContext =>
            {
                INamedTypeSymbol? cacheManagerSymbol = compilationContext.Compilation.GetTypeByMetadataName(
                    CachingWellKnownTypes.ICacheManagerMetadataName);
                INamedTypeSymbol? loggerGenericSymbol = compilationContext.Compilation.GetTypeByMetadataName(
                    CachingWellKnownTypes.LoggerInterfaceMetadataName);
                INamedTypeSymbol? loggerSymbol = compilationContext.Compilation.GetTypeByMetadataName(
                    CachingWellKnownTypes.LoggerNonGenericInterfaceMetadataName);

                if (cacheManagerSymbol is null || (loggerGenericSymbol is null && loggerSymbol is null))
                {
                    // Either ICacheManager or Microsoft.Extensions.Logging.Abstractions is not
                    // referenced by this compilation; nothing to flag.
                    return;
                }

                compilationContext.RegisterSyntaxNodeAction(
                    syntaxContext => AnalyzeMethod(syntaxContext, cacheManagerSymbol, loggerGenericSymbol, loggerSymbol),
                    SyntaxKind.MethodDeclaration);
            });
        }

        private static void AnalyzeMethod(
            SyntaxNodeAnalysisContext context,
            INamedTypeSymbol cacheManagerSymbol,
            INamedTypeSymbol? loggerGenericSymbol,
            INamedTypeSymbol? loggerSymbol)
        {
            var method = (MethodDeclarationSyntax)context.Node;

            SyntaxNode? body = (SyntaxNode?)method.Body ?? method.ExpressionBody;

            if (body is null)
            {
                return;
            }

            var invocations = body.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().ToList();

            InvocationExpressionSyntax? cacheCall = invocations.FirstOrDefault(invocation =>
            {
                if (invocation.Expression is not MemberAccessExpressionSyntax)
                {
                    return false;
                }

                ISymbol? symbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol;

                return symbol is IMethodSymbol methodSymbol &&
                       CachingWellKnownTypes.IsCacheManagerMethod(methodSymbol, cacheManagerSymbol);
            });

            if (cacheCall is null)
            {
                return;
            }

            string methodName = method.Identifier.ValueText;

            foreach (InvocationExpressionSyntax invocation in invocations)
            {
                if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess ||
                    !CachingWellKnownTypes.IsLoggerMethodName(memberAccess.Name.Identifier.ValueText))
                {
                    continue;
                }

                ISymbol? symbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol;

                if (symbol is not IMethodSymbol methodSymbol ||
                    methodSymbol.ReceiverType is null ||
                    !CachingWellKnownTypes.IsLoggerReceiverType(methodSymbol.ReceiverType, loggerGenericSymbol, loggerSymbol))
                {
                    continue;
                }

                if (MessageMentionsCacheHitOrMiss(invocation))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), methodName));
                }
            }
        }

        private static bool MessageMentionsCacheHitOrMiss(InvocationExpressionSyntax logCall)
        {
            foreach (ArgumentSyntax argument in logCall.ArgumentList.Arguments)
            {
                if (argument.Expression is not (LiteralExpressionSyntax or InterpolatedStringExpressionSyntax))
                {
                    continue;
                }

                string text = argument.Expression.ToString();

                if (text.IndexOf("cache", System.StringComparison.OrdinalIgnoreCase) >= 0 &&
                    (text.IndexOf("hit", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                     text.IndexOf("miss", System.StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
