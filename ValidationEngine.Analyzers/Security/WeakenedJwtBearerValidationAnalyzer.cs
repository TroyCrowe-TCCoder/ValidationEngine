using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Security
{
    /// <summary>
    /// Enforces GlobalSecurityStandards.md security.3.5: JWT Bearer configuration must not omit or
    /// weaken any of the four required validation parameters: 'RequireHttpsMetadata = true',
    /// 'MapInboundClaims = false', 'ValidateIssuer = true', and 'ValidateAudience = true'. Flags
    /// assignments inside an 'AddJwtBearer' configuration lambda that set any of these properties
    /// to the wrong literal value.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class WeakenedJwtBearerValidationAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SEC003";

        private const string AddJwtBearerMethodName = "AddJwtBearer";

        private static readonly LocalizableString Title =
            "JWT Bearer validation parameters must not be omitted or weakened";

        private static readonly LocalizableString MessageFormat =
            "'{0}' is set to '{1}'; it must be '{2}', per GlobalSecurityStandards.md security.3.5";

        private static readonly LocalizableString Description =
            "Every API must validate the JWT access token on every request using AddJwtBearer middleware " +
            "configured with all four required parameters: ValidateIssuer = true, ValidateAudience = true, " +
            "MapInboundClaims = false, and RequireHttpsMetadata = true. None of these may be omitted or " +
            "weakened. See GlobalSecurityStandards.md Section 3.5.";

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
                compilationContext.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
            });
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            if (!SecurityWellKnownTypes.IsInvocationOfMember(invocation, AddJwtBearerMethodName))
            {
                return;
            }

            foreach (AssignmentExpressionSyntax assignment in invocation.DescendantNodes().OfType<AssignmentExpressionSyntax>())
            {
                if (assignment.Left is not IdentifierNameSyntax and not MemberAccessExpressionSyntax)
                {
                    continue;
                }

                string propertyName = assignment.Left switch
                {
                    IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                    MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
                    _ => string.Empty,
                };

                bool? requiredValue = propertyName switch
                {
                    SecurityWellKnownTypes.ValidateIssuerPropertyName => true,
                    SecurityWellKnownTypes.ValidateAudiencePropertyName => true,
                    SecurityWellKnownTypes.RequireHttpsMetadataPropertyName => true,
                    SecurityWellKnownTypes.MapInboundClaimsPropertyName => false,
                    _ => (bool?)null,
                };

                if (requiredValue is null)
                {
                    continue;
                }

                if (!TryGetBooleanLiteral(assignment.Right, out bool actualValue) || actualValue == requiredValue)
                {
                    continue;
                }

                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    assignment.GetLocation(),
                    propertyName,
                    actualValue ? "true" : "false",
                    requiredValue.Value ? "true" : "false"));
            }
        }

        private static bool TryGetBooleanLiteral(ExpressionSyntax expression, out bool value)
        {
            if (expression is LiteralExpressionSyntax literal)
            {
                if (literal.IsKind(SyntaxKind.TrueLiteralExpression))
                {
                    value = true;
                    return true;
                }

                if (literal.IsKind(SyntaxKind.FalseLiteralExpression))
                {
                    value = false;
                    return true;
                }
            }

            value = default;
            return false;
        }
    }
}
