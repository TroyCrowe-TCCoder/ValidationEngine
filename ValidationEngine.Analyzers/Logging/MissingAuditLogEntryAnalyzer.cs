using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Logging
{
    /// <summary>
    /// Enforces GlobalLoggingStandards.md logging.5.1: methods that create, modify, or delete
    /// application data (identified by an auditable verb prefix such as Create/Update/Delete)
    /// must write an audit log entry through the dedicated audit-logging mechanism.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class MissingAuditLogEntryAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "LOG011";

        private static readonly LocalizableString Title =
            "State-changing operations must write an audit log entry";

        private static readonly LocalizableString MessageFormat =
            "'{0}' appears to create, modify, or delete data but never calls an audit-logging Record method; write an audit log entry per GlobalLoggingStandards.md logging.5.1";

        private static readonly LocalizableString Description =
            "Every operation that creates, modifies, or deletes application data, accesses sensitive data, or " +
            "changes authorization must write an audit log entry through the dedicated audit-logging mechanism " +
            "(e.g. IAuditLogger.Record), not through ILogger<T>. See GlobalLoggingStandards.md Section 5.1.";

        private const string Category = "Logging";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: "https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalLoggingStandards.md");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
        }

        private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            var method = (MethodDeclarationSyntax)context.Node;

            if (method.Body is null && method.ExpressionBody is null)
            {
                return;
            }

            if (!method.Modifiers.Any(SyntaxKind.PublicKeyword))
            {
                return;
            }

            string methodName = method.Identifier.ValueText;

            if (!LoggingWellKnownTypes.StartsWithAuditableVerb(methodName))
            {
                return;
            }

            SyntaxNode body = (SyntaxNode?)method.Body ?? method.ExpressionBody!;

            bool hasAuditRecordCall = body.DescendantNodesAndSelf()
                .OfType<InvocationExpressionSyntax>()
                .Any(IsAuditRecordInvocation);

            if (hasAuditRecordCall)
            {
                return;
            }

            var diagnostic = Diagnostic.Create(Rule, method.Identifier.GetLocation(), methodName);
            context.ReportDiagnostic(diagnostic);
        }

        private static bool IsAuditRecordInvocation(InvocationExpressionSyntax invocation)
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            {
                return false;
            }

            if (!string.Equals(memberAccess.Name.Identifier.ValueText, LoggingWellKnownTypes.AuditRecordMethodName, System.StringComparison.Ordinal))
            {
                return false;
            }

            string receiverText = memberAccess.Expression.ToString();
            return LoggingWellKnownTypes.LooksLikeAuditLogger(receiverText);
        }
    }
}
