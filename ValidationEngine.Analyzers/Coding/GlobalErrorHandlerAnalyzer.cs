using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Coding
{
    /// <summary>
    /// Enforces GlobalCodingStandards.md coding.7: a global error handler must be registered as the
    /// outermost layer of the request pipeline. Flags Program.cs when the first middleware
    /// registration ("Use*" invocation) is not UseExceptionHandler, indicating the global error
    /// handler is missing entirely or is not the outermost pipeline layer.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class GlobalErrorHandlerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CODE016";

        private const string UsePrefix = "Use";

        private static readonly LocalizableString Title =
            "Global exception handler must be the outermost middleware";

        private static readonly LocalizableString MessageFormat =
            "'{0}' is registered as the first middleware in the pipeline; UseExceptionHandler must be registered as the outermost layer of the request pipeline, per GlobalCodingStandards.md coding.7";

        private static readonly LocalizableString Description =
            "Every application must have a global error handler registered as the outermost layer of the " +
            "request pipeline. See GlobalCodingStandards.md Section 7.";

        private const string Category = "Coding";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: "https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalCodingStandards.md",
            customTags: WellKnownDiagnosticTags.CompilationEnd);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(compilationContext =>
            {
                var middlewareCalls = new ConcurrentBag<(int Position, Location Location, string MethodName)>();

                compilationContext.RegisterSyntaxNodeAction(syntaxContext =>
                {
                    var invocation = (InvocationExpressionSyntax)syntaxContext.Node;
                    string fileName = Path.GetFileName(invocation.SyntaxTree.FilePath);

                    if (!CodingWellKnownTypes.IsProgramFile(fileName))
                    {
                        return;
                    }

                    if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                    {
                        return;
                    }

                    string methodName = memberAccess.Name.Identifier.ValueText;

                    if (!methodName.StartsWith(UsePrefix, System.StringComparison.Ordinal))
                    {
                        return;
                    }

                    middlewareCalls.Add((invocation.SpanStart, invocation.GetLocation(), methodName));
                }, SyntaxKind.InvocationExpression);

                compilationContext.RegisterCompilationEndAction(compilationEndContext =>
                {
                    if (middlewareCalls.IsEmpty)
                    {
                        return;
                    }

                    var ordered = new List<(int Position, Location Location, string MethodName)>(middlewareCalls);
                    ordered.Sort((first, second) => first.Position.CompareTo(second.Position));

                    (int _, Location location, string methodName) = ordered[0];

                    if (string.Equals(methodName, CodingWellKnownTypes.UseExceptionHandlerMethodName, System.StringComparison.Ordinal))
                    {
                        return;
                    }

                    var diagnostic = Diagnostic.Create(Rule, location, methodName);
                    compilationEndContext.ReportDiagnostic(diagnostic);
                });
            });
        }
    }
}
