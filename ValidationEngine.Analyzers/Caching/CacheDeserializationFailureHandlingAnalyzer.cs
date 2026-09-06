using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Caching
{
    /// <summary>
    /// Enforces GlobalCachingStandards.md caching.8.3: deserialization failures must be handled by
    /// evicting the stale cache entry via IDistributedCache.RemoveAsync, logging a warning, and
    /// falling through to the data source. Deserialization failures must never throw to the caller.
    /// Flags CacheSerializer.Deserialize&lt;T&gt;(...) call sites that are not enclosed in a try block
    /// whose catch clause calls IDistributedCache.RemoveAsync.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CacheDeserializationFailureHandlingAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CACHE013";

        private const string DeserializeMethodName = "Deserialize";

        private static readonly LocalizableString Title =
            "Cache deserialization failures must evict the stale entry and never throw";

        private static readonly LocalizableString MessageFormat =
            "'{0}' calls CacheSerializer.Deserialize without a surrounding try/catch that evicts the stale entry via IDistributedCache.RemoveAsync; deserialization failures must be handled by evicting the stale entry, logging a warning, and falling through to the data source, per GlobalCachingStandards.md caching.8.3";

        private static readonly LocalizableString Description =
            "Deserialization can fail when a cached entry was written by a prior application version " +
            "with a different object shape. Deserialization failures must be caught, the stale entry " +
            "evicted via IDistributedCache.RemoveAsync, a warning logged, and the call must fall " +
            "through to the data source. Deserialization failures must never throw to the caller. " +
            "See GlobalCachingStandards.md Section 8.3.";

        private const string Category = "Caching";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
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
                INamedTypeSymbol? distributedCacheSymbol = compilationContext.Compilation.GetTypeByMetadataName(
                    CachingWellKnownTypes.IDistributedCacheMetadataName);

                if (distributedCacheSymbol is null)
                {
                    // IDistributedCache is not referenced by this compilation; nothing to flag.
                    return;
                }

                compilationContext.RegisterSyntaxNodeAction(
                    syntaxContext => AnalyzeInvocation(syntaxContext, distributedCacheSymbol),
                    SyntaxKind.InvocationExpression);
            });
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, INamedTypeSymbol distributedCacheSymbol)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            if (!IsCacheSerializerDeserializeInvocation(invocation))
            {
                return;
            }

            MethodDeclarationSyntax? containingMethod = CachingWellKnownTypes.GetContainingMethod(invocation);

            if (containingMethod is null)
            {
                return;
            }

            TryStatementSyntax? enclosingTry = CachingWellKnownTypes.FindEnclosingTryStatement(invocation);

            bool isHandled = enclosingTry is not null &&
                CatchHandlesEviction(enclosingTry, context.SemanticModel, distributedCacheSymbol, context.CancellationToken);

            if (!isHandled)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), containingMethod.Identifier.ValueText));
            }
        }

        private static bool IsCacheSerializerDeserializeInvocation(InvocationExpressionSyntax invocation)
        {
            return invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                   string.Equals(memberAccess.Name.Identifier.ValueText, DeserializeMethodName, StringComparison.Ordinal) &&
                   GetReceiverSimpleName(memberAccess.Expression) is { } receiverName &&
                   receiverName.EndsWith(CachingWellKnownTypes.CacheSerializerClassName, StringComparison.Ordinal);
        }

        private static string? GetReceiverSimpleName(ExpressionSyntax expression)
        {
            return expression switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
                _ => null,
            };
        }

        private static TryStatementSyntax? FindEnclosingTryStatement(SyntaxNode node)
        {
            SyntaxNode? current = node.Parent;

            while (current is not null)
            {
                if (current is MethodDeclarationSyntax or AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax)
                {
                    return null;
                }

                if (current is TryStatementSyntax tryStatement && tryStatement.Block.Span.Contains(node.Span))
                {
                    return tryStatement;
                }

                current = current.Parent;
            }

            return null;
        }

        private static bool CatchHandlesEviction(
            TryStatementSyntax tryStatement,
            SemanticModel semanticModel,
            INamedTypeSymbol distributedCacheSymbol,
            CancellationToken cancellationToken)
        {
            foreach (CatchClauseSyntax catchClause in tryStatement.Catches)
            {
                foreach (InvocationExpressionSyntax candidateInvocation in catchClause.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    if (!CachingWellKnownTypes.IsInvocationOfMember(candidateInvocation, CachingWellKnownTypes.CacheRemoveAsyncMethodName))
                    {
                        continue;
                    }

                    ISymbol? symbol = semanticModel.GetSymbolInfo(candidateInvocation, cancellationToken).Symbol;

                    if (symbol is IMethodSymbol methodSymbol &&
                        CachingWellKnownTypes.IsDistributedCacheMethod(methodSymbol, distributedCacheSymbol))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
