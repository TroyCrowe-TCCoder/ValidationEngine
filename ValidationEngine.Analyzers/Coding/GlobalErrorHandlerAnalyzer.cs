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
    /// Enforces GlobalCodingStandards.md coding.7: a global error handler must be registered
    /// before any middleware that can produce a client-visible response for a request that later
    /// throws (routing, CORS, auth, static files, endpoint execution, etc. — see
    /// <see cref="CodingWellKnownTypes.MiddlewareRequiringPriorExceptionHandling"/>). Flags
    /// Program.cs when one of those well-known middleware registrations precedes
    /// UseExceptionHandler. Custom, non-error-handling <c>Use*</c>/<c>UseMiddleware&lt;T&gt;()</c>
    /// registrations (e.g. correlation-id or request-logging middleware) are consumer-specific
    /// pass-through concerns and are allowed ahead of the exception handler; this analyzer does
    /// not assume any particular consumer's error-handling architecture.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class GlobalErrorHandlerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CODE016";

        private const string UsePrefix = "Use";

        private static readonly LocalizableString Title =
            "Global exception handler must be registered before response-producing middleware";

        private static readonly LocalizableString MessageFormat =
            "'{0}' is registered before UseExceptionHandler; the global error handler must be registered before any middleware that can produce a client-visible response, per GlobalCodingStandards.md coding.7";

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

                    int exceptionHandlerPosition = ordered.FindIndex(call =>
                        string.Equals(call.MethodName, CodingWellKnownTypes.UseExceptionHandlerMethodName, System.StringComparison.Ordinal));

                    for (int i = 0; i < ordered.Count; i++)
                    {
                        if (exceptionHandlerPosition >= 0 && i >= exceptionHandlerPosition)
                        {
                            break;
                        }

                        (int _, Location location, string methodName) = ordered[i];

                        if (!IsResponseProducingMiddleware(methodName))
                        {
                            continue;
                        }

                        var diagnostic = Diagnostic.Create(Rule, location, methodName);
                        compilationEndContext.ReportDiagnostic(diagnostic);
                    }

                    if (exceptionHandlerPosition < 0)
                    {
                        // No UseExceptionHandler call at all: flag the first response-producing
                        // middleware found, or the first middleware overall if none matched.
                        foreach ((int _, Location location, string methodName) in ordered)
                        {
                            if (IsResponseProducingMiddleware(methodName))
                            {
                                compilationEndContext.ReportDiagnostic(Diagnostic.Create(Rule, location, methodName));
                                return;
                            }
                        }
                    }
                });
            });
        }

        private static bool IsResponseProducingMiddleware(string methodName)
        {
            foreach (string candidate in CodingWellKnownTypes.MiddlewareRequiringPriorExceptionHandling)
            {
                if (string.Equals(methodName, candidate, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
