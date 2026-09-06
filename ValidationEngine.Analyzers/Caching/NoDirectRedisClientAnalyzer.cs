using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Caching
{
    /// <summary>
    /// Enforces GlobalCachingStandards.md caching.3.2: application services must inject
    /// IDistributedCache; direct use of the StackExchange.Redis client
    /// (IConnectionMultiplexer/ConnectionMultiplexer) inside application services is not permitted.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NoDirectRedisClientAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CACHE003";

        private static readonly LocalizableString Title =
            "Direct StackExchange.Redis client usage is not permitted in application services";

        private static readonly LocalizableString MessageFormat =
            "'{0}' references {1}; inject IDistributedCache instead of using the StackExchange.Redis client directly, per GlobalCachingStandards.md caching.3.2";

        private static readonly LocalizableString Description =
            "All services that read from or write to the cache must inject IDistributedCache. Direct use " +
            "of the StackExchange.Redis client inside application services is not permitted. See " +
            "GlobalCachingStandards.md Section 3.2.";

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
                INamedTypeSymbol? connectionMultiplexerSymbol = compilationContext.Compilation.GetTypeByMetadataName(
                    CachingWellKnownTypes.ConnectionMultiplexerMetadataName);
                INamedTypeSymbol? iConnectionMultiplexerSymbol = compilationContext.Compilation.GetTypeByMetadataName(
                    CachingWellKnownTypes.IConnectionMultiplexerMetadataName);

                if (connectionMultiplexerSymbol is null && iConnectionMultiplexerSymbol is null)
                {
                    // StackExchange.Redis is not referenced by this compilation; nothing to flag.
                    return;
                }

                compilationContext.RegisterSyntaxNodeAction(
                    syntaxContext => AnalyzeSimpleName(syntaxContext, connectionMultiplexerSymbol, iConnectionMultiplexerSymbol),
                    SyntaxKind.IdentifierName,
                    SyntaxKind.GenericName);
            });
        }

        private static void AnalyzeSimpleName(
            SyntaxNodeAnalysisContext context,
            INamedTypeSymbol? connectionMultiplexerSymbol,
            INamedTypeSymbol? iConnectionMultiplexerSymbol)
        {
            var nameSyntax = (SimpleNameSyntax)context.Node;
            string identifierText = nameSyntax.Identifier.ValueText;

            if (identifierText != "ConnectionMultiplexer" && identifierText != "IConnectionMultiplexer")
            {
                return;
            }

            ISymbol? symbol = context.SemanticModel.GetSymbolInfo(nameSyntax, context.CancellationToken).Symbol;

            bool matches =
                (symbol is ITypeSymbol typeSymbol &&
                 (SymbolEqualityComparer.Default.Equals(typeSymbol, connectionMultiplexerSymbol) ||
                  SymbolEqualityComparer.Default.Equals(typeSymbol, iConnectionMultiplexerSymbol))) ||
                (symbol is IMethodSymbol methodSymbol &&
                 SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, connectionMultiplexerSymbol));

            if (!matches)
            {
                return;
            }

            string containingMemberName = GetContainingMemberName(nameSyntax);

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                nameSyntax.GetLocation(),
                containingMemberName,
                identifierText));
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
