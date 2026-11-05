using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Caching
{
    /// <summary>
    /// Enforces GlobalCachingStandards.md caching.9.1: RedisCacheManager retries transient failures and
    /// converts any remaining failure into a miss or no-op, so application code must not wrap
    /// ICacheManager calls in try/catch. Flags ICacheManager calls inside a try block that has a catch clause.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CacheAsideResilienceAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CACHE014";

        private static readonly LocalizableString Title =
            "ICacheManager calls must not be wrapped in try/catch";

        private static readonly LocalizableString MessageFormat =
            "'{0}' wraps ICacheManager.{1} in a try/catch; ICacheManager never throws and handles Redis failures itself, so the catch block must be removed, per GlobalCachingStandards.md caching.9.1";

        private static readonly LocalizableString Description =
            "RedisCacheManager retries transient Redis failures and converts any remaining failure into " +
            "a miss (reads) or a no-op (writes and removals). A catch block around a cache call hides " +
            "nothing the package has not already handled. See GlobalCachingStandards.md Section 9.1.";

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

                if (cacheManagerSymbol is null)
                {
                    // ICacheManager is not referenced by this compilation; nothing to flag.
                    return;
                }

                compilationContext.RegisterSyntaxNodeAction(
                    syntaxContext => AnalyzeInvocation(syntaxContext, cacheManagerSymbol),
                    SyntaxKind.InvocationExpression);
            });
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, INamedTypeSymbol cacheManagerSymbol)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            {
                return;
            }

            ISymbol? invokedSymbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol;

            if (invokedSymbol is not IMethodSymbol methodSymbol ||
                !CachingWellKnownTypes.IsCacheManagerMethod(methodSymbol, cacheManagerSymbol))
            {
                return;
            }

            MethodDeclarationSyntax? containingMethod = CachingWellKnownTypes.GetContainingMethod(invocation);

            if (containingMethod is null)
            {
                return;
            }

            TryStatementSyntax? enclosingTry = invocation.Ancestors()
                .OfType<TryStatementSyntax>()
                .FirstOrDefault(t => t.Catches.Count > 0 && t.Block.Span.Contains(invocation.Span));

            if (enclosingTry is not null)
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
