using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Database;

/// <summary>
/// DB002: Flags a literal C# `null` passed directly as a SqlParameter value via AddWithValue.
/// Per database.5.2, null parameters must be passed as DBNull.Value, never as C# null.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NullParameterMustUseDbNullAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DB002";

    public static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Null SQL parameter values must use DBNull.Value",
        messageFormat: "Pass DBNull.Value instead of a C# null literal to SqlParameter '{0}'",
        category: "Database",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Null parameters must be passed as DBNull.Value, never as C# null. See database.5.2.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

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
            memberAccess.Name.Identifier.Text != DatabaseWellKnownTypes.AddWithValueMethodName)
        {
            return;
        }

        var arguments = invocation.ArgumentList.Arguments;
        if (arguments.Count != 2)
        {
            return;
        }

        var valueArgument = arguments[1].Expression;
        if (valueArgument is not LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NullLiteralExpression })
        {
            return;
        }

        var semanticModel = context.SemanticModel;
        var methodSymbol = semanticModel.GetSymbolInfo(memberAccess, context.CancellationToken).Symbol as IMethodSymbol;
        var containingType = methodSymbol?.ReceiverType ?? methodSymbol?.ContainingType;
        if (!IsSqlParameterCollectionType(containingType))
        {
            return;
        }

        var parameterNameArgument = arguments[0].Expression;
        var parameterName = parameterNameArgument is LiteralExpressionSyntax literal
            ? literal.Token.ValueText
            : parameterNameArgument.ToString();

        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), parameterName));
    }

    private static bool IsSqlParameterCollectionType(ITypeSymbol? type)
    {
        if (type is null)
        {
            return false;
        }

        var name = type.ToDisplayString();
        return name == DatabaseWellKnownTypes.SqlParameterCollectionMetadataName ||
               name == DatabaseWellKnownTypes.MicrosoftSqlParameterCollectionMetadataName;
    }
}
