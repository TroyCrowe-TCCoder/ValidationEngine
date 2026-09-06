using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Caching
{
    /// <summary>
    /// Enforces GlobalCachingStandards.md caching.7.2: when a write operation invalidates a group of
    /// related cache entries, all affected keys must be explicitly invalidated using known identifiers
    /// from the CacheKeys class. Wildcard or pattern-based key deletion (e.g. Redis KEYS with a
    /// wildcard, or IServer.Keys(...) enumeration) must not be used in production code — it is a
    /// blocking operation that degrades Redis performance under load.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NoWildcardKeyDeletionAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CACHE009";

        private const string KeysMethodName = "Keys";
        private const string ExecuteMethodName = "Execute";
        private const string ExecuteAsyncMethodName = "ExecuteAsync";
        private const string KeysCommandName = "KEYS";
        private const string ScanCommandName = "SCAN";

        private static readonly LocalizableString Title =
            "Wildcard or pattern-based Redis key deletion must not be used";

        private static readonly LocalizableString MessageFormat =
            "'{0}' performs wildcard/pattern-based key enumeration; explicitly invalidate known keys from the CacheKeys class instead, per GlobalCachingStandards.md caching.7.2";

        private static readonly LocalizableString Description =
            "When a write operation invalidates a group of related cache entries, all affected keys " +
            "must be explicitly invalidated using known identifiers from the CacheKeys class. Wildcard " +
            "or pattern-based key deletion (e.g. Redis KEYS with a wildcard, or IServer.Keys(...)) must " +
            "not be used in production code — it is a blocking operation that degrades Redis performance " +
            "under load. See GlobalCachingStandards.md Section 7.2.";

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

            context.RegisterCompilationStartAction(compilationContext =>
            {
                INamedTypeSymbol? serverSymbol = compilationContext.Compilation.GetTypeByMetadataName(
                    CachingWellKnownTypes.IServerMetadataName);
                INamedTypeSymbol? databaseSymbol = compilationContext.Compilation.GetTypeByMetadataName(
                    CachingWellKnownTypes.IDatabaseMetadataName);

                if (serverSymbol is null && databaseSymbol is null)
                {
                    // StackExchange.Redis is not referenced by this compilation; nothing to flag.
                    return;
                }

                compilationContext.RegisterSyntaxNodeAction(
                    syntaxContext => AnalyzeInvocation(syntaxContext, serverSymbol, databaseSymbol),
                    SyntaxKind.InvocationExpression);
            });
        }

        private static void AnalyzeInvocation(
            SyntaxNodeAnalysisContext context,
            INamedTypeSymbol? serverSymbol,
            INamedTypeSymbol? databaseSymbol)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            {
                return;
            }

            ISymbol? invokedSymbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol;

            if (invokedSymbol is not IMethodSymbol)
            {
                return;
            }

            // Check the receiver's actual type rather than the method's declaring type, since members
            // such as ExecuteAsync are declared on a base interface (IDatabaseAsync) rather than IDatabase
            // itself.
            ITypeSymbol? receiverType = context.SemanticModel.GetTypeInfo(memberAccess.Expression, context.CancellationToken).Type;

            bool isRedisServerOrDatabaseMethod =
                (serverSymbol is not null && ImplementsOrIs(receiverType, serverSymbol)) ||
                (databaseSymbol is not null && ImplementsOrIs(receiverType, databaseSymbol));

            if (!isRedisServerOrDatabaseMethod)
            {
                return;
            }

            string methodName = memberAccess.Name.Identifier.ValueText;

            if (string.Equals(methodName, KeysMethodName, StringComparison.Ordinal))
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), "IServer.Keys(...)"));
                return;
            }

            if (string.Equals(methodName, ExecuteMethodName, StringComparison.Ordinal) ||
                string.Equals(methodName, ExecuteAsyncMethodName, StringComparison.Ordinal))
            {
                if (IsRawKeysOrScanCommand(invocation))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), $"{methodName}(\"{GetFirstArgumentCommandText(invocation)}\", ...)"));
                }
            }
        }

        private static bool ImplementsOrIs(ITypeSymbol? type, INamedTypeSymbol target)
        {
            if (type is null)
            {
                return false;
            }

            if (SymbolEqualityComparer.Default.Equals(type, target))
            {
                return true;
            }

            return type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, target));
        }

        private static bool IsRawKeysOrScanCommand(InvocationExpressionSyntax invocation)
        {
            string? commandText = GetFirstArgumentCommandText(invocation);

            return commandText is not null &&
                   (string.Equals(commandText, KeysCommandName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(commandText, ScanCommandName, StringComparison.OrdinalIgnoreCase));
        }

        private static string? GetFirstArgumentCommandText(InvocationExpressionSyntax invocation)
        {
            ArgumentSyntax? firstArgument = invocation.ArgumentList.Arguments.FirstOrDefault();

            if (firstArgument?.Expression is LiteralExpressionSyntax literal &&
                literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                return literal.Token.ValueText;
            }

            return null;
        }
    }
}
