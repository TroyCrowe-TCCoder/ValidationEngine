; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
LOG001 | Logging | Error | NoConsoleTraceDebugLoggingAnalyzer, [GlobalLoggingStandards.md logging.2.1](../../../GlobalStandards/Docs/Standards/GlobalLoggingStandards.md)
LOG002 | Logging | Error | NoProviderSpecificLoggingApiAnalyzer, [GlobalLoggingStandards.md logging.2.2](../../../GlobalStandards/Docs/Standards/GlobalLoggingStandards.md)
LOG003 | Logging | Warning | NoScatteredApplicationInsightsSdkAnalyzer, [GlobalLoggingStandards.md logging.2.3](../../../GlobalStandards/Docs/Standards/GlobalLoggingStandards.md)
LOG004 | Logging | Warning | ProgramLoggingDestinationAnalyzer, [GlobalLoggingStandards.md logging.2.4](../../../GlobalStandards/Docs/Standards/GlobalLoggingStandards.md)
LOG005 | Logging | Error | NoStringInterpolationInLogMessageAnalyzer, [GlobalLoggingStandards.md logging.2.6/3.1](../../../GlobalStandards/Docs/Standards/GlobalLoggingStandards.md)
LOG006 | Logging | Warning | ExceptionLogSeverityAnalyzer, [GlobalLoggingStandards.md logging.3.2](../../../GlobalStandards/Docs/Standards/GlobalLoggingStandards.md)
LOG007 | Logging | Warning | LogEntryMissingNamedPropertiesAnalyzer, [GlobalLoggingStandards.md logging.3.3](../../../GlobalStandards/Docs/Standards/GlobalLoggingStandards.md)
LOG008 | Logging | Warning | CorrelationIdNotAttachedAnalyzer, [GlobalLoggingStandards.md logging.4.1](../../../GlobalStandards/Docs/Standards/GlobalLoggingStandards.md)
LOG009 | Logging | Warning | CorrelationIdNotInLogScopeAnalyzer, [GlobalLoggingStandards.md logging.4.2](../../../GlobalStandards/Docs/Standards/GlobalLoggingStandards.md)
LOG010 | Logging | Warning | DirectHttpClientCallBypassesManagerAnalyzer, [GlobalLoggingStandards.md logging.4.3](../../../GlobalStandards/Docs/Standards/GlobalLoggingStandards.md)
LOG011 | Logging | Warning | MissingAuditLogEntryAnalyzer, [GlobalLoggingStandards.md logging.5.1](../../../GlobalStandards/Docs/Standards/GlobalLoggingStandards.md)
LOG012 | Logging | Warning | AuditEntryMissingRequiredFieldsAnalyzer, [GlobalLoggingStandards.md logging.5.2](../../../GlobalStandards/Docs/Standards/GlobalLoggingStandards.md)
LOG013 | Logging | Warning | DependencyCallMissingTelemetryAnalyzer, [GlobalLoggingStandards.md logging.6](../../../GlobalStandards/Docs/Standards/GlobalLoggingStandards.md)
LOG014 | Logging | Warning | MissingHealthCheckEndpointAnalyzer, [GlobalLoggingStandards.md logging.7](../../../GlobalStandards/Docs/Standards/GlobalLoggingStandards.md)
LOG015 | Logging | Warning | SensitiveDataInLogEntryAnalyzer, [GlobalLoggingStandards.md logging.8](../../../GlobalStandards/Docs/Standards/GlobalLoggingStandards.md)
LOG016 | Logging | Warning | LogEntryInsideLoopAnalyzer, [GlobalLoggingStandards.md logging.9](../../../GlobalStandards/Docs/Standards/GlobalLoggingStandards.md)
