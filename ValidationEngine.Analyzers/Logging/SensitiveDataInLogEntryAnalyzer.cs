using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Logging
{
    /// <summary>
    /// Enforces GlobalLoggingStandards.md logging.8: passwords, secrets, tokens, PII, and other
    /// sensitive data must never appear in log properties. This analyzer flags ILogger&lt;T&gt; calls
    /// whose message template contains a named placeholder that looks sensitive (e.g.
    /// <c>{Password}</c>), or whose argument is an identifier/member-access whose name looks
    /// sensitive (e.g. <c>user.Password</c>, <c>token</c>).
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class SensitiveDataInLogEntryAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "LOG015";

        private static readonly LocalizableString Title =
            "Log entries must not include sensitive data";

        private static readonly LocalizableString MessageFormat =
            "'{0}' logs a value named '{1}', which looks like sensitive data (password, token, secret, PII, etc.); remove it or project only approved safe fields, per GlobalLoggingStandards.md logging.8";

        private static readonly LocalizableString Description =
            "Passwords, secrets, tokens, PII, full credit card/account numbers, connection strings, and private " +
            "key material must never appear in log properties, telemetry, or health endpoint responses. See " +
            "GlobalLoggingStandards.md Section 8.";

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

            context.RegisterCompilationStartAction(compilationContext =>
            {
                INamedTypeSymbol? loggerGenericSymbol =
                    compilationContext.Compilation.GetTypeByMetadataName(LoggingWellKnownTypes.LoggerInterfaceMetadataName);
                INamedTypeSymbol? loggerSymbol =
                    compilationContext.Compilation.GetTypeByMetadataName(LoggingWellKnownTypes.LoggerNonGenericInterfaceMetadataName);

                if (loggerGenericSymbol is null && loggerSymbol is null)
                {
                    // Microsoft.Extensions.Logging.Abstractions is not referenced by this compilation.
                    return;
                }

                compilationContext.RegisterSyntaxNodeAction(
                    syntaxContext => AnalyzeInvocation(syntaxContext, loggerGenericSymbol, loggerSymbol),
                    SyntaxKind.InvocationExpression);
            });
        }

        private static void AnalyzeInvocation(
            SyntaxNodeAnalysisContext context,
            INamedTypeSymbol? loggerGenericSymbol,
            INamedTypeSymbol? loggerSymbol)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            {
                return;
            }

            if (!LoggingWellKnownTypes.IsLoggerMethodName(memberAccess.Name.Identifier.ValueText))
            {
                return;
            }

            ISymbol? symbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol;

            if (symbol is not IMethodSymbol methodSymbol || methodSymbol.ReceiverType is null)
            {
                return;
            }

            if (!LoggingWellKnownTypes.IsLoggerReceiverType(methodSymbol.ReceiverType, loggerGenericSymbol, loggerSymbol))
            {
                return;
            }

            string containingMethodName =
                LoggingWellKnownTypes.GetContainingMethod(invocation)?.Identifier.ValueText ?? "<unknown>";

            ExpressionSyntax? templateArgument = LoggingWellKnownTypes.GetMessageTemplateArgument(invocation);
            bool reportedFromTemplate = false;

            if (templateArgument is LiteralExpressionSyntax literal)
            {
                string template = literal.Token.ValueText;

                foreach (string name in ExtractPlaceholderNames(template))
                {
                    if (LoggingWellKnownTypes.ContainsSensitiveNameFragment(name))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Rule, literal.GetLocation(), containingMethodName, name));
                        reportedFromTemplate = true;
                    }
                }
            }

            if (reportedFromTemplate)
            {
                // The message template placeholders already identify the sensitive field(s) for this
                // call; avoid double-reporting the matching argument expressions below.
                return;
            }

            foreach (ArgumentSyntax argument in invocation.ArgumentList.Arguments)
            {
                if (ReferenceEquals(argument.Expression, templateArgument))
                {
                    continue;
                }

                string? candidateName = argument.Expression switch
                {
                    IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                    MemberAccessExpressionSyntax memberAccessArg => memberAccessArg.Name.Identifier.ValueText,
                    _ => null,
                };

                if (candidateName is not null && LoggingWellKnownTypes.ContainsSensitiveNameFragment(candidateName))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, argument.Expression.GetLocation(), containingMethodName, candidateName));
                }
            }
        }

        private static System.Collections.Generic.IEnumerable<string> ExtractPlaceholderNames(string template)
        {
            int index = 0;

            while (index < template.Length)
            {
                if (template[index] == '{')
                {
                    int closeIndex = template.IndexOf('}', index + 1);

                    if (closeIndex > index)
                    {
                        yield return template.Substring(index + 1, closeIndex - index - 1);
                        index = closeIndex + 1;
                        continue;
                    }
                }

                index++;
            }
        }
    }
}
