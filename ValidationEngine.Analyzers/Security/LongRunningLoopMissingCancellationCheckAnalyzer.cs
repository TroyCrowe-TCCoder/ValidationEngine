using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Security
{
    /// <summary>
    /// Enforces GlobalSecurityStandards.md security.7.3: long-running operations must respect
    /// cancellation. Polling loops must check 'cancellationToken.IsCancellationRequested' at the
    /// top of every iteration, or the loop body must call 'ThrowIfCancellationRequested()'. Flags
    /// 'while'/'for' loops whose body contains 'await Task.Delay(...)' but never checks
    /// cancellation anywhere in the loop body.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class LongRunningLoopMissingCancellationCheckAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SEC008";

        private static readonly LocalizableString Title =
            "Long-running polling loops must check for cancellation";

        private static readonly LocalizableString MessageFormat =
            "Loop contains 'await Task.Delay(...)' but never checks 'IsCancellationRequested' or calls 'ThrowIfCancellationRequested()', per GlobalSecurityStandards.md security.7.3";

        private static readonly LocalizableString Description =
            "Any operation that may run for more than a few seconds -- batch processing, billing runs, " +
            "bulk imports, external HTTP calls -- must respect cancellation and exit cleanly when the " +
            "token is signalled. Polling loops must check cancellationToken.IsCancellationRequested at " +
            "the top of every iteration, or call ThrowIfCancellationRequested() explicitly. See " +
            "GlobalSecurityStandards.md Section 7.3.";

        private const string Category = "Security";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
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
                compilationContext.RegisterSyntaxNodeAction(
                    AnalyzeLoop,
                    SyntaxKind.WhileStatement,
                    SyntaxKind.ForStatement,
                    SyntaxKind.ForEachStatement,
                    SyntaxKind.DoStatement);
            });
        }

        private static void AnalyzeLoop(SyntaxNodeAnalysisContext context)
        {
            var loop = (StatementSyntax)context.Node;

            SyntaxNode? loopBody = loop switch
            {
                WhileStatementSyntax whileStatement => whileStatement.Statement,
                ForStatementSyntax forStatement => forStatement.Statement,
                ForEachStatementSyntax forEachStatement => forEachStatement.Statement,
                DoStatementSyntax doStatement => doStatement.Statement,
                _ => null,
            };

            if (loopBody is null)
            {
                return;
            }

            bool hasTaskDelay = loopBody.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Any(invocation => SecurityWellKnownTypes.IsInvocationOfMember(invocation, SecurityWellKnownTypes.TaskDelayMethodName)
                    && IsTaskDelayInvocation(invocation));

            if (!hasTaskDelay)
            {
                return;
            }

            // The cancellation check may appear in the loop condition (e.g. 'while
            // (!cancellationToken.IsCancellationRequested)') or anywhere in the loop body (e.g. an
            // 'if (cancellationToken.IsCancellationRequested) break;' guard, or an explicit
            // 'ThrowIfCancellationRequested()' call), so the whole loop node is searched rather
            // than only its body.
            bool hasCancellationCheck = loop.DescendantNodes()
                .OfType<MemberAccessExpressionSyntax>()
                .Any(memberAccess => string.Equals(
                    memberAccess.Name.Identifier.ValueText,
                    SecurityWellKnownTypes.IsCancellationRequestedPropertyName,
                    System.StringComparison.Ordinal));

            hasCancellationCheck |= loop.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Any(invocation => SecurityWellKnownTypes.IsInvocationOfMember(invocation, SecurityWellKnownTypes.ThrowIfCancellationRequestedMethodName));

            if (hasCancellationCheck)
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, loop.GetLocation()));
        }

        private static bool IsTaskDelayInvocation(InvocationExpressionSyntax invocation)
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            {
                return false;
            }

            return memberAccess.Expression switch
            {
                IdentifierNameSyntax identifier => string.Equals(identifier.Identifier.ValueText, SecurityWellKnownTypes.TaskTypeName, System.StringComparison.Ordinal),
                _ => false,
            };
        }
    }
}
