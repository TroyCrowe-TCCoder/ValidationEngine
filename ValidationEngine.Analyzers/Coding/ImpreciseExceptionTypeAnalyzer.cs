using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Coding
{
    /// <summary>
    /// Enforces GlobalCodingStandards.md coding.7.1: the most precise exception type available must
    /// be used. <c>Exception</c> and <c>ApplicationException</c> must not be thrown directly. Flags
    /// <c>throw new Exception(...)</c> and <c>throw new ApplicationException(...)</c>.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ImpreciseExceptionTypeAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CODE017";

        private static readonly LocalizableString Title =
            "The most precise exception type must be used";

        private static readonly LocalizableString MessageFormat =
            "'{0}' must not be thrown directly; throw the most precise exception type available for the failure condition, per GlobalCodingStandards.md coding.7.1";

        private static readonly LocalizableString Description =
            "The most precise exception type available must be used. Exception and ApplicationException must " +
            "not be thrown directly. See GlobalCodingStandards.md Section 7.1.";

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
                INamedTypeSymbol? exceptionSymbol =
                    compilationContext.Compilation.GetTypeByMetadataName(CodingWellKnownTypes.ExceptionMetadataName);
                INamedTypeSymbol? applicationExceptionSymbol =
                    compilationContext.Compilation.GetTypeByMetadataName(CodingWellKnownTypes.ApplicationExceptionMetadataName);

                if (exceptionSymbol is null && applicationExceptionSymbol is null)
                {
                    return;
                }

                compilationContext.RegisterSyntaxNodeAction(
                    syntaxContext => AnalyzeObjectCreation(syntaxContext, exceptionSymbol, applicationExceptionSymbol),
                    SyntaxKind.ObjectCreationExpression);
            });
        }

        private static void AnalyzeObjectCreation(
            SyntaxNodeAnalysisContext context,
            INamedTypeSymbol? exceptionSymbol,
            INamedTypeSymbol? applicationExceptionSymbol)
        {
            var objectCreation = (ObjectCreationExpressionSyntax)context.Node;

            if (objectCreation.Parent is not ThrowStatementSyntax && objectCreation.Parent is not ThrowExpressionSyntax)
            {
                return;
            }

            if (context.SemanticModel.GetSymbolInfo(objectCreation, context.CancellationToken).Symbol?.ContainingType
                is not INamedTypeSymbol createdType)
            {
                return;
            }

            bool isExactException = exceptionSymbol is not null &&
                SymbolEqualityComparer.Default.Equals(createdType, exceptionSymbol);
            bool isExactApplicationException = applicationExceptionSymbol is not null &&
                SymbolEqualityComparer.Default.Equals(createdType, applicationExceptionSymbol);

            if (!isExactException && !isExactApplicationException)
            {
                return;
            }

            var diagnostic = Diagnostic.Create(Rule, objectCreation.GetLocation(), createdType.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
