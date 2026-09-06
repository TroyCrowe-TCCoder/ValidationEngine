using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Coding
{
    /// <summary>
    /// Enforces GlobalCodingStandards.md coding.2.2: layers must not reach across more than one
    /// boundary. Controller classes must delegate to services and must not reference repository
    /// types directly (fields, properties, or constructor parameters).
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ControllerDirectRepositoryAccessAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CODE002";

        private const string ControllerSuffix = "Controller";
        private const string RepositorySuffix = "Repository";

        private static readonly LocalizableString Title =
            "Controllers must not reference repositories directly";

        private static readonly LocalizableString MessageFormat =
            "Controller '{0}' references repository type '{1}' directly; controllers must delegate to a service, which delegates to repositories, per GlobalCodingStandards.md coding.2.2";

        private static readonly LocalizableString Description =
            "Concerns must be separated across layers, and no layer may reach across more than one boundary. " +
            "Controllers must not contain business logic or reference repositories directly; they must delegate " +
            "to services, which in turn delegate to repositories. See GlobalCodingStandards.md Section 2.2.";

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
                compilationContext.RegisterSyntaxNodeAction(AnalyzeField, SyntaxKind.FieldDeclaration);
                compilationContext.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
                compilationContext.RegisterSyntaxNodeAction(AnalyzeConstructor, SyntaxKind.ConstructorDeclaration);
            });
        }

        private static void AnalyzeField(SyntaxNodeAnalysisContext context)
        {
            var field = (FieldDeclarationSyntax)context.Node;
            TypeDeclarationSyntax? containingType = CodingWellKnownTypes.GetContainingType(field);

            if (!IsControllerType(containingType))
            {
                return;
            }

            AnalyzeTypeSyntax(context, field.Declaration.Type, containingType!.Identifier.ValueText);
        }

        private static void AnalyzeProperty(SyntaxNodeAnalysisContext context)
        {
            var property = (PropertyDeclarationSyntax)context.Node;
            TypeDeclarationSyntax? containingType = CodingWellKnownTypes.GetContainingType(property);

            if (!IsControllerType(containingType))
            {
                return;
            }

            AnalyzeTypeSyntax(context, property.Type, containingType!.Identifier.ValueText);
        }

        private static void AnalyzeConstructor(SyntaxNodeAnalysisContext context)
        {
            var constructor = (ConstructorDeclarationSyntax)context.Node;
            TypeDeclarationSyntax? containingType = CodingWellKnownTypes.GetContainingType(constructor);

            if (!IsControllerType(containingType))
            {
                return;
            }

            foreach (ParameterSyntax parameter in constructor.ParameterList.Parameters)
            {
                if (parameter.Type is null)
                {
                    continue;
                }

                AnalyzeTypeSyntax(context, parameter.Type, containingType!.Identifier.ValueText);
            }
        }

        private static bool IsControllerType(TypeDeclarationSyntax? containingType)
        {
            return containingType is not null &&
                   containingType.Identifier.ValueText.EndsWith(ControllerSuffix, System.StringComparison.Ordinal);
        }

        private static void AnalyzeTypeSyntax(SyntaxNodeAnalysisContext context, TypeSyntax typeSyntax, string controllerName)
        {
            string? typeName = typeSyntax switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                GenericNameSyntax generic => generic.Identifier.ValueText,
                QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
                _ => null,
            };

            if (typeName is null || !typeName.EndsWith(RepositorySuffix, System.StringComparison.Ordinal))
            {
                return;
            }

            var diagnostic = Diagnostic.Create(Rule, typeSyntax.GetLocation(), controllerName, typeName);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
