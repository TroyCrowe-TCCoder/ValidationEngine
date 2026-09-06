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
    /// Enforces GlobalCachingStandards.md caching.7.1: any write operation (create, update, delete)
    /// that changes data currently held in cache must explicitly remove the affected cache entries
    /// via IDistributedCache.RemoveAsync, passing a CancellationToken, as part of the same service
    /// method as the write. Invalidation must not be deferred elsewhere.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CacheInvalidationOnWriteAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CACHE010";

        private const string CancellationTokenMetadataName = "System.Threading.CancellationToken";

        private static readonly LocalizableString Title =
            "Write operations in cache-aware services must invalidate affected cache entries";

        private static readonly LocalizableString MessageFormat =
            "'{0}' is a write operation in a cache-aware service but does not call IDistributedCache.RemoveAsync with a CancellationToken in the same method; invalidation must occur in the same logical operation as the write, per GlobalCachingStandards.md caching.7.1";

        private static readonly LocalizableString Description =
            "Any write operation (create, update, delete) that changes data currently held in cache " +
            "must explicitly remove the affected cache entries as part of the same logical operation. " +
            "Invalidation must use RemoveAsync from IDistributedCache and must always pass a " +
            "CancellationToken. Invalidation must occur in the same service method as the write - it " +
            "must not be deferred to a background process unless the staleness window is explicitly " +
            "documented and approved. See GlobalCachingStandards.md Section 7.1.";

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
                INamedTypeSymbol? distributedCacheSymbol = compilationContext.Compilation.GetTypeByMetadataName(
                    CachingWellKnownTypes.IDistributedCacheMetadataName);

                if (distributedCacheSymbol is null)
                {
                    // IDistributedCache is not referenced by this compilation; nothing to flag.
                    return;
                }

                INamedTypeSymbol? cancellationTokenSymbol = compilationContext.Compilation.GetTypeByMetadataName(
                    CancellationTokenMetadataName);

                compilationContext.RegisterSyntaxNodeAction(
                    syntaxContext => AnalyzeMethod(syntaxContext, distributedCacheSymbol, cancellationTokenSymbol),
                    SyntaxKind.MethodDeclaration);
            });
        }

        private static void AnalyzeMethod(
            SyntaxNodeAnalysisContext context,
            INamedTypeSymbol distributedCacheSymbol,
            INamedTypeSymbol? cancellationTokenSymbol)
        {
            var method = (MethodDeclarationSyntax)context.Node;

            if (method.Body is null && method.ExpressionBody is null)
            {
                // Abstract/interface/partial declarations have no body to inspect.
                return;
            }

            if (CachingWellKnownTypes.GetMatchingWriteVerbPrefix(method.Identifier.ValueText) is null)
            {
                return;
            }

            TypeDeclarationSyntax? containingType = CachingWellKnownTypes.GetContainingType(method);

            if (containingType is null ||
                !CachingWellKnownTypes.TypeUsesDistributedCache(containingType, context.SemanticModel, distributedCacheSymbol))
            {
                return;
            }

            if (HasCacheInvalidationCall(method, context, distributedCacheSymbol, cancellationTokenSymbol))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, method.Identifier.GetLocation(), method.Identifier.ValueText));
        }

        private static bool HasCacheInvalidationCall(
            MethodDeclarationSyntax method,
            SyntaxNodeAnalysisContext context,
            INamedTypeSymbol distributedCacheSymbol,
            INamedTypeSymbol? cancellationTokenSymbol)
        {
            SyntaxNode? body = (SyntaxNode?)method.Body ?? method.ExpressionBody;

            if (body is null)
            {
                return false;
            }

            foreach (InvocationExpressionSyntax invocation in body.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
            {
                if (!CachingWellKnownTypes.IsInvocationOfMember(invocation, CachingWellKnownTypes.CacheRemoveAsyncMethodName))
                {
                    continue;
                }

                ISymbol? invokedSymbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol;

                if (invokedSymbol is not IMethodSymbol methodSymbol ||
                    !CachingWellKnownTypes.IsDistributedCacheMethod(methodSymbol, distributedCacheSymbol))
                {
                    continue;
                }

                if (cancellationTokenSymbol is null ||
                    HasCancellationTokenArgument(invocation, context.SemanticModel, cancellationTokenSymbol, context.CancellationToken))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasCancellationTokenArgument(
            InvocationExpressionSyntax invocation,
            SemanticModel semanticModel,
            INamedTypeSymbol cancellationTokenSymbol,
            CancellationToken cancellationToken)
        {
            foreach (ArgumentSyntax argument in invocation.ArgumentList.Arguments)
            {
                ITypeSymbol? argumentType = semanticModel.GetTypeInfo(argument.Expression, cancellationToken).Type;

                if (SymbolEqualityComparer.Default.Equals(argumentType, cancellationTokenSymbol))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
