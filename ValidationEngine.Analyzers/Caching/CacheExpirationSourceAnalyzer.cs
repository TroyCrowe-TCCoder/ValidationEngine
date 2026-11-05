using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Caching
{
    /// <summary>
    /// Enforces GlobalCachingStandards.md caching.6.1/6.3: every <c>ICacheManager.SetAsync</c> call must
    /// pass a <c>ttl</c> sourced from IOptionsMonitor&lt;CacheOptions&gt;.CurrentValue. Omitting the ttl (which
    /// falls back to the package default), passing null, or passing a hardcoded TimeSpan is not permitted.
    /// IOptions&lt;T&gt; must not be used (it is a startup snapshot that never observes configuration changes).
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CacheExpirationSourceAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CACHE006";

        private const string IOptionsGenericName = "IOptions";
        private const string TimeSpanTypeName = "TimeSpan";
        private const string CurrentValuePropertyName = "CurrentValue";

        private static readonly LocalizableString Title =
            "Cache expiration must be sourced from IOptionsMonitor<CacheOptions>, not IOptions<T> or a hardcoded value";

        private static readonly LocalizableString MessageFormat = "{0}";

        private static readonly LocalizableString Description =
            "Every ICacheManager.SetAsync call must pass a ttl read from IOptionsMonitor<CacheOptions>.CurrentValue. " +
            "Omitting the ttl relies on the package default, and hardcoded TimeSpan values bypass configuration " +
            "entirely. IOptions<T> is a startup snapshot and does not pick up configuration changes without an " +
            "application restart. See GlobalCachingStandards.md Sections 6.1 and 6.3.";

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

            context.RegisterSyntaxNodeAction(AnalyzeGenericName, SyntaxKind.GenericName);

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

        private static void AnalyzeGenericName(SyntaxNodeAnalysisContext context)
        {
            var genericName = (GenericNameSyntax)context.Node;

            if (genericName.Identifier.ValueText != IOptionsGenericName)
            {
                return;
            }

            if (genericName.TypeArgumentList.Arguments.Count != 1)
            {
                return;
            }

            string typeArgumentName = genericName.TypeArgumentList.Arguments[0] switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
                _ => string.Empty,
            };

            if (!typeArgumentName.EndsWith(CachingWellKnownTypes.CacheOptionsClassNameSuffix, StringComparison.Ordinal))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                genericName.GetLocation(),
                $"'{genericName}' uses IOptions<{typeArgumentName}> instead of IOptionsMonitor<{typeArgumentName}>; IOptions<T> is a startup snapshot and will not observe configuration changes, per GlobalCachingStandards.md caching.6.3"));
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, INamedTypeSymbol cacheManagerSymbol)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess ||
                memberAccess.Name.Identifier.ValueText != CachingWellKnownTypes.SetAsyncMethodName)
            {
                return;
            }

            if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol methodSymbol ||
                !CachingWellKnownTypes.IsCacheManagerMethod(methodSymbol, cacheManagerSymbol))
            {
                return;
            }

            if (CachingWellKnownTypes.HasLongLivedCacheAttribute(invocation, context.SemanticModel, context.CancellationToken))
            {
                // [LongLivedCache] methods are governed by caching.6.2 (CACHE016).
                return;
            }

            ArgumentSyntax? ttlArgument = CachingWellKnownTypes.GetArgumentForParameter(invocation, methodSymbol, "ttl");

            if (ttlArgument is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    invocation.GetLocation(),
                    "SetAsync does not pass a ttl and would rely on the package DefaultTtl; pass a ttl sourced from IOptionsMonitor<CacheOptions>.CurrentValue, per GlobalCachingStandards.md caching.6.1"));
                return;
            }

            ExpressionSyntax ttlExpression = ttlArgument.Expression;

            if (ttlExpression.IsKind(SyntaxKind.NullLiteralExpression) || ttlExpression.IsKind(SyntaxKind.DefaultLiteralExpression))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    ttlExpression.GetLocation(),
                    "SetAsync passes a null ttl and would rely on the package DefaultTtl; pass a ttl sourced from IOptionsMonitor<CacheOptions>.CurrentValue, per GlobalCachingStandards.md caching.6.1"));
                return;
            }

            if (IsTimeSpanFromInvocation(ttlExpression) && !ReferencesCurrentValue(ttlExpression))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    ttlExpression.GetLocation(),
                    $"SetAsync is passed a hardcoded '{ttlExpression}' ttl instead of one sourced from IOptionsMonitor<CacheOptions>.CurrentValue, per GlobalCachingStandards.md caching.6.3"));
            }
        }

        private static bool IsTimeSpanFromInvocation(ExpressionSyntax expression)
        {
            return expression is InvocationExpressionSyntax
            {
                Expression: MemberAccessExpressionSyntax
                {
                    Expression: IdentifierNameSyntax { Identifier.ValueText: TimeSpanTypeName },
                },
            };
        }

        private static bool ReferencesCurrentValue(ExpressionSyntax expression)
        {
            return expression.DescendantNodesAndSelf()
                .OfType<MemberAccessExpressionSyntax>()
                .Any(m => m.Name.Identifier.ValueText == CurrentValuePropertyName);
        }
    }
}
