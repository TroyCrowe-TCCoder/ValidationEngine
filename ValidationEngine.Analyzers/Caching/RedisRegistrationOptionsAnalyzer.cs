using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Caching
{
    /// <summary>
    /// Enforces GlobalCachingStandards.md caching.3.1: Redis registration via
    /// AddStackExchangeRedisCache must not use a hardcoded connection string and must configure
    /// AbortOnConnectFail (so the app does not crash on startup if Redis is briefly unavailable).
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class RedisRegistrationOptionsAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CACHE002";

        private const string RegistrationMethodName = "AddStackExchangeRedisCache";
        private const string ConfigurationPropertyName = "Configuration";
        private const string AbortOnConnectFailPropertyName = "AbortOnConnectFail";

        private static readonly LocalizableString Title =
            "Redis registration is missing required options";

        private static readonly LocalizableString MessageFormat = "{0}";

        private static readonly LocalizableString Description =
            "AddStackExchangeRedisCache registration must source its connection string from configuration " +
            "(never a hardcoded literal) and must explicitly set AbortOnConnectFail so startup does not crash " +
            "when Redis is briefly unavailable. See GlobalCachingStandards.md Section 3.1.";

        private const string Category = "Caching";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: "https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalCachingStandards.md");

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

            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess ||
                memberAccess.Name.Identifier.ValueText != RegistrationMethodName)
            {
                return;
            }

            LambdaExpressionSyntax? lambda = invocation.ArgumentList.Arguments
                .Select(a => a.Expression)
                .OfType<LambdaExpressionSyntax>()
                .FirstOrDefault();

            if (lambda is null)
            {
                // No options-configuration lambda at all; nothing further to inspect syntactically.
                return;
            }

            var assignments = lambda.DescendantNodes().OfType<AssignmentExpressionSyntax>().ToList();

            bool hasHardcodedConnectionString = assignments.Any(a =>
                GetAssignedPropertyName(a.Left) == ConfigurationPropertyName &&
                a.Right is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression });

            if (hasHardcodedConnectionString)
            {
                AssignmentExpressionSyntax offending = assignments.First(a =>
                    GetAssignedPropertyName(a.Left) == ConfigurationPropertyName &&
                    a.Right is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.StringLiteralExpression });

                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    offending.Right.GetLocation(),
                    "AddStackExchangeRedisCache uses a hardcoded connection string; source it from configuration (e.g. Key Vault-backed), per GlobalCachingStandards.md caching.3.1"));
            }

            bool hasAbortOnConnectFail = assignments.Any(a =>
                GetAssignedPropertyName(a.Left) == AbortOnConnectFailPropertyName);

            if (!hasAbortOnConnectFail)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    memberAccess.Name.GetLocation(),
                    "AddStackExchangeRedisCache registration does not set AbortOnConnectFail; the app must not crash on startup if Redis is temporarily unavailable, per GlobalCachingStandards.md caching.3.1"));
            }
        }

        private static string? GetAssignedPropertyName(ExpressionSyntax left)
        {
            return left switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
                _ => null,
            };
        }
    }
}
