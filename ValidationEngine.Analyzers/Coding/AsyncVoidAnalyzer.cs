using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Coding
{
    /// <summary>
    /// Enforces GlobalCodingStandards.md coding.8.3: 'async void' methods must not be used except
    /// for event handlers where the framework requires it. Flags 'async void' methods that do not
    /// match a conventional event-handler signature, and event handlers using 'async void' whose
    /// body is not wrapped in a try/catch that logs all exceptions.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class AsyncVoidAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CODE021";

        private const string LogMethodNamePrefix = "Log";

        private static readonly LocalizableString Title =
            "'async void' must not be used except for required event handlers";

        private static readonly LocalizableString MessageFormat =
            "'{0}' must not use 'async void'; 'async void' is only permitted for event handlers that wrap their body in a try/catch which logs all exceptions, per GlobalCodingStandards.md coding.8.3";

        private static readonly LocalizableString Description =
            "'async void' methods must not be used except for event handlers where the framework requires " +
            "it. 'async void' prevents exception propagation and cannot be awaited. Event handlers that " +
            "must use 'async void' must wrap their body in a try/catch that logs all exceptions. See " +
            "GlobalCodingStandards.md Section 8.3.";

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
                compilationContext.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
            });
        }

        private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            var method = (MethodDeclarationSyntax)context.Node;

            if (method.ReturnType is not PredefinedTypeSyntax predefinedType ||
                !predefinedType.Keyword.IsKind(SyntaxKind.VoidKeyword))
            {
                return;
            }

            if (!method.Modifiers.Any(SyntaxKind.AsyncKeyword))
            {
                return;
            }

            if (!CodingWellKnownTypes.HasEventHandlerSignature(method))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rule, method.Identifier.GetLocation(), method.Identifier.ValueText));
                return;
            }

            if (method.Body is null || !HasLoggedTryCatch(method.Body))
            {
                context.ReportDiagnostic(
                    Diagnostic.Create(Rule, method.Identifier.GetLocation(), method.Identifier.ValueText));
            }
        }

        private static bool HasLoggedTryCatch(BlockSyntax body)
        {
            foreach (TryStatementSyntax tryStatement in body.Statements.OfType<TryStatementSyntax>())
            {
                foreach (CatchClauseSyntax catchClause in tryStatement.Catches)
                {
                    if (catchClause.Block is not null && HasLogInvocation(catchClause.Block))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool HasLogInvocation(BlockSyntax block)
        {
            foreach (InvocationExpressionSyntax invocation in block.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                string? methodName = invocation.Expression switch
                {
                    MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
                    IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                    _ => null,
                };

                if (methodName is not null && methodName.StartsWith(LogMethodNamePrefix, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
