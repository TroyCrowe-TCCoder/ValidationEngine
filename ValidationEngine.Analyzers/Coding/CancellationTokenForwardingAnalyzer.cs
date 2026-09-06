using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Coding
{
    /// <summary>
    /// Enforces GlobalCodingStandards.md coding.8.4: every 'async' method must accept a
    /// 'CancellationToken' parameter and forward it to every awaited call that accepts one. Flags
    /// async methods missing a 'CancellationToken' parameter, and flags 'CancellationToken.None'
    /// substituted in place of the method's own token when an accepted token is available.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class CancellationTokenForwardingAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CODE022";

        private const string CancellationTokenNoneMemberName = "None";
        private const string CancellationTokenTypeName = "CancellationToken";
        private const string TaskTypeName = "Task";
        private const string ValueTaskTypeName = "ValueTask";

        private static readonly LocalizableString Title =
            "Async methods must accept and forward a CancellationToken";

        private static readonly LocalizableString MessageFormat =
            "'{0}' {1}, per GlobalCodingStandards.md coding.8.4";

        private static readonly LocalizableString Description =
            "Every async method must accept a CancellationToken parameter and forward it to every " +
            "awaited call that accepts one. A CancellationToken must never be ignored or substituted " +
            "with CancellationToken.None inside an async call chain. See GlobalCodingStandards.md " +
            "Section 8.4.";

        private const string Category = "Coding";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: "https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalCodingStandards.md");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(compilationContext =>
            {
                INamedTypeSymbol? cancellationTokenSymbol =
                    compilationContext.Compilation.GetTypeByMetadataName(CodingWellKnownTypes.CancellationTokenMetadataName);

                if (cancellationTokenSymbol is null)
                {
                    return;
                }

                compilationContext.RegisterSyntaxNodeAction(
                    syntaxContext => AnalyzeMethod(syntaxContext, cancellationTokenSymbol),
                    SyntaxKind.MethodDeclaration);
            });
        }

        private static void AnalyzeMethod(SyntaxNodeAnalysisContext context, INamedTypeSymbol cancellationTokenSymbol)
        {
            var method = (MethodDeclarationSyntax)context.Node;

            if (!method.Modifiers.Any(SyntaxKind.AsyncKeyword) || method.Body is null)
            {
                return;
            }

            if (!ReturnsTask(method.ReturnType))
            {
                return;
            }

            ParameterSyntax? cancellationTokenParameter =
                FindCancellationTokenParameter(context, method, cancellationTokenSymbol);

            if (cancellationTokenParameter is null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    method.Identifier.GetLocation(),
                    method.Identifier.ValueText,
                    "must accept a CancellationToken parameter and forward it to awaited calls"));
                return;
            }

            foreach (MemberAccessExpressionSyntax memberAccess in
                method.Body.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
            {
                if (!IsCancellationTokenNone(memberAccess))
                {
                    continue;
                }

                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    memberAccess.GetLocation(),
                    method.Identifier.ValueText,
                    "must not substitute CancellationToken.None for its own CancellationToken parameter when calling other async methods"));
            }
        }

        private static bool IsCancellationTokenNone(MemberAccessExpressionSyntax memberAccess)
        {
            if (!string.Equals(memberAccess.Name.Identifier.ValueText, CancellationTokenNoneMemberName, System.StringComparison.Ordinal))
            {
                return false;
            }

            return memberAccess.Expression is IdentifierNameSyntax identifier &&
                   string.Equals(identifier.Identifier.ValueText, CancellationTokenTypeName, System.StringComparison.Ordinal);
        }

        private static bool ReturnsTask(TypeSyntax returnType)
        {
            string? typeName = returnType switch
            {
                IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                GenericNameSyntax generic => generic.Identifier.ValueText,
                QualifiedNameSyntax qualified => qualified.Right switch
                {
                    IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
                    GenericNameSyntax generic => generic.Identifier.ValueText,
                    _ => null,
                },
                _ => null,
            };

            return string.Equals(typeName, TaskTypeName, System.StringComparison.Ordinal) ||
                   string.Equals(typeName, ValueTaskTypeName, System.StringComparison.Ordinal);
        }

        private static ParameterSyntax? FindCancellationTokenParameter(
            SyntaxNodeAnalysisContext context,
            MethodDeclarationSyntax method,
            INamedTypeSymbol cancellationTokenSymbol)
        {
            foreach (ParameterSyntax parameter in method.ParameterList.Parameters)
            {
                if (parameter.Type is null)
                {
                    continue;
                }

                ITypeSymbol? parameterType = context.SemanticModel.GetTypeInfo(parameter.Type).Type;

                if (SymbolEqualityComparer.Default.Equals(parameterType, cancellationTokenSymbol))
                {
                    return parameter;
                }
            }

            return null;
        }
    }
}
