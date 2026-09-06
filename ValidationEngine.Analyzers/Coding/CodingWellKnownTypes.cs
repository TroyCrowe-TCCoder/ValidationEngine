using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ValidationEngine.Analyzers.Coding
{
    /// <summary>
    /// Shared metadata names, naming-convention constants, and small syntax-walking helpers reused
    /// across the Coding domain analyzers. Centralizing these avoids duplicating lookup/detection
    /// boilerplate in every individual GlobalCodingStandards.md rule analyzer.
    /// </summary>
    internal static class CodingWellKnownTypes
    {
        public const string ProgramFileName = "Program.cs";

        public const string DbContextMetadataName = "Microsoft.EntityFrameworkCore.DbContext";
        public const string SqlCommandMetadataNameSystemData = "System.Data.SqlClient.SqlCommand";
        public const string SqlCommandMetadataNameMicrosoftData = "Microsoft.Data.SqlClient.SqlCommand";
        public const string IServiceProviderMetadataName = "System.IServiceProvider";
        public const string CancellationTokenMetadataName = "System.Threading.CancellationToken";
        public const string ProblemDetailsMetadataName = "Microsoft.AspNetCore.Mvc.ProblemDetails";
        public const string ControllerBaseMetadataName = "Microsoft.AspNetCore.Mvc.ControllerBase";
        public const string ExceptionMetadataName = "System.Exception";
        public const string ApplicationExceptionMetadataName = "System.ApplicationException";
        public const string EventArgsMetadataName = "System.EventArgs";

        public const string AddAuthorizationMethodName = "AddAuthorization";
        public const string UseExceptionHandlerMethodName = "UseExceptionHandler";

        public const string GetServiceMethodName = "GetService";
        public const string GetRequiredServiceMethodName = "GetRequiredService";

        public const string ExtensionsFolderFragment = "Extensions";
        public const string ConstantsFolderFragment = "Constants";
        public const string ExtensionsSuffix = "Extensions";

        public const string DtoSuffix = "Dto";
        public const string ModelSuffix = "Model";

        public const string ExecuteMethodNamePrefix = "Execute";
        public const string StoredProcedureParameterNameFragment = "procedureName";

        public const int MaxMethodLineCount = 40;
        public const int MinDescriptiveIdentifierLength = 3;

        /// <summary>
        /// Conventional short identifiers that are exempt from the minimum-length rule
        /// (loop counters, LINQ lambda parameters, and other well-established short names).
        /// </summary>
        public static readonly ImmutableArray<string> AllowedShortIdentifiers = ImmutableArray.Create(
            "id",
            "ip",
            "ct",
            "db",
            "ok",
            "i",
            "j",
            "k",
            "x",
            "y",
            "z",
            "e",
            "ex",
            "cs",
            "_");

        /// <summary>
        /// Abbreviation fragments that indicate a non-descriptive name regardless of length
        /// (GlobalCodingStandards.md coding.3.2).
        /// </summary>
        public static readonly ImmutableArray<string> AbbreviationDenylist = ImmutableArray.Create(
            "tmp",
            "temp",
            "mgr",
            "svc",
            "ctrl",
            "impl",
            "obj",
            "val",
            "num",
            "cnt",
            "idx",
            "arr",
            "str",
            "btn",
            "lbl",
            "chk",
            "info",
            "data",
            "misc",
            "util");

        public static bool IsProgramFile(string filePath)
        {
            return filePath.EndsWith(ProgramFileName, StringComparison.OrdinalIgnoreCase);
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

        public static bool IsDtoOrModelTypeName(string typeName)
        {
            return typeName.EndsWith(DtoSuffix, StringComparison.Ordinal) ||
                   typeName.EndsWith(ModelSuffix, StringComparison.Ordinal);
        }

        /// <summary>
        /// Returns <c>true</c> if <paramref name="identifier"/> is too short or matches a known
        /// abbreviation fragment, and is not one of the conventional exempt short names
        /// (GlobalCodingStandards.md coding.3.2).
        /// </summary>
        public static bool IsNonDescriptiveIdentifier(string identifier)
        {
            foreach (string allowed in AllowedShortIdentifiers)
            {
                if (string.Equals(identifier, allowed, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (identifier.Length < MinDescriptiveIdentifierLength)
            {
                return true;
            }

            foreach (string fragment in AbbreviationDenylist)
            {
                if (string.Equals(identifier, fragment, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static int GetLineCount(SyntaxNode node)
        {
            FileLinePositionSpan span = node.SyntaxTree.GetLineSpan(node.Span);
            return span.EndLinePosition.Line - span.StartLinePosition.Line + 1;
        }

        /// <summary>
        /// Walks up the syntax tree from <paramref name="node"/> to find the nearest enclosing
        /// <c>for</c>-loop header (the <c>VariableDeclarationSyntax</c> in a for-statement's
        /// initializer), used to exempt conventional loop-counter declarations from naming rules.
        /// </summary>
        public static ForStatementSyntax? FindEnclosingLoopHeader(SyntaxNode node)
        {
            SyntaxNode? current = node;

            while (current is not null)
            {
                if (current is ForStatementSyntax forStatement)
                {
                    return forStatement;
                }

                if (current is StatementSyntax && current is not ForStatementSyntax)
                {
                    return null;
                }

                current = current.Parent;
            }

            return null;
        }

        /// <summary>
        /// Returns <c>true</c> if <paramref name="statement"/> is a guard clause: an <c>if</c>
        /// statement whose body is (or begins with) a <c>throw</c> or <c>return</c> statement
        /// (GlobalCodingStandards.md coding.3.4 / coding.7.2).
        /// </summary>
        public static bool IsGuardClauseStatement(StatementSyntax statement)
        {
            if (statement is not IfStatementSyntax ifStatement)
            {
                return false;
            }

            return IsThrowOrReturn(ifStatement.Statement);
        }

        private static bool IsThrowOrReturn(StatementSyntax statement)
        {
            if (statement is ThrowStatementSyntax || statement is ReturnStatementSyntax)
            {
                return true;
            }

            if (statement is BlockSyntax block && block.Statements.Count > 0)
            {
                StatementSyntax first = block.Statements[0];
                return first is ThrowStatementSyntax || first is ReturnStatementSyntax;
            }

            return false;
        }

        /// <summary>
        /// Returns <c>true</c> if <paramref name="method"/>'s second parameter type looks like an
        /// <c>EventArgs</c>-derived type, indicating a conventional event-handler signature exempt
        /// from the <c>async void</c> prohibition (GlobalCodingStandards.md coding.8.3).
        /// </summary>
        public static bool HasEventHandlerSignature(MethodDeclarationSyntax method)
        {
            if (method.ParameterList.Parameters.Count != 2)
            {
                return false;
            }

            TypeSyntax? secondParameterType = method.ParameterList.Parameters[1].Type;

            string? typeName = secondParameterType switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                GenericNameSyntax generic => generic.Identifier.ValueText,
                QualifiedNameSyntax qualified => qualified.Right.Identifier.ValueText,
                _ => null,
            };

            return typeName is not null && typeName.EndsWith("EventArgs", StringComparison.Ordinal);
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
    }
}
