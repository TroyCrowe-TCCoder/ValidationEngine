; Unshipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
CODE001 | Coding | Warning | ProgramCompositionOnlyAnalyzer, [GlobalCodingStandards.md](https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalCodingStandards.md) coding.2.1
CODE002 | Coding | Warning | ControllerDirectRepositoryAccessAnalyzer, [GlobalCodingStandards.md](https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalCodingStandards.md) coding.2.2
CODE003 | Coding | Warning | WrapperOnlyInterfaceAnalyzer, [GlobalCodingStandards.md](https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalCodingStandards.md) coding.2.5
CODE004 | Coding | Error | InlineAuthorizationPolicyAnalyzer, [GlobalCodingStandards.md](https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalCodingStandards.md) coding.2.6
CODE005 | Coding | Error | NoOrmUsageAnalyzer, [GlobalCodingStandards.md](https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalCodingStandards.md) coding.2.7
CODE006 | Coding | Warning | NonDescriptiveNamingAnalyzer, [GlobalCodingStandards.md](https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalCodingStandards.md) coding.3.2
CODE007 | Coding | Warning | ExcessiveMethodLengthAnalyzer, [GlobalCodingStandards.md](https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalCodingStandards.md) coding.3.3
CODE008 | Coding | Warning | DeferredGuardClauseAnalyzer, [GlobalCodingStandards.md](https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalCodingStandards.md) coding.3.4
CODE009 | Coding | Error | MutableDtoAnalyzer, [GlobalCodingStandards.md](https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalCodingStandards.md) coding.3.5
CODE010 | Coding | Warning | MappingOutsideConstructorAnalyzer, [GlobalCodingStandards.md](https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalCodingStandards.md) coding.3.6
CODE011 | Coding | Warning | NonMinimalVisibilityAnalyzer, [GlobalCodingStandards.md](https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalCodingStandards.md) coding.3.7
CODE012 | Coding | Warning | ScatteredExtensionMethodsAnalyzer, [GlobalCodingStandards.md](https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalCodingStandards.md) coding.3.13.1
CODE013 | Coding | Warning | RepeatedLiteralAnalyzer, [GlobalCodingStandards.md](https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalCodingStandards.md) coding.3.14
CODE014 | Coding | Warning | ConcreteDependencyAnalyzer, [GlobalCodingStandards.md](https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalCodingStandards.md) coding.4.4
CODE015 | Coding | Error | NoServiceLocatorAnalyzer, [GlobalCodingStandards.md](https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalCodingStandards.md) coding.4.5
CODE016 | Coding | Error | GlobalErrorHandlerAnalyzer, [GlobalCodingStandards.md](https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalCodingStandards.md) coding.7
CODE017 | Coding | Error | ImpreciseExceptionTypeAnalyzer, [GlobalCodingStandards.md](https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalCodingStandards.md) coding.7.1
CODE018 | Coding | Error | SwallowedExceptionAnalyzer, [GlobalCodingStandards.md](https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalCodingStandards.md) coding.7.3
CODE019 | Coding | Error | InlineErrorResponseAnalyzer, [GlobalCodingStandards.md](https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalCodingStandards.md) coding.7.4
CODE020 | Coding | Error | ExceptionDetailsExposedAnalyzer, [GlobalCodingStandards.md](https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalCodingStandards.md) coding.7.5, coding.7.7
CODE021 | Coding | Error | AsyncVoidAnalyzer, [GlobalCodingStandards.md](https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalCodingStandards.md) coding.8.3
CODE022 | Coding | Warning | CancellationTokenForwardingAnalyzer, [GlobalCodingStandards.md](https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalCodingStandards.md) coding.8.4
CODE023 | Coding | Error | ConfigureAwaitAnalyzer, [GlobalCodingStandards.md](https://dev.azure.com/tcrowe0170/GlobalStandards/_git/GlobalStandards?path=/Docs/Standards/GlobalCodingStandards.md) coding.8.5
