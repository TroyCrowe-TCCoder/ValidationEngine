using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Caching
{
    /// <summary>
    /// Enforces GlobalCachingStandards.md caching.3: ICacheManager (TCCoder.RedisCacheManager) is the
    /// only approved cache access. IDistributedCache must not be used directly.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NoDistributedCacheAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CACHE017";

        private static readonly LocalizableString Title =
            "IDistributedCache must not be used";

        private static readonly LocalizableString MessageFormat =
            "'{0}' references IDistributedCache; use ICacheManager (TCCoder.RedisCacheManager) instead, per GlobalCachingStandards.md caching.3";

        private static readonly LocalizableString Description =
            "ICacheManager is the only approved cache access for all applications. It owns the Redis " +
            "connection, key building, serialization, retry and telemetry. IDistributedCache must not be " +
            "used directly. See GlobalCachingStandards.md Section 3.";

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
                    // Microsoft.Extensions.Caching.Abstractions is not referenced by this compilation; nothing to flag.
                    return;
                }

                compilationContext.RegisterSyntaxNodeAction(
                    syntaxContext => AnalyzeSimpleName(syntaxContext, distributedCacheSymbol),
                    SyntaxKind.IdentifierName,
                    SyntaxKind.GenericName);
            });
        }

        private static void AnalyzeSimpleName(SyntaxNodeAnalysisContext context, INamedTypeSymbol distributedCacheSymbol)
        {
            var nameSyntax = (SimpleNameSyntax)context.Node;

            if (!string.Equals(nameSyntax.Identifier.ValueText, "IDistributedCache", StringComparison.Ordinal))
            {
                return;
            }

            ISymbol? symbol = context.SemanticModel.GetSymbolInfo(nameSyntax, context.CancellationToken).Symbol;

            if (symbol is not ITypeSymbol typeSymbol ||
                !SymbolEqualityComparer.Default.Equals(typeSymbol, distributedCacheSymbol))
            {
                return;
            }

            string containingMemberName = GetContainingMemberName(nameSyntax);

            context.ReportDiagnostic(Diagnostic.Create(Rule, nameSyntax.GetLocation(), containingMemberName));
        }

        private static string GetContainingMemberName(SyntaxNode node)
        {
            SyntaxNode? current = node;

            while (current is not null)
            {
                switch (current)
                {
                    case ParameterSyntax parameter:
                        return parameter.Identifier.ValueText;
                    case VariableDeclaratorSyntax variable:
                        return variable.Identifier.ValueText;
                    case PropertyDeclarationSyntax property:
                        return property.Identifier.ValueText;
                    case FieldDeclarationSyntax field when field.Declaration.Variables.Count > 0:
                        return field.Declaration.Variables[0].Identifier.ValueText;
                    case MethodDeclarationSyntax method:
                        return method.Identifier.ValueText;
                }

                current = current.Parent;
            }

            return node.ToString();
        }
    }
}
