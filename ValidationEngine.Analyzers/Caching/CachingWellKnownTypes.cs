using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ValidationEngine.Analyzers.Caching
{
    /// <summary>
    /// Shared metadata names, naming-convention constants, and small syntax-walking helpers reused
    /// across the Caching domain analyzers. Centralizing these avoids duplicating lookup/detection
    /// boilerplate in every individual GlobalCachingStandards.md rule analyzer.
    /// </summary>
    internal static class CachingWellKnownTypes
    {
        public const string IDistributedCacheMetadataName = "Microsoft.Extensions.Caching.Distributed.IDistributedCache";
        public const string ConnectionMultiplexerMetadataName = "StackExchange.Redis.ConnectionMultiplexer";
        public const string IConnectionMultiplexerMetadataName = "StackExchange.Redis.IConnectionMultiplexer";
        public const string IServerMetadataName = "StackExchange.Redis.IServer";
        public const string IDatabaseMetadataName = "StackExchange.Redis.IDatabase";
        public const string IOptionsMetadataName = "Microsoft.Extensions.Options.IOptions`1";
        public const string IOptionsMonitorMetadataName = "Microsoft.Extensions.Options.IOptionsMonitor`1";
        public const string JsonSerializerMetadataName = "System.Text.Json.JsonSerializer";
        public const string NewtonsoftJsonConvertMetadataName = "Newtonsoft.Json.JsonConvert";

        public const string CacheKeysClassName = "CacheKeys";
        public const string CacheSerializerClassName = "CacheSerializer";
        public const string CacheOptionsClassNameSuffix = "CacheOptions";
        public const string TenantIdParameterName = "tenantId";
        public const string CacheRemoveAsyncMethodName = "RemoveAsync";

        public const string LoggerInterfaceMetadataName = "Microsoft.Extensions.Logging.ILogger`1";
        public const string LoggerNonGenericInterfaceMetadataName = "Microsoft.Extensions.Logging.ILogger";

        public const string LongLivedCacheAttributeName = "LongLivedCache";
        public const string LongLivedCacheAttributeFullName = "LongLivedCacheAttribute";

        public const string SetAsyncMethodName = "SetAsync";
        public const string SetAbsoluteExpirationMethodName = "SetAbsoluteExpiration";
        public const string SetSlidingExpirationMethodName = "SetSlidingExpiration";
        public const string AbsoluteExpirationRelativeToNowPropertyName = "AbsoluteExpirationRelativeToNow";
        public const string AbsoluteExpirationPropertyName = "AbsoluteExpiration";
        public const string SlidingExpirationPropertyName = "SlidingExpiration";

        /// <summary>
        /// Method-name prefixes that indicate a write operation (create, update, delete) against a
        /// data source, used to detect methods that must invalidate cache entries on write
        /// (GlobalCachingStandards.md caching.7.1).
        /// </summary>
        public static readonly ImmutableArray<string> WriteVerbPrefixes = ImmutableArray.Create(
            "Create",
            "Update",
            "Delete",
            "Remove",
            "Insert",
            "Save",
            "Add",
            "Patch");

        /// <summary>
        /// Naming fragments that indicate a value/type is likely to hold sensitive data that must
        /// never be written to a cache (GlobalCachingStandards.md caching.2.3 / caching.10.1).
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
        /// type declaration (class/struct/record), or <c>null</c> if none is found.
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

        /// <summary>
        /// Returns true when <paramref name="invocation"/> is a member-access invocation whose member
        /// name equals <paramref name="methodName"/> (e.g. <c>_cache.SetAsync(...)</c> when
        /// <paramref name="methodName"/> is <c>"SetAsync"</c>). This is a syntactic check only; callers
        /// that need semantic confirmation (the receiver is really an IDistributedCache) should also
        /// verify via the semantic model.
        /// </summary>
        public static bool IsInvocationOfMember(Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax invocation, string methodName)
        {
            return invocation.Expression is Microsoft.CodeAnalysis.CSharp.Syntax.MemberAccessExpressionSyntax memberAccess &&
                   string.Equals(memberAccess.Name.Identifier.ValueText, methodName, StringComparison.Ordinal);
        }

        /// <summary>
        /// Returns true when <paramref name="methodSymbol"/> is a direct member of, or an extension
        /// method whose reduced receiver is, <paramref name="distributedCacheSymbol"/> (i.e. it is a
        /// call against an <c>IDistributedCache</c> instance, whether invoked as an instance member or
        /// via the <c>Microsoft.Extensions.Caching.Distributed</c> extension methods).
        /// </summary>
        public static bool IsDistributedCacheMethod(IMethodSymbol methodSymbol, INamedTypeSymbol distributedCacheSymbol)
        {
            if (SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, distributedCacheSymbol))
            {
                return true;
            }

            IMethodSymbol candidate = methodSymbol.ReducedFrom ?? methodSymbol;

            return candidate.Parameters.Length > 0 &&
                   SymbolEqualityComparer.Default.Equals(candidate.Parameters[0].Type, distributedCacheSymbol);
        }

        /// <summary>
        /// Walks up the syntax tree from <paramref name="node"/> to find the nearest enclosing
        /// <c>try</c> statement whose <c>try</c> block (not a <c>catch</c> or <c>finally</c> block)
        /// contains <paramref name="node"/>, stopping at the nearest enclosing method/lambda/local
        /// function boundary. Returns <c>null</c> if no such enclosing try statement exists.
        /// </summary>
        public static TryStatementSyntax? FindEnclosingTryStatement(SyntaxNode node)
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

        /// <summary>
        /// Returns true when any <c>catch</c> clause of <paramref name="tryStatement"/> contains a
        /// <c>throw</c> statement (bare rethrow or <c>throw new ...</c>), which would allow a cache
        /// failure to propagate to the caller instead of degrading gracefully
        /// (GlobalCachingStandards.md caching.9).
        /// </summary>
        public static bool AnyCatchClauseRethrows(TryStatementSyntax tryStatement)
        {
            foreach (CatchClauseSyntax catchClause in tryStatement.Catches)
            {
                if (catchClause.DescendantNodes().OfType<ThrowStatementSyntax>().Any())
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the matching entry from <see cref="WriteVerbPrefixes"/> if <paramref name="methodName"/>
        /// begins with a write-verb prefix followed by a PascalCase word boundary (end-of-name, an
        /// uppercase letter, or a digit) — for example <c>"UpdateClientConfigAs
        /// <c>"Update"</c>, but <c>"Address"</c> does not match <c>"Add"</c>. Returns <c>null</c> when
        /// there is no match.
        /// </summary>
        public static string? GetMatchingWriteVerbPrefix(string methodName)
        {
            foreach (string prefix in WriteVerbPrefixes)
            {
                if (!methodName.StartsWith(prefix, StringComparison.Ordinal))
                {
                    continue;
                }

                if (methodName.Length == prefix.Length)
                {
                    return prefix;
                }

                char boundaryChar = methodName[prefix.Length];

                if (char.IsUpper(boundaryChar) || char.IsDigit(boundaryChar))
                {
                    return prefix;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns true when <paramref name="containingType"/> is a cache-aware service: it has a
        /// constructor parameter, field, or property typed as <see cref="IDistributedCacheMetadataName"/>.
        /// Shared by rules that only apply inside services that actually use the distributed cache
        /// (e.g. caching.7.1, caching.8.1).
        /// </summary>
        public static bool TypeUsesDistributedCache(
            TypeDeclarationSyntax containingType,
            SemanticModel semanticModel,
            INamedTypeSymbol distributedCacheSymbol)
        {
            foreach (ParameterSyntax parameter in containingType.Members
                .OfType<ConstructorDeclarationSyntax>()
                .SelectMany(constructor => constructor.ParameterList.Parameters))
            {
                if (parameter.Type is null)
                {
                    continue;
                }

                ITypeSymbol? parameterType = semanticModel.GetTypeInfo(parameter.Type).Type;

                if (SymbolEqualityComparer.Default.Equals(parameterType, distributedCacheSymbol))
                {
                    return true;
                }
            }

            foreach (FieldDeclarationSyntax field in containingType.Members.OfType<FieldDeclarationSyntax>())
            {
                ITypeSymbol? fieldType = semanticModel.GetTypeInfo(field.Declaration.Type).Type;

                if (SymbolEqualityComparer.Default.Equals(fieldType, distributedCacheSymbol))
                {
                    return true;
                }
            }

            foreach (PropertyDeclarationSyntax property in containingType.Members.OfType<PropertyDeclarationSyntax>())
            {
                ITypeSymbol? propertyType = semanticModel.GetTypeInfo(property.Type).Type;

                if (SymbolEqualityComparer.Default.Equals(propertyType, distributedCacheSymbol))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true when <paramref name="receiverType"/> is (or derives from) an
        /// <c>ILogger</c>/<c>ILogger&lt;T&gt;</c> receiver, matching either the generic or
        /// non-generic logger interface symbol.
        /// </summary>
        public static bool IsLoggerReceiverType(
            ITypeSymbol receiverType,
            INamedTypeSymbol? loggerGenericSymbol,
            INamedTypeSymbol? loggerSymbol)
        {
            if (loggerSymbol is not null && receiverType.AllInterfaces.Append((INamedTypeSymbol)receiverType)
                .Any(i => SymbolEqualityComparer.Default.Equals(i, loggerSymbol)))
            {
                return true;
            }

            if (loggerGenericSymbol is not null && receiverType is INamedTypeSymbol named)
            {
                if (SymbolEqualityComparer.Default.Equals(named.ConstructedFrom, loggerGenericSymbol))
                {
                    return true;
                }

                return named.AllInterfaces.Any(i =>
                    SymbolEqualityComparer.Default.Equals(i.ConstructedFrom, loggerGenericSymbol));
            }

            return false;
        }

        /// <summary>
        /// Returns true when <paramref name="methodName"/> is one of the <c>ILogger&lt;T&gt;</c>
        /// logging extension method names (LogTrace/LogDebug/LogInformation/LogWarning/LogError/
        /// LogCritical/Log).
        /// </summary>
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
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Returns true when <paramref name="methodSymbol"/> is a member of, or an attribute
        /// declared on, the method/property named <see cref="LongLivedCacheAttributeName"/> or
        /// <see cref="LongLivedCacheAttributeFullName"/>. Used to detect the machine-readable
        /// long-lived cache signal required to enforce GlobalCachingStandards.md caching.6.2.
        /// </summary>
        public static bool HasLongLivedCacheAttribute(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            SyntaxNode? current = node;

            while (current is not null)
            {
                if (current is MethodDeclarationSyntax method)
                {
                    if (MethodHasLongLivedAttribute(method, semanticModel, cancellationToken))
                    {
                        return true;
                    }
                }

                current = current.Parent;
            }

            return false;
        }

        private static bool MethodHasLongLivedAttribute(MethodDeclarationSyntax method, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            foreach (AttributeListSyntax attributeList in method.AttributeLists)
            {
                foreach (AttributeSyntax attribute in attributeList.Attributes)
                {
                    string attributeName = attribute.Name switch
                    {
                        IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                        QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
                        _ => string.Empty,
                    };

                    if (string.Equals(attributeName, LongLivedCacheAttributeName, StringComparison.Ordinal) ||
                        string.Equals(attributeName, LongLivedCacheAttributeFullName, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
