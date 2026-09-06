using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Coding
{
    /// <summary>
    /// Enforces GlobalCodingStandards.md coding.3.2: classes, methods, parameters, and variables
    /// must use descriptive names that communicate intent without requiring a comment to explain
    /// them. Abstract or abbreviated names are not permitted. Conventional short loop counters and
    /// lambda parameters are exempt.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NonDescriptiveNamingAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CODE006";

        private static readonly LocalizableString Title =
            "Names must be descriptive; abbreviations are not permitted";

        private static readonly LocalizableString MessageFormat =
            "'{0}' '{1}' is not descriptive; use a name that communicates intent without an abbreviation, per GlobalCodingStandards.md coding.3.2";

        private static readonly LocalizableString Description =
            "All classes, methods, parameters, and variables must use descriptive names that communicate intent " +
            "without requiring a comment to explain them. Abstract or abbreviated names are not permitted. " +
            "See GlobalCodingStandards.md Section 3.2.";

        private const string Category = "Coding";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
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
                compilationContext.RegisterSyntaxNodeAction(AnalyzeParameter, SyntaxKind.Parameter);
                compilationContext.RegisterSyntaxNodeAction(AnalyzeVariableDeclarator, SyntaxKind.VariableDeclarator);
                compilationContext.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
            });
        }

        private static void AnalyzeParameter(SyntaxNodeAnalysisContext context)
        {
            var parameter = (ParameterSyntax)context.Node;

            if (parameter.Parent?.Parent is SimpleLambdaExpressionSyntax or ParenthesizedLambdaExpressionSyntax)
            {
                // Lambda parameters are conventionally short (x, y, item) and are exempt.
                return;
            }

            string name = parameter.Identifier.ValueText;

            if (string.IsNullOrEmpty(name) || !CodingWellKnownTypes.IsNonDescriptiveIdentifier(name))
            {
                return;
            }

            ReportDiagnostic(context, parameter.Identifier.GetLocation(), "Parameter", name);
        }

        private static void AnalyzeVariableDeclarator(SyntaxNodeAnalysisContext context)
        {
            var declarator = (VariableDeclaratorSyntax)context.Node;

            if (declarator.Parent?.Parent is FieldDeclarationSyntax)
            {
                // Fields are covered by naming-convention analyzers/style rules elsewhere.
                return;
            }

            if (CodingWellKnownTypes.FindEnclosingLoopHeader(declarator) is not null)
            {
                // Loop-counter declarations (for (int i = 0; ...)) are conventionally short.
                return;
            }

            string name = declarator.Identifier.ValueText;

            if (string.IsNullOrEmpty(name) || !CodingWellKnownTypes.IsNonDescriptiveIdentifier(name))
            {
                return;
            }

            ReportDiagnostic(context, declarator.Identifier.GetLocation(), "Variable", name);
        }

        private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            var method = (MethodDeclarationSyntax)context.Node;
            string name = method.Identifier.ValueText;

            if (string.IsNullOrEmpty(name) || !CodingWellKnownTypes.IsNonDescriptiveIdentifier(name))
            {
                return;
            }

            ReportDiagnostic(context, method.Identifier.GetLocation(), "Method", name);
        }

        private static void ReportDiagnostic(SyntaxNodeAnalysisContext context, Location location, string kind, string name)
        {
            var diagnostic = Diagnostic.Create(Rule, location, kind, name);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
