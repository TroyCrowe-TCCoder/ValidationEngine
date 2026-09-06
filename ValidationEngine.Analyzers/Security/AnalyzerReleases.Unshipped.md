; Unshipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
SEC001 | Security | Error | DirectKeyVaultAccessAnalyzer, [GlobalSecurityStandards.md](https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalSecurityStandards.md) security.2.3
SEC002 | Security | Warning | UndocumentedAllowAnonymousAnalyzer, [GlobalSecurityStandards.md](https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalSecurityStandards.md) security.3.1
SEC003 | Security | Error | WeakenedJwtBearerValidationAnalyzer, [GlobalSecurityStandards.md](https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalSecurityStandards.md) security.3.5
SEC004 | Security | Error | InMemoryTokenCacheAnalyzer, [GlobalSecurityStandards.md](https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalSecurityStandards.md) security.3.6
SEC005 | Security | Error | InlineSqlAnalyzer, [GlobalSecurityStandards.md](https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalSecurityStandards.md) security.4.1
SEC006 | Security | Error | UnsafeUriEscapingAnalyzer, [GlobalSecurityStandards.md](https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalSecurityStandards.md) security.5.4
SEC007 | Security | Warning | DirectHttpClientInstantiationAnalyzer, [GlobalSecurityStandards.md](https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalSecurityStandards.md) security.8.6
SEC008 | Security | Warning | LongRunningLoopMissingCancellationCheckAnalyzer, [GlobalSecurityStandards.md](https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalSecurityStandards.md) security.7.3
SEC009 | Security | Error | NoOpMalwareScannerRegistrationAnalyzer, [GlobalSecurityStandards.md](https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalSecurityStandards.md) security.5.3
SEC010 | Security | Error | FallbackPolicyAnalyzer, [GlobalSecurityStandards.md](https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalSecurityStandards.md) security.3.1
SEC011 | Security | Error | SecretConfigurationAnalyzer, [GlobalSecurityStandards.md](https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalSecurityStandards.md) security.2.3
SEC012 | Security | Error | SecretInAppSettingsAnalyzer, [GlobalSecurityStandards.md](https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalSecurityStandards.md) security.2.2
