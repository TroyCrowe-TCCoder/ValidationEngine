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
    /// Enforces GlobalCachingStandards.md caching.8.1: JSON via System.Text.Json is the required
    /// serialization format for all cache entries. Newtonsoft.Json must not be used for cache
    /// serialization. Flags Newtonsoft.Json.JsonConvert usage inside a dedicated CacheSerializer-named
    /// class, or inside any class that also injects/uses IDistributedCache.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NoNewtonsoftJsonForCacheAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CACHE008";

        private static readonly LocalizableString Title =
            "Newtonsoft.Json must not be used for cache serialization";

        private static readonly LocalizableString MessageFormat =
            "'{0}' uses Newtonsoft.Json.JsonConvert for cache serialization; System.Text.Json is the required serialization format for cache entries, per GlobalCachingStandards.md caching.8.1";

        private static readonly LocalizableString Description =
            "JSON via System.Text.Json is the required serialization format for all cache entries. " +
            "Newtonsoft.Json must not be used for cache serialization in new code. See " +
            "GlobalCachingStandards.md Section 8.1.";

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
                INamedTypeSymbol? jsonConvertSymbol = compilationContext.Compilation.GetTypeByMetadataName(
                    CachingWellKnownTypes.NewtonsoftJsonConvertMetadataName);
                INamedTypeSymbol? distributedCacheSymbol = compilationContext.Compilation.GetTypeByMetadataName(
                    CachingWellKnownTypes.IDistributedCacheMetadataName);

                if (jsonConvertSymbol is null)
                {
                    // Newtonsoft.Json is not referenced by this compilation; nothing to flag.
                    return;
                }

                compilationContext.RegisterSyntaxNodeAction(
                    syntaxContext => AnalyzeInvocation(syntaxContext, jsonConvertSymbol, distributedCacheSymbol),
                    SyntaxKind.InvocationExpression);
            });
        }

        private static void AnalyzeInvocation(
            SyntaxNodeAnalysisContext context,
            INamedTypeSymbol jsonConvertSymbol,
            INamedTypeSymbol? distributedCacheSymbol)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            ISymbol? invokedSymbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol;

            if (invokedSymbol is not IMethodSymbol methodSymbol ||
                !SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, jsonConvertSymbol))
            {
                return;
            }

            TypeDeclarationSyntax? containingType = CachingWellKnownTypes.GetContainingType(invocation);

            if (containingType is null)
            {
                return;
            }

            string containingTypeName = containingType.Identifier.ValueText;

            bool isCacheSerializationContext =
                containingTypeName.EndsWith(CachingWellKnownTypes.CacheSerializerClassName, StringComparison.Ordinal) ||
                (distributedCacheSymbol is not null && CachingWellKnownTypes.TypeUsesDistributedCache(containingType, context.SemanticModel, distributedCacheSymbol));

            if (!isCacheSerializationContext)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), containingTypeName));
        }
    }
}
