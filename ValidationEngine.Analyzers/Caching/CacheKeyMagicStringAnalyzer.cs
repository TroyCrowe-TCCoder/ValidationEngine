using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Caching
{
    /// <summary>
    /// Enforces GlobalCachingStandards.md caching.4: every category and id passed to ICacheManager
    /// (TryGetAsync/SetAsync/RemoveAsync) must come from the dedicated "CacheKeys" static class. Inline
    /// magic-string values (raw literals or ad-hoc interpolation/concatenation) are not permitted.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CacheKeyMagicStringAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CACHE004";

        private static readonly ImmutableArray<string> CacheAccessMethodNames = ImmutableArray.Create(
            "TryGetAsync", "SetAsync", "RemoveAsync");

        private static readonly ImmutableArray<string> KeyParameterNames = ImmutableArray.Create("category", "id");

        private static readonly LocalizableString Title =
            "Cache category and id must come from the CacheKeys class, not an inline literal or interpolation";

        private static readonly LocalizableString MessageFormat =
            "The '{1}' argument to '{0}' is built inline instead of using the CacheKeys class; define it in a dedicated CacheKeys static class per GlobalCachingStandards.md caching.4";

        private static readonly LocalizableString Description =
            "All cache categories and ids must be defined as constants or static members in a dedicated " +
            "CacheKeys static class. Magic strings (literals or inline interpolated/concatenated strings) " +
            "scattered throughout the codebase are not permitted. See GlobalCachingStandards.md Section 4.";

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
                INamedTypeSymbol? cacheManagerSymbol = compilationContext.Compilation.GetTypeByMetadataName(
                    CachingWellKnownTypes.ICacheManagerMetadataName);

                if (cacheManagerSymbol is null)
                {
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

            string methodName = memberAccess.Name.Identifier.ValueText;

            if (!CacheAccessMethodNames.Contains(methodName))
            {
                return;
            }

            ISymbol? invokedSymbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol;

            if (invokedSymbol is not IMethodSymbol methodSymbol ||
                !CachingWellKnownTypes.IsCacheManagerMethod(methodSymbol, cacheManagerSymbol))
            {
                return;
            }

            foreach (string parameterName in KeyParameterNames)
            {
                ArgumentSyntax? argument = CachingWellKnownTypes.GetArgumentForParameter(invocation, methodSymbol, parameterName);

                if (argument is not null && IsInlineKeyExpression(argument.Expression))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, argument.Expression.GetLocation(), methodName, parameterName));
                }
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
