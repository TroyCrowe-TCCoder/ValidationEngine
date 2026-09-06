using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Caching
{
    /// <summary>
    /// Enforces GlobalCachingStandards.md caching.2.3 / caching.10.1: PII, authentication
    /// tokens/credentials, and financial transaction records must never be written to a cache.
    /// Flags IDistributedCache.SetAsync call sites whose cached value - or the value passed into a
    /// CacheSerializer.Serialize(...) call feeding it - is a variable/member or a type property/field
    /// whose name matches a known-sensitive naming pattern (Password, Token, Ssn, CardNumber, etc.).
    /// This is a heuristic, name-based check; it flags candidates for human review rather than
    /// providing a deterministic guarantee.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class NoSensitiveDataInCacheAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CACHE011";

        private const string SetAsyncMethodName = "SetAsync";
        private const string SerializeMethodName = "Serialize";

        private static readonly LocalizableString Title =
            "Sensitive data must not be written to the cache";

        private static readonly LocalizableString MessageFormat =
            "Cache value passed to SetAsync appears to include sensitive data via '{0}'; PII, authentication tokens/credentials, and financial transaction records must never be written to a cache, per GlobalCachingStandards.md caching.2.3 / caching.10.1";

        private static readonly LocalizableString Description =
            "Sensitive personal data (PII), authentication tokens or credentials, and financial " +
            "transaction records must never be cached. This heuristic flags IDistributedCache.SetAsync " +
            "call sites whose cached value - directly, or via a CacheSerializer.Serialize(...) call - " +
            "is a variable, member, or type property/field whose name matches a known-sensitive naming " +
            "pattern. Flagged occurrences require human confirmation; this check is a candidate " +
            "detector, not a deterministic guarantee. See GlobalCachingStandards.md Sections 2.3 and 10.1.";

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

            if (!CachingWellKnownTypes.IsInvocationOfMember(invocation, SetAsyncMethodName))
            {
                return;
            }

            ISymbol? invokedSymbol = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol;

            if (invokedSymbol is not IMethodSymbol methodSymbol ||
                !CachingWellKnownTypes.IsDistributedCacheMethod(methodSymbol, distributedCacheSymbol))
            {
                return;
            }

            if (invocation.ArgumentList.Arguments.Count < 2)
            {
                return;
            }

            ExpressionSyntax valueArgument = invocation.ArgumentList.Arguments[1].Expression;
            ExpressionSyntax dataExpression = UnwrapSerializedValueExpression(valueArgument);

            string? sensitiveNameFragmentSource = GetSensitiveNameFragmentSource(dataExpression, context.SemanticModel, context.CancellationToken);

            if (sensitiveNameFragmentSource is not null)
            {
                context.ReportDiagnostic(Diagnostic.Create(Rule, valueArgument.GetLocation(), sensitiveNameFragmentSource));
            }
        }

        private static ExpressionSyntax UnwrapSerializedValueExpression(ExpressionSyntax expression)
        {
            if (expression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax memberAccess } invocation &&
                string.Equals(memberAccess.Name.Identifier.ValueText, SerializeMethodName, StringComparison.Ordinal) &&
                GetSimpleName(memberAccess.Expression) is { } receiverName &&
                receiverName.EndsWith(CachingWellKnownTypes.CacheSerializerClassName, StringComparison.Ordinal) &&
                invocation.ArgumentList.Arguments.Count > 0)
            {
                return invocation.ArgumentList.Arguments[0].Expression;
            }

            return expression;
        }

        private static string? GetSensitiveNameFragmentSource(
            ExpressionSyntax expression,
            SemanticModel semanticModel,
            System.Threading.CancellationToken cancellationToken)
        {
            if (GetSimpleName(expression) is { } simpleName &&
                CachingWellKnownTypes.ContainsSensitiveNameFragment(simpleName))
            {
                return simpleName;
            }

            ITypeSymbol? type = semanticModel.GetTypeInfo(expression, cancellationToken).Type;

            return FindSensitiveMemberName(type);
        }

        private static string? FindSensitiveMemberName(ITypeSymbol? type)
        {
            ITypeSymbol? current = type;

            while (current is not null && current.SpecialType != SpecialType.System_Object)
            {
                foreach (ISymbol member in current.GetMembers())
                {
                    if (member is IPropertySymbol { IsIndexer: false } property &&
                        CachingWellKnownTypes.ContainsSensitiveNameFragment(property.Name))
                    {
                        return property.Name;
                    }

                    if (member is IFieldSymbol { IsImplicitlyDeclared: false } field &&
                        CachingWellKnownTypes.ContainsSensitiveNameFragment(field.Name))
                    {
                        return field.Name;
                    }
                }

                current = current.BaseType;
            }

            return null;
        }

        private static string? GetSimpleName(ExpressionSyntax expression)
        {
            return expression switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
                _ => null,
            };
        }
    }
}
