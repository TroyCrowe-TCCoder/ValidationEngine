using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Coding
{
    /// <summary>
    /// Enforces GlobalCodingStandards.md coding.2.1: Program.cs must contain only DI registration
    /// and middleware pipeline wiring. Business logic, orchestration logic, loops, switches, and
    /// local functions must not accumulate in the composition root.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ProgramCompositionOnlyAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CODE001";

        private static readonly LocalizableString Title =
            "Program.cs must contain only composition-root wiring";

        private static readonly LocalizableString MessageFormat =
            "'{0}' introduces business/orchestration logic in Program.cs; keep Program.cs limited to DI and middleware wiring, per GlobalCodingStandards.md coding.2.1";

        private static readonly LocalizableString Description =
            "Application composition (DI registration and middleware pipeline configuration) must be kept at the " +
            "application entry point only. Loops, switch statements, and local functions in Program.cs indicate " +
            "business or orchestration logic that has accumulated in the composition root. See GlobalCodingStandards.md Section 2.1.";

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
                compilationContext.RegisterSyntaxNodeAction(
                    AnalyzeNode,
                    SyntaxKind.LocalFunctionStatement,
                    SyntaxKind.ForStatement,
                    SyntaxKind.ForEachStatement,
                    SyntaxKind.WhileStatement,
                    SyntaxKind.DoStatement,
                    SyntaxKind.SwitchStatement);
            });
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            string fileName = Path.GetFileName(context.Node.SyntaxTree.FilePath);

            if (!CodingWellKnownTypes.IsProgramFile(fileName))
            {
                return;
            }

            string constructName = context.Node switch
            {
                LocalFunctionStatementSyntax localFunction => $"local function '{localFunction.Identifier.ValueText}'",
                ForStatementSyntax => "for loop",
                ForEachStatementSyntax => "foreach loop",
                WhileStatementSyntax => "while loop",
                DoStatementSyntax => "do-while loop",
                SwitchStatementSyntax => "switch statement",
                _ => context.Node.Kind().ToString(),
            };

            var diagnostic = Diagnostic.Create(Rule, context.Node.GetLocation(), constructName);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
