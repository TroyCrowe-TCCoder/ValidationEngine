using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Coding
{
    /// <summary>
    /// Enforces GlobalCodingStandards.md coding.2.6: authorization policies must be defined in
    /// dedicated class files separate from Program.cs and controller classes. Flags
    /// AddAuthorization policy-builder lambdas containing a RequireAssertion/RequireClaim/policy
    /// configuration body defined inline in Program.cs or a controller file.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class InlineAuthorizationPolicyAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CODE004";

        private const string ControllerSuffix = "Controller";

        private static readonly LocalizableString Title =
            "Authorization policies must not be defined inline";

        private static readonly LocalizableString MessageFormat =
            "AddAuthorization policy builder is defined inline in '{0}'; move policy definitions to a dedicated policy-provider class, per GlobalCodingStandards.md coding.2.6";

        private static readonly LocalizableString Description =
            "Authorization policies must be defined in dedicated class files separate from Program.cs and from " +
            "controller classes. Named policies must be registered at the application entry point, but the " +
            "policy body itself must not be defined inline. See GlobalCodingStandards.md Section 2.6.";

        private const string Category = "Coding";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
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
                compilationContext.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
            });
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            if (!CodingWellKnownTypes.IsInvocationOfMember(invocation, CodingWellKnownTypes.AddAuthorizationMethodName))
            {
                return;
            }

            bool hasLambdaArgument = false;

            foreach (ArgumentSyntax argument in invocation.ArgumentList.Arguments)
            {
                if (argument.Expression is LambdaExpressionSyntax or AnonymousMethodExpressionSyntax)
                {
                    hasLambdaArgument = true;
                    break;
                }
            }

            if (!hasLambdaArgument)
            {
                return;
            }

            string fileName = System.IO.Path.GetFileName(invocation.SyntaxTree.FilePath);
            bool isProgramFile = CodingWellKnownTypes.IsProgramFile(fileName);

            TypeDeclarationSyntax? containingType = CodingWellKnownTypes.GetContainingType(invocation);
            bool isControllerFile = containingType is not null &&
                containingType.Identifier.ValueText.EndsWith(ControllerSuffix, System.StringComparison.Ordinal);

            if (!isProgramFile && !isControllerFile)
            {
                return;
            }

            string location = isProgramFile ? fileName : containingType!.Identifier.ValueText;

            var diagnostic = Diagnostic.Create(Rule, invocation.GetLocation(), location);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
