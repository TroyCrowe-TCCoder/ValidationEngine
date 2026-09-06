; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
CACHE001 | Caching | Error | NoMemoryCacheAnalyzer, [GlobalCachingStandards.md caching.3](../../../GlobalStandards/Docs/Standards/GlobalCachingStandards.md)
CACHE002 | Caching | Error | RedisRegistrationOptionsAnalyzer, [GlobalCachingStandards.md caching.3.1](../../../GlobalStandards/Docs/Standards/GlobalCachingStandards.md)
CACHE003 | Caching | Error | NoDirectRedisClientAnalyzer, [GlobalCachingStandards.md caching.3.2](../../../GlobalStandards/Docs/Standards/GlobalCachingStandards.md)
CACHE004 | Caching | Error | CacheKeyMagicStringAnalyzer, [GlobalCachingStandards.md caching.4.2](../../../GlobalStandards/Docs/Standards/GlobalCachingStandards.md)
CACHE005 | Caching | Error | CacheKeyTenantIdSegmentAnalyzer, [GlobalCachingStandards.md caching.4.1 / caching.5.1](../../../GlobalStandards/Docs/Standards/GlobalCachingStandards.md)
CACHE006 | Caching | Error | CacheExpirationSourceAnalyzer, [GlobalCachingStandards.md caching.6.3](../../../GlobalStandards/Docs/Standards/GlobalCachingStandards.md)
CACHE007 | Caching | Error | CacheOptionsConfigurationAnalyzer, [GlobalCachingStandards.md caching.6.4](../../../GlobalStandards/Docs/Standards/GlobalCachingStandards.md)
CACHE008 | Caching | Error | NoNewtonsoftJsonForCacheAnalyzer, [GlobalCachingStandards.md caching.8.1](../../../GlobalStandards/Docs/Standards/GlobalCachingStandards.md)
CACHE009 | Caching | Error | NoWildcardKeyDeletionAnalyzer, [GlobalCachingStandards.md caching.7.2](../../../GlobalStandards/Docs/Standards/GlobalCachingStandards.md)
CACHE010 | Caching | Error | CacheInvalidationOnWriteAnalyzer, [GlobalCachingStandards.md caching.7.1](../../../GlobalStandards/Docs/Standards/GlobalCachingStandards.md)
CACHE011 | Caching | Warning | NoSensitiveDataInCacheAnalyzer, [GlobalCachingStandards.md caching.2.3 / caching.10.1](../../../GlobalStandards/Docs/Standards/GlobalCachingStandards.md)
CACHE012 | Caching | Warning | NoInlineJsonSerializerForCacheAnalyzer, [GlobalCachingStandards.md caching.8.2](../../../GlobalStandards/Docs/Standards/GlobalCachingStandards.md)
CACHE013 | Caching | Warning | CacheDeserializationFailureHandlingAnalyzer, [GlobalCachingStandards.md caching.8.3](../../../GlobalStandards/Docs/Standards/GlobalCachingStandards.md)
CACHE014 | Caching | Warning | CacheAsideResilienceAnalyzer, [GlobalCachingStandards.md caching.9.1](../../../GlobalStandards/Docs/Standards/GlobalCachingStandards.md)
CACHE015 | Caching | Warning | CacheObservabilityAnalyzer, [GlobalCachingStandards.md caching.9.2](../../../GlobalStandards/Docs/Standards/GlobalCachingStandards.md)
CACHE016 | Caching | Error | CacheLongLivedExpirationAnalyzer, [GlobalCachingStandards.md caching.6.2](../../../GlobalStandards/Docs/Standards/GlobalCachingStandards.md)
