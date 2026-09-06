using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Caching
{
    /// <summary>
    /// Enforces GlobalCachingStandards.md caching.4.2: all cache keys must be defined as
    /// constants/static members in a dedicated "CacheKeys" static class. Inline magic-string keys
    /// (raw literals or ad-hoc interpolation/concatenation) at IDistributedCache call sites are not
    /// permitted.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CacheKeyMagicStringAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CACHE004";

        private static readonly ImmutableArray<string> CacheAccessMethodNames = ImmutableArray.Create(
            "GetAsync", "SetAsync", "RemoveAsync", "RefreshAsync");

        private static readonly LocalizableString Title =
            "Cache key must come from the CacheKeys class, not an inline literal or interpolation";

        private static readonly LocalizableString MessageFormat =
            "Cache key argument to '{0}' is built inline instead of using the CacheKeys class; define it in a dedicated CacheKeys static class per GlobalCachingStandards.md caching.4.2";

        private static readonly LocalizableString Description =
            "All cache keys must be defined as constants or static members in a dedicated CacheKeys static " +
            "class. Magic string keys (literals or inline interpolated/concatenated strings) scattered " +
            "throughout the codebase are not permitted. See GlobalCachingStandards.md Section 4.2.";

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

            context.RegisterCompilationStartAction(compilationContext =>
            {
                INamedTypeSymbol? distributedCacheSymbol = compilationContext.Compilation.GetTypeByMetadataName(
                    CachingWellKnownTypes.IDistributedCacheMetadataName);

                if (distributedCacheSymbol is null)
                {
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

            string methodName = memberAccess.Name.Identifier.ValueText;

            if (!CacheAccessMethodNames.Contains(methodName))
            {
                return;
            }

            ISymbol? invokedSymbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol;

            if (invokedSymbol is not IMethodSymbol methodSymbol ||
                !CachingWellKnownTypes.IsDistributedCacheMethod(methodSymbol, distributedCacheSymbol))
            {
                return;
            }

            if (invocation.ArgumentList.Arguments.Count == 0)
            {
                return;
            }

            ExpressionSyntax keyArgument = invocation.ArgumentList.Arguments[0].Expression;

            if (IsInlineKeyExpression(keyArgument))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, keyArgument.GetLocation(), methodName));
            }
        }

        private static bool IsInlineKeyExpression(ExpressionSyntax expression)
        {
            return expression is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression } ||
                   expression is InterpolatedStringExpressionSyntax ||
                   expression is BinaryExpressionSyntax { RawKind: (int)SyntaxKind.AddExpression };
        }
    }
}
