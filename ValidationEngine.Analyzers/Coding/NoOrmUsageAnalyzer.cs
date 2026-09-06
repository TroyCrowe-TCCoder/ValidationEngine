using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Coding
{
    /// <summary>
    /// Enforces GlobalCodingStandards.md coding.2.7: all data access must be implemented using
    /// ADO.NET through the repository pattern, executed via stored procedures. Object-relational
    /// mapping frameworks (Entity Framework Core DbContext) must not be used. Flags any type that
    /// derives from Microsoft.EntityFrameworkCore.DbContext.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NoOrmUsageAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CODE005";

        private static readonly LocalizableString Title =
            "Object-relational mapping frameworks must not be used";

        private static readonly LocalizableString MessageFormat =
            "'{0}' derives from DbContext; data access must use ADO.NET through the repository pattern with stored procedures, not an ORM, per GlobalCodingStandards.md coding.2.7";

        private static readonly LocalizableString Description =
            "All data access must be implemented using ADO.NET through the repository pattern. Object-relational " +
            "mapping tools and frameworks must not be used. All database operations must be executed through " +
            "stored procedures. See GlobalCodingStandards.md Section 2.7.";

        private const string Category = "Coding";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: "https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalCodingStandards.md");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(compilationContext =>
            {
                INamedTypeSymbol? dbContextSymbol =
                    compilationContext.Compilation.GetTypeByMetadataName(CodingWellKnownTypes.DbContextMetadataName);

                if (dbContextSymbol is null)
                {
                    // Microsoft.EntityFrameworkCore is not referenced by this compilation.
                    return;
                }

                compilationContext.RegisterSyntaxNodeAction(
                    syntaxContext => AnalyzeTypeDeclaration(syntaxContext, dbContextSymbol),
                    SyntaxKind.ClassDeclaration);
            });
        }

        private static void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context, INamedTypeSymbol dbContextSymbol)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;

            if (context.SemanticModel.GetDeclaredSymbol(classDeclaration, context.CancellationToken)
                is not INamedTypeSymbol typeSymbol)
            {
                return;
            }

            INamedTypeSymbol? baseType = typeSymbol.BaseType;

            while (baseType is not null)
            {
                if (SymbolEqualityComparer.Default.Equals(baseType, dbContextSymbol))
                {
                    var diagnostic = Diagnostic.Create(Rule, classDeclaration.Identifier.GetLocation(), typeSymbol.Name);
                    context.ReportDiagnostic(diagnostic);
                    return;
                }

                baseType = baseType.BaseType;
            }
        }
    }
}
