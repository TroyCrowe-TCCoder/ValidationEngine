using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Coding
{
    /// <summary>
    /// Enforces GlobalCodingStandards.md coding.2.4: every source file must declare exactly one
    /// non-nested class/interface/record/struct/enum. Nested types consumed only by their
    /// enclosing type are exempt, as are additional partial declarations of the same type.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class OneTypePerFileAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CODE024";

        private static readonly LocalizableString Title =
            "File must declare exactly one type";

        private static readonly LocalizableString MessageFormat =
            "File declares {0} top-level types ('{1}'); split into one file per type, per GlobalCodingStandards.md coding.2.4";

        private static readonly LocalizableString Description =
            "Every file must declare exactly one class/interface/record/struct/enum, except private/nested " +
            "types consumed only by their enclosing type. See GlobalCodingStandards.md Section 2.4.";

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

            context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
        }

        private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            SyntaxNode root = context.Tree.GetRoot(context.CancellationToken);

            var topLevelTypes = root.DescendantNodes(descendIntoChildren: node =>
                    node is CompilationUnitSyntax or NamespaceDeclarationSyntax or FileScopedNamespaceDeclarationSyntax)
                .OfType<BaseTypeDeclarationSyntax>()
                .Where(type => type.Parent is NamespaceDeclarationSyntax or FileScopedNamespaceDeclarationSyntax or CompilationUnitSyntax)
                .ToImmutableArray();

            var distinctTypeNames = topLevelTypes
                .Select(type => type.Identifier.ValueText)
                .Distinct()
                .ToImmutableArray();

            if (distinctTypeNames.Length <= 1)
            {
                return;
            }

            BaseTypeDeclarationSyntax firstType = topLevelTypes[0];
            string typeNames = string.Join("', '", distinctTypeNames);

            var diagnostic = Diagnostic.Create(
                Rule,
                firstType.Identifier.GetLocation(),
                distinctTypeNames.Length,
                typeNames);

            context.ReportDiagnostic(diagnostic);
        }
    }
}
