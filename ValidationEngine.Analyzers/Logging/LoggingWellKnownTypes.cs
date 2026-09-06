using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ValidationEngine.Analyzers.Logging
{
    /// <summary>
    /// Shared metadata names, naming-convention constants, and small syntax-walking helpers reused
    /// across the Logging domain analyzers. Centralizing these avoids duplicating lookup/detection
    /// boilerplate in every individual GlobalLoggingStandards.md rule analyzer.
    /// </summary>
    internal static class LoggingWellKnownTypes
    {
        public const string LoggerInterfaceMetadataName = "Microsoft.Extensions.Logging.ILogger`1";
        public const string LoggerNonGenericInterfaceMetadataName = "Microsoft.Extensions.Logging.ILogger";
        public const string ConsoleMetadataName = "System.Console";
        public const string TraceMetadataName = "System.Diagnostics.Trace";
        public const string DebugMetadataName = "System.Diagnostics.Debug";
        public const string TelemetryClientMetadataName = "Microsoft.ApplicationInsights.TelemetryClient";
        public const string HttpContextMetadataName = "Microsoft.AspNetCore.Http.HttpContext";

        public const string ApplicationInsightsNamespacePrefix = "Microsoft.ApplicationInsights";
        public const string InfrastructureNamespaceFragment = "Infrastructure";
        public const string OptionsNamespaceFragment = "Options";

        public const string ApplicationInsightsRegistrationMethodName = "AddApplicationInsightsTelemetry";
        public const string ProgramFileName = "Program.cs";

        public const string CorrelationIdHeaderName = "X-Correlation-Id";
        public const string RequestIdHeaderName = "X-Request-Id";
        public const string TraceIdentifierPropertyName = "TraceIdentifier";
        public const string CorrelationIdScopeKey = "CorrelationId";
        public const string BeginScopeMethodName = "BeginScope";

        public const string HttpClientManagerClassNameFragment = "HttpClientManager";
        public const string HttpClientMetadataName = "System.Net.Http.HttpClient";
        public const string HttpRequestMessageMetadataName = "System.Net.Http.HttpRequestMessage";

        public const string AuditLoggerFieldNameFragment = "auditLogger";
        public const string AuditLoggerInterfaceNameFragment = "IAuditLogger";
        public const string AuditRecordMethodName = "Record";

        public const string MapHealthChecksMethodName = "MapHealthChecks";
        public const string AllowAnonymousMethodName = "AllowAnonymous";
        public const string HealthCheckEndpointRoute = "/healthcheck";

        public const int LargeBatchThreshold = 100;

        /// <summary>
        /// Argument-name fragments that indicate an argument carries the correlation identifier
        /// (GlobalLoggingStandards.md logging.4.x).
        /// </summary>
        public static readonly ImmutableArray<string> CorrelationIdNameFragments = ImmutableArray.Create(
            "correlationid",
            "correlationId");

        /// <summary>
        /// Audit-entry field names required by GlobalLoggingStandards.md logging.5.2: Who, What,
        /// When, Outcome, CorrelationId, EntityType, EntityId.
        /// </summary>
        public static readonly ImmutableArray<string> RequiredAuditFieldNames = ImmutableArray.Create(
            "who",
            "what",
            "when",
            "outcome",
            "correlationid",
            "entitytype",
            "entityid");

        /// <summary>
        /// TelemetryClient methods that bypass ILogger&lt;T&gt; and write telemetry directly
        /// (GlobalLoggingStandards.md logging.2.2).
        /// </summary>
        public static readonly ImmutableArray<string> TelemetryClientTrackMethodNames = ImmutableArray.Create(
            "TrackTrace",
            "TrackEvent",
            "TrackException",
            "TrackMetric",
            "TrackDependency",
            "TrackRequest",
            "TrackPageView");

        /// <summary>
        /// Naming fragments that indicate a value/argument is likely to hold sensitive data that must
        /// never be written to a log message (GlobalLoggingStandards.md logging.4.x).
        /// </summary>
        public static readonly ImmutableArray<string> SensitiveNameFragments = ImmutableArray.Create(
            "password",
            "passwd",
            "pwd",
            "token",
            "jwt",
            "apikey",
            "api_key",
            "secret",
            "credential",
            "ssn",
            "socialsecurity",
            "cardnumber",
            "creditcard",
            "cvv");

        /// <summary>
        /// Method-name prefixes that indicate a state-changing/auditable operation, used to detect
        /// methods that must emit an audit-worthy log entry (GlobalLoggingStandards.md logging.5.x).
        /// </summary>
        public static readonly ImmutableArray<string> AuditableVerbPrefixes = ImmutableArray.Create(
            "Create",
            "Update",
            "Delete",
            "Remove",
            "Approve",
            "Reject",
            "Submit",
            "Authorize");

        /// <summary>
        /// Method-name fragments that indicate an outbound dependency call (HTTP, database, queue),
        /// used by rules that require dependency calls to be logged with duration/outcome
        /// (GlobalLoggingStandards.md logging.6.x).
        /// </summary>
        public static readonly ImmutableArray<string> DependencyCallMethodNames = ImmutableArray.Create(
            "ExecuteAsync",
            "SendAsync",
            "QueryAsync",
            "ExecuteReaderAsync",
            "GetAsync",
            "PostAsync",
            "PutAsync",
            "DeleteAsync");

        public static bool ContainsSensitiveNameFragment(string name)
        {
            foreach (string fragment in SensitiveNameFragments)
            {
                if (name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsLoggerMethodName(string methodName)
        {
            switch (methodName)
            {
                case "LogTrace":
                case "LogDebug":
                case "LogInformation":
                case "LogWarning":
                case "LogError":
                case "LogCritical":
                case "Log":
                case "BeginScope":
                    return true;
                default:
                    return false;
            }
        }

        public static int GetSeverityRank(string logMethodName)
        {
            switch (logMethodName)
            {
                case "LogTrace":
                    return 0;
                case "LogDebug":
                    return 1;
                case "LogInformation":
                    return 2;
                case "LogWarning":
                    return 3;
                case "LogError":
                    return 4;
                case "LogCritical":
                    return 5;
                default:
                    return -1;
            }
        }

        /// <summary>
        /// Walks up the syntax tree from <paramref name="node"/> to find the nearest enclosing
        /// method declaration, or <c>null</c> if <paramref name="node"/> is not inside one.
        /// </summary>
        public static MethodDeclarationSyntax? GetContainingMethod(SyntaxNode node)
        {
            SyntaxNode? current = node;

            while (current is not null)
            {
                if (current is MethodDeclarationSyntax method)
                {
                    return method;
                }

                current = current.Parent;
            }

            return null;
        }

        /// <summary>
        /// Walks up the syntax tree from <paramref name="node"/> to find the nearest enclosing
        /// type declaration (class, struct, record, interface), or <c>null</c> if none is found.
        /// </summary>
        public static TypeDeclarationSyntax? GetContainingType(SyntaxNode node)
        {
            SyntaxNode? current = node;

            while (current is not null)
            {
                if (current is TypeDeclarationSyntax type)
                {
                    return type;
                }

                current = current.Parent;
            }

            return null;
        }

        public static bool IsInvocationOfMember(InvocationExpressionSyntax invocation, string memberName)
        {
            return invocation.Expression switch
            {
                MemberAccessExpressionSyntax memberAccess =>
                    string.Equals(memberAccess.Name.Identifier.ValueText, memberName, StringComparison.Ordinal),
                IdentifierNameSyntax identifier =>
                    string.Equals(identifier.Identifier.ValueText, memberName, StringComparison.Ordinal),
                _ => false,
            };
        }

        /// <summary>
        /// Walks up the syntax tree from <paramref name="node"/> to find the nearest enclosing loop
        /// (for/foreach/while/do), stopping at the enclosing method/local function boundary.
        /// </summary>
        public static SyntaxNode? FindEnclosingLoop(SyntaxNode node)
        {
            SyntaxNode? current = node;

            while (current is not null)
            {
                if (current is ForStatementSyntax ||
                    current is ForEachStatementSyntax ||
                    current is WhileStatementSyntax ||
                    current is DoStatementSyntax)
                {
                    return current;
                }

                if (current is MethodDeclarationSyntax || current is LocalFunctionStatementSyntax)
                {
                    return null;
                }

                current = current.Parent;
            }

            return null;
        }

        /// <summary>
        /// Returns the first string-literal or interpolated-string argument of a logging invocation,
        /// treated as the message template, or <c>null</c> if none is found.
        /// </summary>
        public static ExpressionSyntax? GetMessageTemplateArgument(InvocationExpressionSyntax invocation)
        {
            foreach (ArgumentSyntax argument in invocation.ArgumentList.Arguments)
            {
                if (argument.Expression is LiteralExpressionSyntax literal &&
                    literal.Token.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.StringLiteralToken))
                {
                    return literal;
                }

                if (argument.Expression is InterpolatedStringExpressionSyntax interpolated)
                {
                    return interpolated;
                }
            }

            return null;
        }

        /// <summary>
        /// Counts <c>{Name}</c>-style named placeholders in a structured-logging message template.
        /// </summary>
        public static int CountNamedPlaceholders(string messageTemplate)
        {
            int count = 0;
            int index = 0;

            while (index < messageTemplate.Length)
            {
                if (messageTemplate[index] == '{')
                {
                    int closeIndex = messageTemplate.IndexOf('}', index + 1);

                    if (closeIndex > index)
                    {
                        count++;
                        index = closeIndex + 1;
                        continue;
                    }
                }

                index++;
            }

            return count;
        }

        /// <summary>
        /// Returns <c>true</c> if the given method name is one of the recognized ILogger&lt;T&gt;
        /// severity-level logging methods (excludes <see cref="BeginScopeMethodName"/> and the
        /// generic <c>Log</c> overload).
        /// </summary>
        public static bool IsSeverityLogMethodName(string methodName)
        {
            return GetSeverityRank(methodName) >= 0;
        }

        /// <summary>
        /// Returns <c>true</c> if any argument name in <paramref name="argument"/>'s enclosing
        /// invocation looks like it carries the correlation identifier.
        /// </summary>
        public static bool ArgumentNameLooksLikeCorrelationId(string name)
        {
            foreach (string fragment in CorrelationIdNameFragments)
            {
                if (name.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns <c>true</c> if <paramref name="name"/> starts with one of
        /// <see cref="AuditableVerbPrefixes"/> (e.g. <c>CreateDocument</c>, <c>ApproveRequest</c>).
        /// </summary>
        public static bool StartsWithAuditableVerb(string name)
        {
            foreach (string prefix in AuditableVerbPrefixes)
            {
                if (name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns <c>true</c> if <paramref name="name"/> looks like the repository's dedicated
        /// audit-logging abstraction (an <c>IAuditLogger</c> field/type or an <c>_auditLogger</c>
        /// identifier), as distinct from <c>ILogger&lt;T&gt;</c> operational logging.
        /// </summary>
        public static bool LooksLikeAuditLogger(string name)
        {
            return name.IndexOf(AuditLoggerFieldNameFragment, StringComparison.OrdinalIgnoreCase) >= 0 ||
                   name.IndexOf(AuditLoggerInterfaceNameFragment, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Returns <c>true</c> if <paramref name="name"/> looks like the repository's
        /// <c>HttpClientManager</c> convention type, used to forward the correlation identifier on
        /// outbound HTTP calls (GlobalLoggingStandards.md logging.4.3).
        /// </summary>
        public static bool LooksLikeHttpClientManager(string name)
        {
            return name.IndexOf(HttpClientManagerClassNameFragment, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Returns <c>true</c> if <paramref name="receiverType"/> is (or implements)
        /// <c>ILogger&lt;T&gt;</c> or the non-generic <c>ILogger</c>.
        /// </summary>
        public static bool IsLoggerReceiverType(
            ITypeSymbol receiverType,
            INamedTypeSymbol? loggerGenericSymbol,
            INamedTypeSymbol? loggerSymbol)
        {
            return IsAssignableToLoggerSymbol(receiverType, loggerGenericSymbol) ||
                   IsAssignableToLoggerSymbol(receiverType, loggerSymbol);
        }

        private static bool IsAssignableToLoggerSymbol(ITypeSymbol receiverType, INamedTypeSymbol? loggerSymbol)
        {
            if (loggerSymbol is null)
            {
                return false;
            }

            if (receiverType is INamedTypeSymbol namedReceiverType &&
                SymbolEqualityComparer.Default.Equals(namedReceiverType.OriginalDefinition, loggerSymbol))
            {
                return true;
            }

            foreach (INamedTypeSymbol iface in receiverType.AllInterfaces)
            {
                if (SymbolEqualityComparer.Default.Equals(iface.OriginalDefinition, loggerSymbol))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Counts the statements directly inside <paramref name="block"/> that are logging
        /// invocations (used to detect per-item logging inside a tight loop body,
        /// GlobalLoggingStandards.md logging.9).
        /// </summary>
        public static bool ContainsDirectLoggerInvocation(SyntaxNode body)
        {
            foreach (InvocationExpressionSyntax invocation in body.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
            {
                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                    IsSeverityLogMethodName(memberAccess.Name.Identifier.ValueText))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
