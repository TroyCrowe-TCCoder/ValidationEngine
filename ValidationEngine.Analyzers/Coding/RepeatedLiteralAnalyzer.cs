using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Coding
{
    /// <summary>
    /// Enforces GlobalCodingStandards.md coding.3.14: string literals and numeric values that
    /// appear in more than one place must be defined as named constants. Flags identical string or
    /// numeric literals that appear more than once within the same type declaration.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class RepeatedLiteralAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CODE013";

        private const int MaxAllowedOccurrences = 1;

        private static readonly LocalizableString Title =
            "Repeated string and numeric literals must be named constants";

        private static readonly LocalizableString MessageFormat =
            "Literal {0} appears {1} times in '{2}'; extract it into a named constant in a dedicated Constants class, per GlobalCodingStandards.md coding.3.14";

        private static readonly LocalizableString Description =
            "All string literals and numeric values that appear in more than one place, are used as " +
            "identifiers, or represent a named concept must be defined as named constants. Constants must be " +
            "defined in a dedicated Constants/ folder within the project. See GlobalCodingStandards.md Section 3.14.";

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
                compilationContext.RegisterSyntaxNodeAction(AnalyzeType, SyntaxKind.ClassDeclaration);
                compilationContext.RegisterSyntaxNodeAction(AnalyzeType, SyntaxKind.RecordDeclaration);
                compilationContext.RegisterSyntaxNodeAction(AnalyzeType, SyntaxKind.StructDeclaration);
            });
        }

        private static void AnalyzeType(SyntaxNodeAnalysisContext context)
        {
            var typeDeclaration = (TypeDeclarationSyntax)context.Node;

            if (IsNestedInAnotherType(typeDeclaration))
            {
                return;
            }

            var occurrences = new Dictionary<string, List<LiteralExpressionSyntax>>(System.StringComparer.Ordinal);

            CollectLiterals(typeDeclaration, occurrences);

            foreach (KeyValuePair<string, List<LiteralExpressionSyntax>> entry in occurrences)
            {
                if (entry.Value.Count <= MaxAllowedOccurrences)
                {
                    continue;
                }

                foreach (LiteralExpressionSyntax literal in entry.Value)
                {
                    var diagnostic = Diagnostic.Create(
                        Rule,
                        literal.GetLocation(),
                        entry.Key,
                        entry.Value.Count,
                        typeDeclaration.Identifier.ValueText);

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }

        private static bool IsNestedInAnotherType(TypeDeclarationSyntax typeDeclaration)
        {
            return typeDeclaration.Parent is TypeDeclarationSyntax;
        }

        private static void CollectLiterals(SyntaxNode node, Dictionary<string, List<LiteralExpressionSyntax>> occurrences)
        {
            foreach (SyntaxNode child in node.ChildNodes())
            {
                if (child is TypeDeclarationSyntax)
                {
                    continue;
                }

                if (child is LiteralExpressionSyntax literal && IsEligibleLiteral(literal))
                {
                    string key = literal.Token.Text;

                    if (!occurrences.TryGetValue(key, out List<LiteralExpressionSyntax>? matches))
                    {
                        matches = new List<LiteralExpressionSyntax>();
                        occurrences[key] = matches;
                    }

                    matches.Add(literal);
                }

                CollectLiterals(child, occurrences);
            }
        }

        private static bool IsEligibleLiteral(LiteralExpressionSyntax literal)
        {
            if (literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                return literal.Token.ValueText.Length > 0;
            }

            if (literal.IsKind(SyntaxKind.NumericLiteralExpression))
            {
                return literal.Token.ValueText != "0" && literal.Token.ValueText != "1";
            }

            return false;
        }
    }
}
