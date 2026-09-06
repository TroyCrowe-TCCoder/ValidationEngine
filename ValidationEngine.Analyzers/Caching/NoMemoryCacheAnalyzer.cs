using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Caching
{
    /// <summary>
    /// Enforces GlobalCachingStandards.md caching.3: Azure Cache for Redis (IDistributedCache) is the
    /// only approved cache provider. In-process memory cache (IMemoryCache) must not be used.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NoMemoryCacheAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CACHE001";

        private const string MemoryCacheInterfaceMetadataName = "Microsoft.Extensions.Caching.Memory.IMemoryCache";

        private static readonly LocalizableString Title =
            "IMemoryCache must not be used";

        private static readonly LocalizableString MessageFormat =
            "'{0}' references IMemoryCache; use IDistributedCache (Azure Cache for Redis) instead, per GlobalCachingStandards.md caching.3";

        private static readonly LocalizableString Description =
            "Azure Cache for Redis is the only approved cache provider for all applications. In-process " +
            "memory cache (IMemoryCache) must not be used. See GlobalCachingStandards.md Section 3.";

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
                INamedTypeSymbol? memoryCacheSymbol =
                    compilationContext.Compilation.GetTypeByMetadataName(MemoryCacheInterfaceMetadataName);

                if (memoryCacheSymbol is null)
                {
                    // Microsoft.Extensions.Caching.Memory is not referenced by this compilation; nothing to flag.
                    return;
                }

                compilationContext.RegisterSyntaxNodeAction(
                    syntaxContext => AnalyzeSimpleName(syntaxContext, memoryCacheSymbol),
                    SyntaxKind.IdentifierName,
                    SyntaxKind.GenericName);
            });
        }

        private static void AnalyzeSimpleName(SyntaxNodeAnalysisContext context, INamedTypeSymbol memoryCacheSymbol)
        {
            var nameSyntax = (SimpleNameSyntax)context.Node;

            // Only inspect the rightmost identifier of a qualified/member access to avoid duplicate reports
            // (e.g. "Memory" in "Microsoft.Extensions.Caching.Memory.IMemoryCache" would otherwise also match parts).
            if (!string.Equals(nameSyntax.Identifier.ValueText, "IMemoryCache", StringComparison.Ordinal))
            {
                return;
            }

            ISymbol? symbol = context.SemanticModel.GetSymbolInfo(nameSyntax, context.CancellationToken).Symbol;

            if (symbol is not ITypeSymbol typeSymbol ||
                !SymbolEqualityComparer.Default.Equals(typeSymbol, memoryCacheSymbol))
            {
                return;
            }

            string containingMemberName = GetContainingMemberName(nameSyntax);

            var diagnostic = Diagnostic.Create(Rule, nameSyntax.GetLocation(), containingMemberName);
            context.ReportDiagnostic(diagnostic);
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
