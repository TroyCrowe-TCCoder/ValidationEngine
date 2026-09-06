using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Security
{
    /// <summary>
    /// Enforces GlobalSecurityStandards.md security.4.1: all database operations must be executed
    /// through stored procedures. Inline SQL strings -- including parameterized inline SQL -- must
    /// not appear in any service, repository, or infrastructure class. Flags 'SqlCommand'
    /// construction with an inline SQL text argument, assignment of a raw SQL string to
    /// 'CommandText', and Dapper-style 'QueryAsync'/'Execute*' calls whose SQL argument is a
    /// string literal, interpolated string, or concatenation rather than a stored procedure name.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class InlineSqlAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SEC005";

        private static readonly LocalizableString Title =
            "Inline SQL must not be used; all database operations must go through stored procedures";

        private static readonly LocalizableString MessageFormat =
            "'{0}' contains inline SQL text; all database operations must be executed through stored procedures, per GlobalSecurityStandards.md security.4.1";

        private static readonly LocalizableString Description =
            "All database operations must be executed through stored procedures. Inline SQL strings -- " +
            "including parameterized inline SQL -- must not appear in any service, repository, or " +
            "infrastructure class. See GlobalSecurityStandards.md Section 4.1.";

        private const string Category = "Security";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: "https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalSecurityStandards.md");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(compilationContext =>
            {
                compilationContext.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
                compilationContext.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.SimpleAssignmentExpression);
                compilationContext.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
            });
        }

        private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
        {
            var creation = (ObjectCreationExpressionSyntax)context.Node;

            if (context.SemanticModel.GetSymbolInfo(creation, context.CancellationToken).Symbol is not IMethodSymbol constructorSymbol)
            {
                return;
            }

            string typeMetadataName = constructorSymbol.ContainingType?.ToDisplayString() ?? string.Empty;

            if (typeMetadataName != SecurityWellKnownTypes.SqlCommandMetadataNameSystemData &&
                typeMetadataName != SecurityWellKnownTypes.SqlCommandMetadataNameMicrosoftData)
            {
                return;
            }

            ArgumentSyntax? sqlArgument = creation.ArgumentList?.Arguments.FirstOrDefault();

            if (sqlArgument is null || !IsRawSqlText(sqlArgument.Expression))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                sqlArgument.Expression.GetLocation(),
                constructorSymbol.ContainingType!.Name));
        }

        private static void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
        {
            var assignment = (AssignmentExpressionSyntax)context.Node;

            if (assignment.Left is not MemberAccessExpressionSyntax memberAccess ||
                !string.Equals(memberAccess.Name.Identifier.ValueText, SecurityWellKnownTypes.CommandTextPropertyName, System.StringComparison.Ordinal))
            {
                return;
            }

            if (!IsRawSqlText(assignment.Right))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                assignment.Right.GetLocation(),
                SecurityWellKnownTypes.CommandTextPropertyName));
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;

            string? memberName = invocation.Expression switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
                _ => null,
            };

            if (memberName is null || !SecurityWellKnownTypes.InlineSqlExecutionMethodNames.Contains(memberName))
            {
                return;
            }

            ArgumentSyntax? sqlArgument = invocation.ArgumentList.Arguments.FirstOrDefault();

            if (sqlArgument is null)
            {
                return;
            }

            ExpressionSyntax sqlExpression = sqlArgument.Expression;

            if (sqlExpression is IdentifierNameSyntax identifier)
            {
                VariableDeclaratorSyntax? declarator = FindLocalDeclarator(invocation, identifier.Identifier.ValueText);

                if (declarator?.Initializer is null || !IsRawSqlText(declarator.Initializer.Value))
                {
                    return;
                }

                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    declarator.Initializer.Value.GetLocation(),
                    memberName));
                return;
            }

            if (!IsRawSqlText(sqlExpression))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                sqlExpression.GetLocation(),
                memberName));
        }

        /// <summary>
        /// Walks up the syntax tree from <paramref name="node"/> looking for a local variable
        /// declaration named <paramref name="identifierName"/> in an enclosing block, so an
        /// invocation argument passed by variable (e.g. 'var sql = "..."; connection.QueryAsync(sql, ...)')
        /// can be traced back to its initializer.
        /// </summary>
        private static VariableDeclaratorSyntax? FindLocalDeclarator(SyntaxNode node, string identifierName)
        {
            SyntaxNode? current = node.Parent;

            while (current is not null)
            {
                if (current is BlockSyntax block)
                {
                    foreach (StatementSyntax statement in block.Statements)
                    {
                        if (statement is not LocalDeclarationStatementSyntax localDeclaration)
                        {
                            continue;
                        }

                        foreach (VariableDeclaratorSyntax declarator in localDeclaration.Declaration.Variables)
                        {
                            if (string.Equals(declarator.Identifier.ValueText, identifierName, System.StringComparison.Ordinal))
                            {
                                return declarator;
                            }
                        }
                    }
                }

                current = current.Parent;
            }

            return null;
        }

        private static bool IsRawSqlText(ExpressionSyntax expression)
        {
            if (expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                return true;
            }

            return SecurityWellKnownTypes.IsConcatenatedOrInterpolatedString(expression);
        }
    }
}
