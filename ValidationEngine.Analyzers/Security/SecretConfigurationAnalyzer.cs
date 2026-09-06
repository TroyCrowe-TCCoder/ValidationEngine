using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Security
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class SecretConfigurationAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SEC011";

        private static readonly LocalizableString Title = "Secrets must be read through configuration and Key Vault SDKs must not be called in application code";
        private static readonly LocalizableString MessageFormat = "Avoid direct Key Vault SDK usage or inline/concatenated secret construction: {0}";
        private static readonly LocalizableString Description = "Detect direct usage of Key Vault SDKs (SecretClient/KeyVaultClient) outside Program.cs and flag concatenated or interpolated secret-shaped values assigned in code. See GlobalSecurityStandards.md security.2.3.";
        private const string Category = "Security";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description,
            customTags: new[] { "CompilationEnd" });

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
            context.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.SimpleAssignmentExpression);
            context.RegisterSyntaxNodeAction(AnalyzeVariableDeclarator, SyntaxKind.VariableDeclarator);
        }

        private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
        {
            var creation = (ObjectCreationExpressionSyntax)context.Node;
            var model = context.SemanticModel;

            if (SecurityWellKnownTypes.IsProgramFile(creation.SyntaxTree.FilePath))
                return;

            if (model.GetSymbolInfo(creation, context.CancellationToken).Symbol is not IMethodSymbol constructorSymbol)
                return;

            var containingType = constructorSymbol.ContainingType?.ToDisplayString() ?? string.Empty;

            if (containingType == SecurityWellKnownTypes.SecretClientMetadataName ||
                containingType == SecurityWellKnownTypes.KeyVaultClientMetadataName)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, creation.GetLocation(), constructorSymbol.ContainingType!.Name));
            }
        }

        private static void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
        {
            var assignment = (AssignmentExpressionSyntax)context.Node;

            string? targetName = assignment.Left switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
                _ => null,
            };

            if (targetName is null || !IsSecretShapedName(targetName))
                return;

            if (SecurityWellKnownTypes.IsConcatenatedOrInterpolatedString(assignment.Right))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, assignment.Right.GetLocation(), targetName));
            }
        }

        private static void AnalyzeVariableDeclarator(SyntaxNodeAnalysisContext context)
        {
            var declarator = (VariableDeclaratorSyntax)context.Node;

            if (declarator.Initializer is null)
                return;

            if (!IsSecretShapedName(declarator.Identifier.ValueText))
                return;

            if (SecurityWellKnownTypes.IsConcatenatedOrInterpolatedString(declarator.Initializer.Value))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, declarator.Initializer.Value.GetLocation(), declarator.Identifier.ValueText));
            }
        }

        private static bool IsSecretShapedName(string identifier)
        {
            return SecurityWellKnownTypes.IsSecretShapedName(identifier);
        }
    }
}
