using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Caching
{
    /// <summary>
    /// Enforces GlobalCachingStandards.md caching.9.2: cache hits, misses, and errors must be
    /// logged for observability. As a static heuristic, this analyzer flags a method that calls
    /// IDistributedCache.GetAsync/SetAsync/RemoveAsync but never calls ILogger&lt;T&gt; anywhere in
    /// that method body, meaning no hit/miss/error telemetry is ever recorded for that cache
    /// operation.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CacheObservabilityAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CACHE015";

        private static readonly LocalizableString Title =
            "Cache operations must log hits, misses, and errors for observability";

        private static readonly LocalizableString MessageFormat =
            "'{0}' calls IDistributedCache.{1} but never calls ILogger<T> anywhere in the method; cache hits, misses, and errors must be logged using structured named properties, per GlobalCachingStandards.md caching.9.2";

        private static readonly LocalizableString Description =
            "Cache hits, misses, and errors must be logged (Debug for hits/misses, Warning for read/write/deserialization errors) using structured logging with named properties. Consistent logging enables performance diagnostics, cache effectiveness monitoring, and issue investigation. See GlobalCachingStandards.md Section 9.2.";

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
                INamedTypeSymbol? distributedCacheSymbol = compilationContext.Compilation.GetTypeByMetadataName(
                    CachingWellKnownTypes.IDistributedCacheMetadataName);
                INamedTypeSymbol? loggerGenericSymbol = compilationContext.Compilation.GetTypeByMetadataName(
                    CachingWellKnownTypes.LoggerInterfaceMetadataName);
                INamedTypeSymbol? loggerSymbol = compilationContext.Compilation.GetTypeByMetadataName(
                    CachingWellKnownTypes.LoggerNonGenericInterfaceMetadataName);

                if (distributedCacheSymbol is null || (loggerGenericSymbol is null && loggerSymbol is null))
                {
                    // Either IDistributedCache or Microsoft.Extensions.Logging.Abstractions is not
                    // referenced by this compilation; there is no way for this method to comply,
                    // so skip to avoid false positives.
                    return;
                }

                compilationContext.RegisterSyntaxNodeAction(
                    syntaxContext => AnalyzeMethod(syntaxContext, distributedCacheSymbol, loggerGenericSymbol, loggerSymbol),
                    SyntaxKind.MethodDeclaration);
            });
        }

        private static void AnalyzeMethod(
            SyntaxNodeAnalysisContext context,
            INamedTypeSymbol distributedCacheSymbol,
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
                if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                {
                    return false;
                }

                ISymbol? symbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol;

                return symbol is IMethodSymbol methodSymbol &&
                       CachingWellKnownTypes.IsDistributedCacheMethod(methodSymbol, distributedCacheSymbol);
            });

            if (cacheCall is null)
            {
                return;
            }

            bool hasLoggerCall = invocations.Any(invocation =>
            {
                if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                {
                    return false;
                }

                if (!CachingWellKnownTypes.IsLoggerMethodName(memberAccess.Name.Identifier.ValueText))
                {
                    return false;
                }

                ISymbol? symbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol;

                return symbol is IMethodSymbol methodSymbol &&
                       methodSymbol.ReceiverType is not null &&
                       CachingWellKnownTypes.IsLoggerReceiverType(methodSymbol.ReceiverType, loggerGenericSymbol, loggerSymbol);
            });

            if (hasLoggerCall)
            {
                return;
            }

            var memberAccessSyntax = (MemberAccessExpressionSyntax)cacheCall.Expression;
            string methodName = method.Identifier.ValueText;
            string cacheMethodName = memberAccessSyntax.Name.Identifier.ValueText;

            context.ReportDiagnostic(Diagnostic.Create(Rule, memberAccessSyntax.Name.GetLocation(), methodName, cacheMethodName));
        }
    }
}
