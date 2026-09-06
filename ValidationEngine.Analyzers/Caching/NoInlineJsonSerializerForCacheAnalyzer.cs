using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Caching
{
    /// <summary>
    /// Enforces GlobalCachingStandards.md caching.8.2: a shared CacheSerializer helper must be used
    /// rather than inline System.Text.Json.JsonSerializer calls scattered throughout the codebase.
    /// Flags JsonSerializer.Serialize/Deserialize usage inside any class that injects/uses
    /// IDistributedCache, unless the call site is inside the dedicated CacheSerializer class itself.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NoInlineJsonSerializerForCacheAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CACHE012";

        private static readonly LocalizableString Title =
            "Cache serialization must go through the shared CacheSerializer helper";

        private static readonly LocalizableString MessageFormat =
            "'{0}' calls System.Text.Json.JsonSerializer directly instead of using the shared CacheSerializer helper; a shared cache serialization helper must be used rather than inline JsonSerializer calls, per GlobalCachingStandards.md caching.8.2";

        private static readonly LocalizableString Description =
            "A shared cache serialization helper (CacheSerializer) must encapsulate serialization, " +
            "deserialization, and byte[] conversion for cache entries. Inline JsonSerializer calls " +
            "scattered across cache-aware services duplicate this logic and must be replaced with a " +
            "call to the shared helper. See GlobalCachingStandards.md Section 8.2.";

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
                INamedTypeSymbol? distributedCacheSymbol = compilationContext.Compilation.GetTypeByMetadataName(
                    CachingWellKnownTypes.IDistributedCacheMetadataName);

                if (jsonSerializerSymbol is null || distributedCacheSymbol is null)
                {
                    // System.Text.Json or IDistributedCache is not referenced by this compilation;
                    // nothing to flag.
                    return;
                }

                compilationContext.RegisterSyntaxNodeAction(
                    syntaxContext => AnalyzeInvocation(syntaxContext, jsonSerializerSymbol, distributedCacheSymbol),
                    SyntaxKind.InvocationExpression);
            });
        }

        private static void AnalyzeInvocation(
            SyntaxNodeAnalysisContext context,
            INamedTypeSymbol jsonSerializerSymbol,
            INamedTypeSymbol distributedCacheSymbol)
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

            string containingTypeName = containingType.Identifier.ValueText;

            if (containingTypeName.EndsWith(CachingWellKnownTypes.CacheSerializerClassName, StringComparison.Ordinal))
            {
                // The dedicated CacheSerializer helper is exactly where JsonSerializer calls belong.
                return;
            }

            if (!CachingWellKnownTypes.TypeUsesDistributedCache(containingType, context.SemanticModel, distributedCacheSymbol))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), containingTypeName));
        }
    }
}
