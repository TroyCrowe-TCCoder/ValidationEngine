using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Coding
{
    /// <summary>
    /// Enforces GlobalCodingStandards.md coding.3.6: object mapping must be performed exclusively
    /// in the constructor of the target type. Flags object-creation expressions with an object
    /// initializer that map two or more properties from a single source parameter's members when
    /// the mapping occurs outside the target type's own constructor.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MappingOutsideConstructorAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CODE010";

        private const int MinMappedPropertiesToFlag = 2;

        private static readonly LocalizableString Title =
            "Object mapping must occur only in the target type's constructor";

        private static readonly LocalizableString MessageFormat =
            "'{0}' maps {1} properties from '{2}' using an object initializer outside a constructor; move this mapping into '{0}''s own constructor, per GlobalCodingStandards.md coding.3.6";

        private static readonly LocalizableString Description =
            "Object mapping must be performed exclusively in the constructor of the target type. The " +
            "constructor of the target type must accept the source object as a parameter and map its own " +
            "properties. See GlobalCodingStandards.md Section 3.6.";

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
                compilationContext.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
            });
        }

        private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
        {
            var objectCreation = (ObjectCreationExpressionSyntax)context.Node;

            if (objectCreation.Initializer is null ||
                !objectCreation.Initializer.IsKind(SyntaxKind.ObjectInitializerExpression))
            {
                return;
            }

            if (IsWithinConstructor(objectCreation))
            {
                return;
            }

            string? sourceIdentifier = null;
            int mappedCount = 0;

            foreach (ExpressionSyntax expression in objectCreation.Initializer.Expressions)
            {
                if (expression is not AssignmentExpressionSyntax
                    {
                        Left: IdentifierNameSyntax,
                        Right: MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax sourceExpression },
                    })
                {
                    continue;
                }

                string candidate = sourceExpression.Identifier.ValueText;

                if (sourceIdentifier is null)
                {
                    sourceIdentifier = candidate;
                    mappedCount = 1;
                }
                else if (string.Equals(sourceIdentifier, candidate, System.StringComparison.Ordinal))
                {
                    mappedCount++;
                }
            }

            if (sourceIdentifier is null || mappedCount < MinMappedPropertiesToFlag)
            {
                return;
            }

            string targetTypeName = objectCreation.Type.ToString();

            var diagnostic = Diagnostic.Create(
                Rule,
                objectCreation.GetLocation(),
                targetTypeName,
                mappedCount,
                sourceIdentifier);

            context.ReportDiagnostic(diagnostic);
        }

        private static bool IsWithinConstructor(SyntaxNode node)
        {
            SyntaxNode? current = node;

            while (current is not null)
            {
                if (current is ConstructorDeclarationSyntax)
                {
                    return true;
                }

                if (current is MethodDeclarationSyntax || current is AccessorDeclarationSyntax || current is LocalFunctionStatementSyntax)
                {
                    return false;
                }

                current = current.Parent;
            }

            return false;
        }
    }
}
