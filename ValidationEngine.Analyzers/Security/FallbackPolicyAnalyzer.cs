using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ValidationEngine.Analyzers.Security
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class FallbackPolicyAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SEC010";

        private static readonly LocalizableString Title = "Global fallback authorization policy required";
        private static readonly LocalizableString MessageFormat = "Register a global fallback policy requiring authenticated users in Program.cs (SetFallbackPolicy/RequireAuthenticatedUser)";
        private static readonly LocalizableString Description = "A global fallback authorization policy that requires authenticated users must be registered at application startup (Program.cs). This prevents endpoints being accidentally left open without an explicit documented reason.";
        private const string Category = "Security";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: "https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalSecurityStandards.md",
            customTags: new[] { "CompilationEnd" });

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationAction(compilationContext =>
            {
                var programFiles = compilationContext.Compilation.SyntaxTrees
                    .Where(st => !string.IsNullOrEmpty(st.FilePath) && SecurityWellKnownTypes.IsProgramFile(st.FilePath))
                    .ToList();

                if (programFiles.Count == 0)
                {
                    // No Program.cs in compilation; nothing to check here
                    return;
                }

                bool foundFallback = false;

                foreach (var tree in programFiles)
                {
                    var root = tree.GetRoot(compilationContext.CancellationToken);
                    var invocations = root.DescendantNodes().OfType<InvocationExpressionSyntax>();

                    foreach (var invocation in invocations)
                    {
                        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                        {
                            var name = memberAccess.Name.Identifier.ValueText;
                            if (string.Equals(name, "SetFallbackPolicy", System.StringComparison.Ordinal))
                            {
                                foundFallback = true;
                                break;
                            }
                        }
                    }

                    if (foundFallback)
                        break;

                    // also check for assignment to options.FallbackPolicy = ...
                    var assignments = root.DescendantNodes().OfType<AssignmentExpressionSyntax>();
                    foreach (var assignment in assignments)
                    {
                        if (assignment.Left is MemberAccessExpressionSyntax leftMember &&
                            leftMember.Name.Identifier.ValueText == "FallbackPolicy")
                        {
                            foundFallback = true;
                            break;
                        }
                    }

                    if (foundFallback)
                        break;
                }

                if (!foundFallback)
                {
                    // Report diagnostic at the first Program.cs file root location
                    var targetTree = programFiles.First();
                    var location = targetTree.GetRoot().GetLocation();
                    compilationContext.ReportDiagnostic(Diagnostic.Create(Rule, location));
                }
            });
        }
    }
}
