using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Security
{
    /// <summary>
    /// Enforces GlobalSecurityStandards.md security.2.3: secrets must be accessed through the
    /// standard .NET configuration pipeline. Services must never call Key Vault SDKs directly, and
    /// connection strings/secrets must never be constructed inline or concatenated from parts.
    /// Flags 'SecretClient'/'KeyVaultClient' usage outside 'Program.cs', and flags
    /// concatenated/interpolated string values assigned to identifiers whose name suggests a
    /// connection string or secret.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DirectKeyVaultAccessAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SEC001";

        private static readonly ImmutableArray<string> SecretShapedNameFragments = ImmutableArray.Create(
            "connectionstring",
            "connstring",
            "secret",
            "password",
            "apikey");

        private static readonly LocalizableString Title =
            "Secrets must be accessed through configuration, not the Key Vault SDK or string concatenation";

        private static readonly LocalizableString MessageFormat =
            "'{0}' {1}, per GlobalSecurityStandards.md security.2.3";

        private static readonly LocalizableString Description =
            "Secrets must be accessed through the standard .NET configuration pipeline (IConfiguration or " +
            "strongly typed options). Services must never call Key Vault SDKs directly outside application " +
            "startup composition, and connection strings/secrets must never be constructed inline or " +
            "concatenated from parts. See GlobalSecurityStandards.md Section 2.3.";

        private const string Category = "Security";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: "https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalSecurityStandards.md");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(compilationContext =>
            {
                compilationContext.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
                compilationContext.RegisterSyntaxNodeAction(AnalyzeVariableDeclarator, SyntaxKind.VariableDeclarator);
                compilationContext.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.SimpleAssignmentExpression);
            });
        }

        private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
        {
            var creation = (ObjectCreationExpressionSyntax)context.Node;
            string filePath = creation.SyntaxTree.FilePath;

            if (SecurityWellKnownTypes.IsProgramFile(filePath))
            {
                return;
            }

            if (context.SemanticModel.GetSymbolInfo(creation, context.CancellationToken).Symbol is not IMethodSymbol constructorSymbol)
            {
                return;
            }

            string typeMetadataName = constructorSymbol.ContainingType?.ToDisplayString() ?? string.Empty;

            if (typeMetadataName != SecurityWellKnownTypes.SecretClientMetadataName &&
                typeMetadataName != SecurityWellKnownTypes.KeyVaultClientMetadataName)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                creation.GetLocation(),
                constructorSymbol.ContainingType!.Name,
                "must not be instantiated outside Program.cs; Key Vault must be connected as a configuration provider at the application entry point"));
        }

        private static void AnalyzeVariableDeclarator(SyntaxNodeAnalysisContext context)
        {
            var declarator = (VariableDeclaratorSyntax)context.Node;

            if (declarator.Initializer is null || !IsSecretShapedName(declarator.Identifier.ValueText))
            {
                return;
            }

            if (!SecurityWellKnownTypes.IsConcatenatedOrInterpolatedString(declarator.Initializer.Value))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                declarator.Initializer.Value.GetLocation(),
                declarator.Identifier.ValueText,
                "must not be constructed by string concatenation or interpolation; secrets must be read by name through IConfiguration or bound to a strongly typed options class"));
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
            {
                return;
            }

            if (!SecurityWellKnownTypes.IsConcatenatedOrInterpolatedString(assignment.Right))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                assignment.Right.GetLocation(),
                targetName,
                "must not be constructed by string concatenation or interpolation; secrets must be read by name through IConfiguration or bound to a strongly typed options class"));
        }

        private static bool IsSecretShapedName(string identifierName)
        {
            string normalized = identifierName.ToLowerInvariant();
            return SecretShapedNameFragments.Any(fragment => normalized.Contains(fragment));
        }
    }
}
