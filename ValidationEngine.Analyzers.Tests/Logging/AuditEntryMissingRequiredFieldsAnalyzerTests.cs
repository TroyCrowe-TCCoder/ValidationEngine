using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ValidationEngine.Analyzers.Logging.Tests
{
    public class AuditEntryMissingRequiredFieldsAnalyzerTests
    {
        private static CSharpAnalyzerTest<AuditEntryMissingRequiredFieldsAnalyzer, DefaultVerifier> CreateTest(string source)
        {
            var test = new CSharpAnalyzerTest<AuditEntryMissingRequiredFieldsAnalyzer, DefaultVerifier>
            {
                TestCode = source,
                ReferenceAssemblies = ReferenceAssemblies.Net.Net90,
            };

            return test;
        }

        [Fact]
        public async Task WhenAuditRecordCallIsMissingOutcomeThenLog012IsReported()
        {
            const string source = @"
using System;

namespace TestApp
{
    public interface IAuditLogger
    {
        void Record(string what, string outcome = ""Success"", string entityType = """", int entityId = 0, string who = """", DateTime when = default, string correlationId = """");
    }

    public class DocumentService
    {
        private readonly IAuditLogger _auditLogger;

        public DocumentService(IAuditLogger auditLogger)
        {
            _auditLogger = auditLogger;
        }

        public void CreateDocument()
        {
            _auditLogger.{|#0:Record|}(
                what: ""DocumentCreated"",
                entityType: ""Document"",
                entityId: 1,
                who: ""user"",
                when: DateTime.UtcNow,
                correlationId: ""corr-1"");
        }
    }
}
";

            var test = CreateTest(source);
            test.ExpectedDiagnostics.Add(
                new DiagnosticResult(AuditEntryMissingRequiredFieldsAnalyzer.Rule)
                    .WithLocation(0)
                    .WithArguments("CreateDocument", "outcome"));

            await test.RunAsync();
        }

        [Fact]
        public async Task WhenAuditRecordCallIncludesAllRequiredFieldsThenNoDiagnosticIsReported()
        {
            const string source = @"
using System;

namespace TestApp
{
    public interface IAuditLogger
    {
        void Record(string what, string outcome, string entityType, int entityId, string who, DateTime when, string correlationId);
    }

    public class DocumentService
    {
        private readonly IAuditLogger _auditLogger;

        public DocumentService(IAuditLogger auditLogger)
        {
            _auditLogger = auditLogger;
        }

        public void CreateDocument()
        {
            _auditLogger.Record(
                what: ""DocumentCreated"",
                outcome: ""Success"",
                entityType: ""Document"",
                entityId: 1,
                who: ""user"",
                when: DateTime.UtcNow,
                correlationId: ""corr-1"");
        }
    }
}
";

            var test = CreateTest(source);

            await test.RunAsync();
        }
    }
}
