using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Logging
{
    /// <summary>
    /// Enforces GlobalLoggingStandards.md logging.5.2: every audit log entry must record Who, What,
    /// When, Outcome, CorrelationId, EntityType, and EntityId. This analyzer inspects calls to a
    /// dedicated audit-logging Record method that use named arguments and flags any missing
    /// required field.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class AuditEntryMissingRequiredFieldsAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "LOG012";

        private static readonly LocalizableString Title =
            "Audit log entries must record all required fields";

        private static readonly LocalizableString MessageFormat =
            "'{0}' calls the audit logger without the required '{1}' field(s); every audit entry must record Who, What, When, Outcome, CorrelationId, EntityType, and EntityId, per GlobalLoggingStandards.md logging.5.2";

        private static readonly LocalizableString Description =
            "Every audit entry must record Who, What, When, Outcome, CorrelationId, EntityType, and EntityId. " +
            "Without these fields the audit trail cannot distinguish a successful action from a denied or " +
            "failed one. See GlobalLoggingStandards.md Section 5.2.";

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

            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            {
                return;
            }

            if (!string.Equals(memberAccess.Name.Identifier.ValueText, LoggingWellKnownTypes.AuditRecordMethodName, System.StringComparison.Ordinal))
            {
                return;
            }

            string receiverText = memberAccess.Expression.ToString();

            if (!LoggingWellKnownTypes.LooksLikeAuditLogger(receiverText))
            {
                return;
            }

            var namedArgumentNames = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            foreach (ArgumentSyntax argument in invocation.ArgumentList.Arguments)
            {
                if (argument.NameColon is not null)
                {
                    namedArgumentNames.Add(argument.NameColon.Name.Identifier.ValueText);
                }
            }

            if (namedArgumentNames.Count == 0)
            {
                // All-positional call; field names cannot be verified statically.
                return;
            }

            List<string> missingFields = LoggingWellKnownTypes.RequiredAuditFieldNames
                .Where(required => !namedArgumentNames.Contains(required))
                .ToList();

            if (missingFields.Count == 0)
            {
                return;
            }

            string containingMethodName =
                LoggingWellKnownTypes.GetContainingMethod(invocation)?.Identifier.ValueText ?? "<unknown>";

            var diagnostic = Diagnostic.Create(
                Rule,
                memberAccess.Name.GetLocation(),
                containingMethodName,
                string.Join(", ", missingFields));
            context.ReportDiagnostic(diagnostic);
        }
    }
}
