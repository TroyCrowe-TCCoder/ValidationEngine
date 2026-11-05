using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Caching
{
    /// <summary>
    /// Enforces GlobalCachingStandards.md caching.6.2: a method marked <c>[LongLivedCache]</c> asserts
    /// that the entries it writes follow the long-lived pattern, so every <c>ICacheManager.SetAsync</c>
    /// call in it must pass a ttl sourced from <c>CacheOptions.LongLivedExpirationMinutes</c>. Omitting
    /// the ttl, hardcoding it, or passing a time-bounded value is flagged.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CacheLongLivedExpirationAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CACHE016";

        private static readonly LocalizableString Title =
            "[LongLivedCache] methods must pass CacheOptions.LongLivedExpirationMinutes as the SetAsync ttl";

        private static readonly LocalizableString MessageFormat =
            "'{0}' is marked [LongLivedCache] but its SetAsync call {1}; the long-lived pattern requires the ttl to come from CacheOptions.LongLivedExpirationMinutes, per GlobalCachingStandards.md caching.6.2";

        private static readonly LocalizableString Description =
            "A cache entry authored using the long-lived pattern (stable reference data that changes only " +
            "on explicit administrator write) must be written with the long-lived backstop ttl from " +
            "CacheOptions.LongLivedExpirationMinutes. Omitting the ttl, hardcoding a value, or passing a " +
            "time-bounded value contradicts the declared lifecycle. See GlobalCachingStandards.md Section 6.2.";

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

            if (!CachingWellKnownTypes.IsInvocationOfMember(invocation, CachingWellKnownTypes.SetAsyncMethodName))
            {
                return;
            }

            if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol methodSymbol ||
                !CachingWellKnownTypes.IsCacheManagerMethod(methodSymbol, cacheManagerSymbol))
            {
                return;
            }

            MethodDeclarationSyntax? containingMethod = CachingWellKnownTypes.GetContainingMethod(invocation);

            if (containingMethod is null ||
                !CachingWellKnownTypes.HasLongLivedCacheAttribute(containingMethod, context.SemanticModel, context.CancellationToken))
            {
                return;
            }

            ArgumentSyntax? ttlArgument = CachingWellKnownTypes.GetArgumentForParameter(invocation, methodSymbol, "ttl");

            if (ttlArgument is null)
            {
                Report(context, invocation.GetLocation(), containingMethod, "does not pass a ttl");
                return;
            }

            if (ReferencesLongLivedExpiration(ttlArgument.Expression, context))
            {
                return;
            }

            Report(context, ttlArgument.Expression.GetLocation(), containingMethod, "passes a ttl that is not CacheOptions.LongLivedExpirationMinutes");
        }

        private static bool ReferencesLongLivedExpiration(ExpressionSyntax expression, SyntaxNodeAnalysisContext context)
        {
            if (MentionsLongLivedExpiration(expression))
            {
                return true;
            }

            // Follow a local variable one step so `var ttl = TimeSpan.FromMinutes(o.LongLivedExpirationMinutes)` is accepted.
            if (expression is IdentifierNameSyntax identifier &&
                context.SemanticModel.GetSymbolInfo(identifier, context.CancellationToken).Symbol is ILocalSymbol local)
            {
                foreach (SyntaxReference reference in local.DeclaringSyntaxReferences)
                {
                    if (reference.GetSyntax(context.CancellationToken) is VariableDeclaratorSyntax { Initializer.Value: { } initializer } &&
                        MentionsLongLivedExpiration(initializer))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool MentionsLongLivedExpiration(SyntaxNode node)
        {
            return node.DescendantNodesAndSelf()
                .OfType<IdentifierNameSyntax>()
                .Any(i => i.Identifier.ValueText == CachingWellKnownTypes.LongLivedExpirationPropertyName);
        }

        private static void Report(SyntaxNodeAnalysisContext context, Location location, MethodDeclarationSyntax method, string detail)
        {
            context.ReportDiagnostic(Diagnostic.Create(Rule, location, method.Identifier.ValueText, detail));
        }
    }
}
