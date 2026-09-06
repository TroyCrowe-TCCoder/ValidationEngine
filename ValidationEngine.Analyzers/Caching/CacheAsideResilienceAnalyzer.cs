using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Caching
{
    /// <summary>
    /// Enforces GlobalCachingStandards.md caching.9.1: the cache-aside pattern requires that cache
    /// reads and writes never allow a cache failure to break the calling operation. On a cache error,
    /// a warning must be logged and the call must fall through to the data source — it must not throw.
    /// Flags IDistributedCache.GetAsync/SetAsync/RemoveAsync/etc. call sites that are not enclosed in
    /// a try block, or whose enclosing catch clause rethrows instead of degrading gracefully.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CacheAsideResilienceAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CACHE014";

        private static readonly LocalizableString Title =
            "Cache reads and writes must not allow cache failures to throw to the caller";

        private static readonly LocalizableString MessageFormat =
            "'{0}' calls IDistributedCache.{1} without a surrounding try/catch that falls through on failure; a cache error must be logged as a warning and the call must fall through to the data source instead of throwing, per GlobalCachingStandards.md caching.9.1";

        private static readonly LocalizableString Description =
            "The cache-aside pattern requires that cache failures degrade performance, not break " +
            "functionality. Every IDistributedCache call must be enclosed in a try block whose catch " +
            "clause does not rethrow, so a cache outage falls through to the data source instead of " +
            "propagating to the caller. See GlobalCachingStandards.md Section 9.1.";

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

                if (distributedCacheSymbol is null)
                {
                    // IDistributedCache is not referenced by this compilation; nothing to flag.
                    return;
                }

                compilationContext.RegisterSyntaxNodeAction(
                    syntaxContext => AnalyzeInvocation(syntaxContext, distributedCacheSymbol),
                    SyntaxKind.InvocationExpression);
            });
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, INamedTypeSymbol distributedCacheSymbol)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            {
                return;
            }

            ISymbol? invokedSymbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol;

            if (invokedSymbol is not IMethodSymbol methodSymbol ||
                !CachingWellKnownTypes.IsDistributedCacheMethod(methodSymbol, distributedCacheSymbol))
            {
                return;
            }

            MethodDeclarationSyntax? containingMethod = CachingWellKnownTypes.GetContainingMethod(invocation);

            if (containingMethod is null)
            {
                return;
            }

            TryStatementSyntax? enclosingTry = CachingWellKnownTypes.FindEnclosingTryStatement(invocation);

            bool isResilient = enclosingTry is not null && !CachingWellKnownTypes.AnyCatchClauseRethrows(enclosingTry);

            if (!isResilient)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    invocation.GetLocation(),
                    containingMethod.Identifier.ValueText,
                    memberAccess.Name.Identifier.ValueText));
            }
        }
    }
}
