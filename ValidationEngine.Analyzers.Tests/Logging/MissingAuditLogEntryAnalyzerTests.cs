using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Logging.Tests
{
    public class MissingAuditLogEntryAnalyzerTests
    {
        private static CSharpAnalyzerTest<MissingAuditLogEntryAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            var test = new CSharpAnalyzerTest<MissingAuditLogEntryAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            return test;
        }

        [Fact]
        public async Task WhenCreateMethodNeverRecordsAuditEntryThenLog011IsReported()
        {
            const string source = @"
namespace TestApp
{
    public class DocumentService
    {
        public void {|#0:CreateDocument|}(string name)
        {
            // saves the document
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(MissingAuditLogEntryAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("CreateDocument"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenCreateMethodRecordsAuditEntryThenNoDiagnosticIsReported()
        {
            const string source = @"
namespace TestApp
{
    public interface IAuditLogger
    {
        void Record(string what, string outcome, string entityType, int entityId, string who, System.DateTime when, string correlationId);
    }

    public class DocumentService
    {
        private readonly IAuditLogger _auditLogger;

        public DocumentService(IAuditLogger auditLogger)
        {
            _auditLogger = auditLogger;
        }

        public void CreateDocument(string name)
        {
            _auditLogger.Record(""DocumentCreated"", ""Success"", ""Document"", 1, ""user"", System.DateTime.UtcNow, ""corr-1"");
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
