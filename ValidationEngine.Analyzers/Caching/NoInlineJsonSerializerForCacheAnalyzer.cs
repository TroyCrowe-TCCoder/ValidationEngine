using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Caching
{
    /// <summary>
    /// Enforces GlobalCachingStandards.md caching.8.2: ICacheManager owns serialization, so no
    /// System.Text.Json.JsonSerializer call may appear inside any class that injects/uses ICacheManager.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NoInlineJsonSerializerForCacheAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CACHE012";

        private static readonly LocalizableString Title =
            "Cache-aware classes must not call JsonSerializer; ICacheManager owns serialization";

        private static readonly LocalizableString MessageFormat =
            "'{0}' uses ICacheManager and also calls System.Text.Json.JsonSerializer directly; ICacheManager owns serialization and deserialization, so consumers must pass and receive typed values, per GlobalCachingStandards.md caching.8.2";

        private static readonly LocalizableString Description =
            "ICacheManager serializes and deserializes cache values itself. Consumers must not call " +
            "JsonSerializer, convert to byte[], or wrap serialization in helpers; pass the typed value " +
            "to SetAsync and read the typed value from TryGetAsync. See GlobalCachingStandards.md Section 8.2.";

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
                INamedTypeSymbol? jsonSerializerSymbol = compilationContext.Compilation.GetTypeByMetadataName(
                    CachingWellKnownTypes.JsonSerializerMetadataName);
                INamedTypeSymbol? cacheManagerSymbol = compilationContext.Compilation.GetTypeByMetadataName(
                    CachingWellKnownTypes.ICacheManagerMetadataName);

                if (jsonSerializerSymbol is null || cacheManagerSymbol is null)
                {
                    // System.Text.Json or ICacheManager is not referenced by this compilation;
                    // nothing to flag.
                    return;
                }

                compilationContext.RegisterSyntaxNodeAction(
                    syntaxContext => AnalyzeInvocation(syntaxContext, jsonSerializerSymbol, cacheManagerSymbol),
                    SyntaxKind.InvocationExpression);
            });
        }

        private static void AnalyzeInvocation(
            SyntaxNodeAnalysisContext context,
            INamedTypeSymbol jsonSerializerSymbol,
            INamedTypeSymbol cacheManagerSymbol)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            ISymbol? invokedSymbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol;

            if (invokedSymbol is not IMethodSymbol methodSymbol ||
                !SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, jsonSerializerSymbol))
            {
                return;
            }

            TypeDeclarationSyntax? containingType = CachingWellKnownTypes.GetContainingType(invocation);

            if (containingType is null)
            {
                return;
            }

            if (!CachingWellKnownTypes.TypeUsesCacheManager(containingType, context.SemanticModel, cacheManagerSymbol))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), containingType.Identifier.ValueText));
        }
    }
}
