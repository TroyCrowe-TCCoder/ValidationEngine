using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ValidationEngine.Analyzers.Security
{
    /// <summary>
    /// Shared metadata names, method/type-name constants, and small syntax-walking helpers reused
    /// across the Security domain analyzers. Centralizing these avoids duplicating lookup/detection
    /// boilerplate in every individual GlobalSecurityStandards.md rule analyzer.
    /// </summary>
    internal static class SecurityWellKnownTypes
    {
        public const string ProgramFileName = "Program.cs";

        public const string SecretClientMetadataName = "Azure.Security.KeyVault.Secrets.SecretClient";
        public const string KeyVaultClientMetadataName = "Microsoft.Azure.KeyVault.KeyVaultClient";
        public const string SqlCommandMetadataNameSystemData = "System.Data.SqlClient.SqlCommand";
        public const string SqlCommandMetadataNameMicrosoftData = "Microsoft.Data.SqlClient.SqlCommand";
        public const string HttpClientMetadataName = "System.Net.Http.HttpClient";
        public const string CancellationTokenMetadataName = "System.Threading.CancellationToken";

        public const string AllowAnonymousAttributeName = "AllowAnonymous";
        public const string AddInMemoryTokenCachesMethodName = "AddInMemoryTokenCaches";
        public const string AddDistributedTokenCachesMethodName = "AddDistributedTokenCaches";

        public const string EscapeUriStringMethodName = "EscapeUriString";
        public const string EscapeDataStringMethodName = "EscapeDataString";
        public const string UriTypeName = "Uri";
        public const string UriMetadataName = "System.Uri";

        public const string TaskDelayMethodName = "Delay";
        public const string TaskTypeName = "Task";
        public const string IsCancellationRequestedPropertyName = "IsCancellationRequested";
        public const string ThrowIfCancellationRequestedMethodName = "ThrowIfCancellationRequested";

        public const string MalwareScannerInterfaceNameFragment = "IMalwareScanner";
        public const string NoOpMalwareScannerTypeNameFragment = "NoOp";
        public const string TestsProjectNameFragment = "Tests";

        public static readonly ImmutableArray<string> SecretShapedNameFragments = ImmutableArray.Create(
            "connectionstring",
            "connstring",
            "secret",
            "password",
            "apikey");

        public static bool IsSecretShapedName(string identifierName)
        {
            string normalized = identifierName.ToLowerInvariant();
            foreach (var fragment in SecretShapedNameFragments)
            {
                if (normalized.Contains(fragment))
                    return true;
            }

            return false;
        }

        public const string ValidateIssuerPropertyName = "ValidateIssuer";
        public const string ValidateAudiencePropertyName = "ValidateAudience";
        public const string MapInboundClaimsPropertyName = "MapInboundClaims";
        public const string RequireHttpsMetadataPropertyName = "RequireHttpsMetadata";
        public const string JwtBearerOptionsTypeNameFragment = "JwtBearerOptions";

        public const string QueryAsyncMethodName = "QueryAsync";
        public const string ExecuteMethodName = "Execute";
        public const string ExecuteNonQueryMethodName = "ExecuteNonQuery";
        public const string ExecuteReaderMethodName = "ExecuteReader";
        public const string ExecuteScalarMethodName = "ExecuteScalar";
        public const string CommandTextPropertyName = "CommandText";

        public static bool IsProgramFile(string filePath)
        {
            return filePath.EndsWith(ProgramFileName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsTestProjectFile(string filePath)
        {
            return filePath.IndexOf(TestsProjectNameFragment, StringComparison.OrdinalIgnoreCase) >= 0;
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
        /// Returns <c>true</c> if <paramref name="expression"/> is a string concatenation
        /// (<c>+</c> operator) or interpolated string, either of which is a red flag when building
        /// connection strings, SQL text, or URLs from caller-supplied values.
        /// </summary>
        public static bool IsConcatenatedOrInterpolatedString(ExpressionSyntax expression)
        {
            return expression switch
            {
                InterpolatedStringExpressionSyntax => true,
                BinaryExpressionSyntax binary when binary.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.AddExpression) => true,
                _ => false,
            };
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

        /// <summary>
        /// Walks up the syntax tree from <paramref name="node"/> to find the nearest enclosing
        /// loop statement (<c>while</c>, <c>for</c>, <c>foreach</c>, <c>do</c>), or <c>null</c> if
        /// none is found before reaching a member declaration boundary.
        /// </summary>
        public static StatementSyntax? FindEnclosingLoop(SyntaxNode node)
        {
            SyntaxNode? current = node;

            while (current is not null)
            {
                switch (current)
                {
                    case WhileStatementSyntax:
                    case ForStatementSyntax:
                    case ForEachStatementSyntax:
                    case DoStatementSyntax:
                        return (StatementSyntax)current;
                    case MemberDeclarationSyntax:
                        return null;
                }

                current = current.Parent;
            }

            return null;
        }

        public static readonly ImmutableArray<string> InlineSqlExecutionMethodNames = ImmutableArray.Create(
            ExecuteMethodName,
            ExecuteNonQueryMethodName,
            ExecuteReaderMethodName,
            ExecuteScalarMethodName,
            QueryAsyncMethodName);
    }
}
