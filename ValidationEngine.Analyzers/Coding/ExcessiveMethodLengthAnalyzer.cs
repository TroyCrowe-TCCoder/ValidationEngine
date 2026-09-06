using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Coding
{
    /// <summary>
    /// Enforces GlobalCodingStandards.md coding.3.3: methods must be short with one clear purpose.
    /// A method that requires scrolling to read in full must be split. Flags method bodies
    /// exceeding a configurable line-count threshold.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ExcessiveMethodLengthAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CODE007";

        private static readonly LocalizableString Title =
            "Methods must be short with one clear purpose";

        private static readonly LocalizableString MessageFormat =
            "Method '{0}' is {1} lines long, exceeding the {2}-line threshold; split it into smaller, single-purpose methods, per GlobalCodingStandards.md coding.3.3";

        private static readonly LocalizableString Description =
            "Methods must be short with one clear purpose. A method that requires scrolling to read in full " +
            "must be split. See GlobalCodingStandards.md Section 3.3.";

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
                compilationContext.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
            });
        }

        private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            var method = (MethodDeclarationSyntax)context.Node;

            SyntaxNode? body = (SyntaxNode?)method.Body ?? method.ExpressionBody;

            if (body is null)
            {
                return;
            }

            int lineCount = CodingWellKnownTypes.GetLineCount(body);

            if (lineCount <= CodingWellKnownTypes.MaxMethodLineCount)
            {
                return;
            }

            var diagnostic = Diagnostic.Create(
                Rule,
                method.Identifier.GetLocation(),
                method.Identifier.ValueText,
                lineCount,
                CodingWellKnownTypes.MaxMethodLineCount);

            context.ReportDiagnostic(diagnostic);
        }
    }
}
