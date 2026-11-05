# Caching Standards

**Version:** 3.0.0
**Status:** Active
**Applies To:** All repositories under the GlobalStandards governance model
**Audience:** AI models and human developers
**Created:** 2025-01-01
**Last Modified:** 2026-11-01

---
<!-- STD-MARKER: caching.file -->


## 1. Purpose
<!-- STD-MARKER: caching.1 -->

This standard defines the caching rules that must be applied across all repositories. Both human developers and AI models must apply every rule in this file when writing, reviewing, or modifying any code that reads from or writes to a cache. Caching is a performance requirement — the decision of what to cache and where is not optional for the data categories defined in this file. Compliance is verified during pull request review and the pre-merge compliance process.

This file owns the caching rules. The requirement to cache is stated in [`GlobalPerformanceStandards.md`](../standards/GlobalPerformanceStandards.md#36-caching). Security constraints on cached data (tenant isolation, sensitive data prohibition) are enforced here and cross-referenced to [`GlobalSecurityStandards.md`](../standards/GlobalSecurityStandards.md).

All applications are designed and built with multi-tenancy as a first-class requirement from the start, regardless of whether the application currently serves a single tenant. This principle applies directly to the cache layer — every cache key structure, every isolation rule, and every invalidation pattern in this standard is written to be correct in a multi-tenant deployment. No caching implementation may be designed in a way that would require structural rework when multiple tenants are introduced.

---

## 2. What Must Be Cached
<!-- STD-MARKER: caching.2 -->

Both human developers and AI models must evaluate every data access operation against the categories below before a work slice is closed. Any operation that falls into a required caching category and does not use a cache is a violation that must be corrected before the PR is opened.

### 2.1 Qualifying Criteria
<!-- STD-MARKER: caching.2.1 -->

A data access operation qualifies for caching when it meets one or more of the following criteria:

- The same data is loaded repeatedly within a session or across requests and the data rarely or never changes between writes
- The data is loaded on every authenticated request and is expensive to retrieve or compute
- The data is loaded repeatedly during a batch processing run against a fixed data set for that run
- The data populates a UI selection list and changes only when an administrator explicitly modifies it

A data access operation that retrieves a small, simple result — fewer than 50 rows from a single indexed table with no joins — does not qualify for caching on frequency alone. The retrieval cost must justify the cache management overhead.

### 2.2 Required Caching Categories
<!-- STD-MARKER: caching.2.2 -->

The following data categories meet the qualifying criteria and must be served from cache. A database round-trip on every request for this data is not permitted.

| Category | Examples | Rationale |
|---|---|---|
| Client data | Client profiles, client configuration, client-specific settings | Referenced by all application components on every client-scoped operation; changes only on explicit admin update |
| User profiles | Authenticated user data loaded on every request | Loaded on every authenticated request; expensive to rebuild from multiple sources |
| Dropdown and reference data | Item types, carrier lists, category hierarchies, country lists, any data populating a UI selection list | Loaded repeatedly per session; changes only when an administrator explicitly modifies the data |
| Client billing thresholds | Threshold records for a client loaded at billing run start | Checked against every invoice line during processing; loaded once per run, not per invoice line |
| Billing run reference data | Carrier mappings, GL code mappings, client billing rules loaded at run start | Referenced repeatedly across every record in a billing file; must not be fetched per record |

### 2.3 What Must Not Be Cached
<!-- STD-MARKER: caching.2.3 -->

The following data categories must never be cached.

| Category | Reason |
|---|---|
| Sensitive personal data (PII) | Privacy and compliance risk; must always reflect current state |
| Authentication tokens or credentials | Security violation — must always be fetched live |
| Financial transaction records | Must always reflect current state; caching introduces stale-data risk on financial data |
| Any data that changes on every request | Caching provides no benefit and adds stale-data risk |
| Any tenant-scoped cache entry missing a TenantId key segment | Cross-tenant data leakage risk — tenant-scoped data must carry TenantId in the key; see Section 5 |

---

## 3. Cache Provider
<!-- STD-MARKER: caching.3 -->

Both human developers and AI models must use Azure Cache for Redis as the cache provider for all applications, accessed exclusively through the shared `TCCoder.RedisCacheManager` NuGet package (`ICacheManager`). In-process memory cache (`IMemoryCache`) must not be used, and `IDistributedCache` must not be used. Redis is mandated for the following reasons:

- **Memory isolation** — cache memory is separate from application process memory; large cached data sets do not compete with application and batch processing memory
- **Multi-instance correctness** — all application instances share a single cache; no instance holds a stale or inconsistent local copy
- **Restart resilience** — the cache survives application deployments and restarts; entries do not need to be rebuilt from the database on every restart
- **Multi-tenancy readiness** — a shared external cache with key-based tenant partitioning is the correct foundation for a multi-tenant architecture

`TCCoder.RedisCacheManager` is the single implementation of cache access for the application family. It owns the Redis connection, transient-failure retry, key construction, JSON serialization, failure handling, and cache telemetry, so that every application behaves the same way and none re-implements these concerns. The package is consumed as a `PackageReference` per [`GlobalNuGetLibraryStandards.md`](GlobalNuGetLibraryStandards.md).

No other cache provider or cache abstraction may be introduced without an approved deviation documented in the repository-local addendum.

### 3.1 Registration
<!-- STD-MARKER: caching.3.1 -->

```csharp
// Required — registration in Program.cs
// Registers ICacheManager, the Redis connection, the key builder, the distributed lock helper, and the Redis health check
builder.Services.AddRedisCacheManager(builder.Configuration);
```

The package binds its options from the `RedisCacheManager` configuration section. The Redis access key must be resolved from Azure Key Vault at startup — it must never be stored in configuration or committed to source control. The package supplies connection retry with exponential backoff and does not fail application startup when Redis is temporarily unavailable, so consuming applications must not add their own Redis connection or retry configuration.

```json
{
  "RedisCacheManager": {
    "HostName": "myapp-cache.redis.cache.windows.net",
    "Port": 6380,
    "KeyVaultUri": "https://myvault.vault.azure.net/",
    "AccessKeySecretName": "redis-access-key",
    "InstanceName": "MyApp",
    "UseSsl": true
  }
}
```

`InstanceName` is the first segment of every key the package writes. Each application must use its own `InstanceName` unless it intentionally shares cache entries with another application (for example, a cache written by one API and read by consumer APIs), in which case the shared `InstanceName` must be documented in the repository-local addendum.

### 3.2 Injection
<!-- STD-MARKER: caching.3.2 -->

All services that read from or write to the cache must inject `ICacheManager`. Direct use of the StackExchange.Redis client, `IDistributedCache`, or `IMemoryCache` inside application services is not permitted — all cache access must go through `ICacheManager`.

`ICacheManager` exposes three operations. Every call must pass the caller's `CancellationToken`.

| Operation | Behavior |
|---|---|
| `TryGetAsync<T>(category, id, cancellationToken)` | Returns `(Found, Value)`. A missing entry, a Redis failure, and a deserialization failure all return `Found = false` — the call never throws |
| `SetAsync<T>(category, id, value, ttl, cancellationToken)` | Writes the value as JSON. Applies `ttl` when supplied, otherwise the configured `DefaultTtl`. Never throws; a failed write is logged and the value is simply not cached |
| `RemoveAsync(category, id, cancellationToken)` | Deletes the entry. Never throws; a failed delete is logged |

```csharp
public class ReferenceDataService : IReferenceDataService
{
    private readonly ICacheManager _cache;
    private readonly IOptionsMonitor<CacheOptions> _cacheOptions;

    public ReferenceDataService(ICacheManager cache, IOptionsMonitor<CacheOptions> cacheOptions)
    {
        _cache = cache;
        _cacheOptions = cacheOptions;
    }
}
```

---

## 4. Cache Key Naming
<!-- STD-MARKER: caching.4 -->

Both human developers and AI models must follow the key naming rules in this section for every cache entry written. A cache entry with a non-conforming key must be corrected before the PR is approved.

### 4.1 Key Structure
<!-- STD-MARKER: caching.4.1 -->

`ICacheManager` builds every Redis key as `{InstanceName}:{category}:{id}`. `InstanceName` comes from configuration (Section 3.1); the application supplies the `category` and the `id` on every call. Both values must come from the `CacheKeys` class (Section 4.2).

- **category** — the type of data being cached, a stable constant in PascalCase (e.g., `ItemTypes`, `ClientThresholds`, `UserProfile`). A category must name one data type and one cached shape only.
- **id** — identifies the specific entry within the category. Its segments narrow from left to right based on the scope of the data, separated by `:`.

The `id` hierarchy depends on whether the data is tenant-scoped or tenant-independent (Section 5):

```
Tenant-scoped data:
{TenantId}:{ClientId}
{TenantId}:{ClientId}:{ParentId}
{TenantId}:{UserId}
{TenantId}

Tenant-independent data (Section 5.3):
{UserId}
{ReferenceId}
{fixed identifier for a single shared collection}
```

- **TenantId** — required and always the first segment of the `id` for tenant-scoped data; never present for tenant-independent data
- **ClientId** — required when the data belongs to a specific client; omitted when the data is not client-specific
- **UserId** — required when the data belongs to a specific user; omitted otherwise
- **ParentId** — required when a child collection belongs to a specific parent record

```csharp
// Required — all categories and ids defined in a dedicated CacheKeys static class
public static class CacheKeys
{
    // Categories — one constant per cached data type
    public const string ItemTypesCategory = "ItemTypes";
    public const string ClientConfigCategory = "ClientConfig";
    public const string ClientThresholdsCategory = "ClientThresholds";
    public const string UserProfileCategory = "UserProfile";
    public const string ItemTypeDescriptionsCategory = "ItemTypeDescriptions";
    public const string CarrierMappingsCategory = "CarrierMappings";
    public const string GlCodeMappingsCategory = "GlCodeMappings";
    public const string ClientBillingRulesCategory = "ClientBillingRules";
    public const string OrganizationMapCategory = "OrganizationMap";

    // Tenant-scoped ids — TenantId is always the first segment
    public static string ItemTypes(int tenantId)
        => $"{tenantId}";

    public static string ClientConfig(int tenantId, int clientId)
        => $"{tenantId}:{clientId}";

    public static string ClientThresholds(int tenantId, int clientId)
        => $"{tenantId}:{clientId}";

    public static string UserProfile(int tenantId, int userId)
        => $"{tenantId}:{userId}";

    // Child collection scoped to a parent record within a client
    public static string ItemTypeDescriptions(int tenantId, int clientId, int itemTypeId)
        => $"{tenantId}:{clientId}:{itemTypeId}";

    // Billing run reference data — client-scoped, held for the duration of the run
    public static string CarrierMappings(int tenantId, int clientId)
        => $"{tenantId}:{clientId}";

    public static string GlCodeMappings(int tenantId, int clientId)
        => $"{tenantId}:{clientId}";

    public static string ClientBillingRules(int tenantId, int clientId)
        => $"{tenantId}:{clientId}";

    // Tenant-independent id (Section 5.3) — the organization map is not aligned to any tenant;
    // it is the same list for every caller and is used to choose an organizational alignment during user onboarding
    public const string OrganizationMapId = "map";
}
```

### 4.2 Key Constant Rules
<!-- STD-MARKER: caching.4.2 -->

- All cache categories and ids must be defined as constants or static members in a dedicated `CacheKeys` static class.
- Magic string categories or ids scattered inline throughout the codebase are not permitted.
- The `CacheKeys` class must be the single source of truth for all category and id definitions in the application.

---

## 5. Tenant Isolation
<!-- STD-MARKER: caching.5 -->

Both human developers and AI models must classify every cache entry as tenant-scoped or tenant-independent and verify that the key matches its classification before the work slice is closed. A tenant-scoped entry whose `id` does not begin with TenantId is a violation regardless of whether the data appears sensitive. A cache entry that can return one tenant's data to a different tenant is a critical security violation and must be treated as a blocking defect.

### 5.1 Tenant-Scoped Data Requires TenantId
<!-- STD-MARKER: caching.5.1 -->

Data that belongs to a tenant — including data owned by a client, user, or other record within a tenant — must carry TenantId as the first segment of the `id`. The application may not yet serve more than one tenant, but this structure is enforced now so that the cache layer is correct when multi-tenancy is introduced. Retrofitting cache keys across an established codebase is costly and error-prone.

Tenant key partitioning is a security requirement, not only a data-correctness requirement. A cache entry for tenant-scoped data that does not include the tenant identifier in the `id` must be treated as a security violation and resolved before the PR is approved.

```csharp
// Required — TenantId is the first segment of the id, even in a single-tenant application
var (found, thresholds) = await _cache.TryGetAsync<List<Threshold>>(
    CacheKeys.ClientThresholdsCategory,
    CacheKeys.ClientThresholds(tenantId, clientId),
    cancellationToken);
```

### 5.2 Tenant Isolation Verification
<!-- STD-MARKER: caching.5.2 -->

Before a PR is opened, both human developers and AI models must confirm that every cache entry in new or changed code is classified as tenant-scoped or tenant-independent, that every tenant-scoped `id` begins with TenantId, and that no cache read path can return data belonging to a different tenant than the one making the request. Any tenant-scoped `id` that does not begin with TenantId must be corrected before the PR is approved.

### 5.3 Tenant-Independent Data
<!-- STD-MARKER: caching.5.3 -->

Data that is not aligned to any tenant is tenant-independent and must not carry a TenantId. For example, an organization map used to choose an organizational alignment during user onboarding exists before the user belongs to a tenant, and is the same list for every caller. Forcing a TenantId onto such data would create duplicate copies of identical content, and would leave a caller with no tenant unable to construct the key.

An entry may be classified as tenant-independent only when all of the following are true:

- The entry contains no tenant-owned data, and no value that varies by tenant
- Every caller, regardless of tenant, is entitled to receive the identical content
- The classification is recorded in the `CacheKeys` class, with a comment stating why the data is tenant-independent

When there is any doubt, the data must be treated as tenant-scoped. A key that is built without TenantId must never be used to store a per-tenant variant of the data.

---

## 6. Cache Lifecycle
<!-- STD-MARKER: caching.6 -->

Both human developers and AI models must assign every cache entry to one of the two lifecycle patterns defined in this section and implement the correct release strategy for that pattern. A cache entry with no defined release strategy is not permitted.

### 6.1 Lifecycle Patterns
<!-- STD-MARKER: caching.6.1 -->

| Pattern | Data Type | Load | Release Strategy |
|---|---|---|---|
| **Long-lived** | Stable reference data, dropdown lists, data that changes only on explicit admin write | On first request or on application startup | Explicit invalidation on write, plus a configured long expiration as a backstop |
| **Time-bounded** | User profiles, session-scoped data, billing run data | On demand | Configured expiration; may also be invalidated on explicit write |

**Long-lived pattern** is the correct choice when the data changes only when an administrator explicitly modifies it. A short expiration on this data causes unnecessary database round-trips and a staleness window that serves no purpose. Invalidation on write is the primary release mechanism — the cache entry lives until the data changes.

`ICacheManager` always applies an expiration: the `ttl` the caller supplies, or the configured `DefaultTtl` when none is supplied. An entry with no expiration cannot be created, and `RemoveAsync` does not throw when a delete fails. The long-lived pattern therefore passes an explicit long expiration from configuration, which bounds how long an entry can stay stale if an invalidation is missed. The long expiration is a safety net, not the release strategy.

Every `SetAsync` call must pass `ttl` explicitly from configuration. Relying on the package's `DefaultTtl` is not permitted for either pattern.

**Time-bounded pattern** is the correct choice when the data has a natural staleness window — user profiles that may be updated externally, session-scoped data tied to a user's activity, or billing run data that is only valid for the duration of the run.

### 6.2 Long-Lived Cache Implementation
<!-- STD-MARKER: caching.6.2 -->

Any method implementing the long-lived pattern must be decorated with `[LongLivedCache]` so the rule can be verified mechanically. The attribute marks the code boundary where "this entry is long-lived" is an asserted fact rather than an inferred judgment call; it does not replace the design-time decision in Section 6.1, it records the outcome of that decision. A method marked `[LongLivedCache]` must pass the configured long-lived expiration (`CacheOptions.LongLivedExpirationMinutes`) as the `ttl` of every `SetAsync` call. It must not omit `ttl` (which would apply `DefaultTtl`), pass a hardcoded value, or pass a time-bounded expiration value.

```csharp
// Required — long expiration from configuration is the backstop; the entry is released by invalidation on write
[LongLivedCache]
public async Task<IReadOnlyList<ItemType>> GetItemTypesAsync(int tenantId, CancellationToken cancellationToken)
{
    var id = CacheKeys.ItemTypes(tenantId);

    var (found, cached) = await _cache.TryGetAsync<List<ItemType>>(CacheKeys.ItemTypesCategory, id, cancellationToken);
    if (found && cached is not null)
        return cached;

    var data = (await _repository.GetItemTypesAsync(tenantId, cancellationToken)).ToList();

    await _cache.SetAsync(
        CacheKeys.ItemTypesCategory,
        id,
        data,
        TimeSpan.FromMinutes(_cacheOptions.CurrentValue.LongLivedExpirationMinutes),
        cancellationToken);

    return data;
}
```

### 6.3 Time-Bounded Cache Implementation
<!-- STD-MARKER: caching.6.3 -->

All expiration values must be defined in application configuration and accessed via `IOptionsMonitor<CacheOptions>`. `IOptionsMonitor<T>` must be used — not `IOptions<T>`. `IOptions<T>` is a startup snapshot and does not pick up configuration changes without an application restart. `IOptionsMonitor<T>` re-evaluates `CurrentValue` on every access, picking up changes immediately.

Hardcoded expiration values are not permitted.

```csharp
// Required — expiration sourced from configuration via IOptionsMonitor, not hardcoded
public async Task<UserProfile> GetUserProfileAsync(int tenantId, int userId, CancellationToken cancellationToken)
{
    var id = CacheKeys.UserProfile(tenantId, userId);

    var (found, cached) = await _cache.TryGetAsync<UserProfile>(CacheKeys.UserProfileCategory, id, cancellationToken);
    if (found && cached is not null)
        return cached;

    var data = await _repository.GetUserProfileAsync(userId, cancellationToken);

    await _cache.SetAsync(
        CacheKeys.UserProfileCategory,
        id,
        data,
        TimeSpan.FromMinutes(_cacheOptions.CurrentValue.UserProfileExpirationMinutes),
        cancellationToken);

    return data;
}
```

### 6.4 Expiration Configuration
<!-- STD-MARKER: caching.6.4 -->

All cache expiration values must be defined in application configuration under a `Cache` section and bound to a strongly typed options class. No default values may be assigned in the class — all values must come from configuration.

```csharp
// Required — strongly typed options class; no default values assigned
public class CacheOptions
{
    public int UserProfileExpirationMinutes { get; init; }
    public int BillingRunExpirationMinutes { get; init; }
    public int LongLivedExpirationMinutes { get; init; }
}

// Required — registration in Program.cs
builder.Services.Configure<CacheOptions>(builder.Configuration.GetSection("Cache"));
```

The `appsettings.json` section below is the single source of truth for all expiration values:

```json
{
  "Cache": {
    "UserProfileExpirationMinutes": 30,
    "BillingRunExpirationMinutes": 120,
    "LongLivedExpirationMinutes": 1440
  }
}
```

---

## 7. Invalidation and Pre-Loading
<!-- STD-MARKER: caching.7 -->

Both human developers and AI models must implement explicit cache invalidation for every cache entry that corresponds to data that can be modified through the application. A cache entry that has no defined release strategy is a violation.

### 7.1 Invalidation on Write
<!-- STD-MARKER: caching.7.1 -->

Invalidation must use `RemoveAsync` from `ICacheManager` and must always pass a `CancellationToken`. Invalidation must occur in the same service method as the write — it must not be deferred to a background process unless the staleness window is explicitly documented and approved.

```csharp
// Required — invalidate immediately after the write succeeds
public async Task UpdateClientConfigAsync(int tenantId, int clientId, ConfigModel config, CancellationToken cancellationToken)
{
    await _repository.UpdateClientConfigAsync(config, cancellationToken);

    await _cache.RemoveAsync(CacheKeys.ClientConfigCategory, CacheKeys.ClientConfig(tenantId, clientId), cancellationToken);
}
```

### 7.2 Group Invalidation
<!-- STD-MARKER: caching.7.2 -->

When a write operation invalidates a group of related cache entries, all affected keys must be explicitly invalidated using known identifiers from the `CacheKeys` class. Wildcard or pattern-based key deletion (e.g., Redis `KEYS` with a wildcard) must not be used in production code — it is a blocking operation that degrades Redis performance under load.

```csharp
// Required — explicit invalidation of all affected keys; no wildcard deletion
public async Task UpdateItemTypeAsync(int tenantId, int clientId, int itemTypeId, ItemType itemType, CancellationToken cancellationToken)
{
    await _repository.UpdateItemTypeAsync(itemType, cancellationToken);

    await _cache.RemoveAsync(CacheKeys.ItemTypesCategory, CacheKeys.ItemTypes(tenantId), cancellationToken);
    await _cache.RemoveAsync(CacheKeys.ItemTypeDescriptionsCategory, CacheKeys.ItemTypeDescriptions(tenantId, clientId, itemTypeId), cancellationToken);
}
```

### 7.3 Billing Run Pre-Loading
<!-- STD-MARKER: caching.7.3 -->

For billing run processing, required reference data must be pre-loaded into cache at the start of the run before any invoice processing begins. Pre-loading prevents repeated cache-miss database round-trips during processing and ensures consistent data for the full duration of the run. Data must not be fetched per invoice record.

The required pre-load sequence at billing run start:
1. Load client thresholds for the client being processed
2. Load carrier mappings, GL code mappings, and client billing rules
3. Confirm all required entries are in cache before processing begins
4. On run completion, explicitly remove all run-scoped cache entries

Because `SetAsync` never throws, a failed pre-load write is not reported to the caller. Pre-loading is a performance measure only — processing must still fall back to the data source on a cache miss.

```csharp
// Required — pre-load all billing run reference data before processing begins
// All four data types must be loaded; processing must not start until this method completes
public async Task PreloadBillingRunCacheAsync(int tenantId, int clientId, CancellationToken cancellationToken)
{
    var expiry = TimeSpan.FromMinutes(_cacheOptions.CurrentValue.BillingRunExpirationMinutes);

    var thresholds = (await _repository.GetClientThresholdsAsync(clientId, cancellationToken)).ToList();
    await _cache.SetAsync(CacheKeys.ClientThresholdsCategory, CacheKeys.ClientThresholds(tenantId, clientId),
        thresholds, expiry, cancellationToken);

    var carrierMappings = (await _repository.GetCarrierMappingsAsync(clientId, cancellationToken)).ToList();
    await _cache.SetAsync(CacheKeys.CarrierMappingsCategory, CacheKeys.CarrierMappings(tenantId, clientId),
        carrierMappings, expiry, cancellationToken);

    var glCodes = (await _repository.GetGlCodeMappingsAsync(clientId, cancellationToken)).ToList();
    await _cache.SetAsync(CacheKeys.GlCodeMappingsCategory, CacheKeys.GlCodeMappings(tenantId, clientId),
        glCodes, expiry, cancellationToken);

    var billingRules = (await _repository.GetClientBillingRulesAsync(clientId, cancellationToken)).ToList();
    await _cache.SetAsync(CacheKeys.ClientBillingRulesCategory, CacheKeys.ClientBillingRules(tenantId, clientId),
        billingRules, expiry, cancellationToken);

    _logger.LogInformation(
        "Billing run cache pre-loaded for TenantId {TenantId} ClientId {ClientId}. " +
        "Thresholds: {ThresholdCount}, CarrierMappings: {CarrierCount}, GlCodes: {GlCount}, BillingRules: {RuleCount}",
        tenantId, clientId,
        thresholds.Count, carrierMappings.Count, glCodes.Count, billingRules.Count);
}

// Required — explicitly release all run-scoped cache entries on run completion
public async Task ClearBillingRunCacheAsync(int tenantId, int clientId, CancellationToken cancellationToken)
{
    await _cache.RemoveAsync(CacheKeys.ClientThresholdsCategory, CacheKeys.ClientThresholds(tenantId, clientId), cancellationToken);
    await _cache.RemoveAsync(CacheKeys.CarrierMappingsCategory, CacheKeys.CarrierMappings(tenantId, clientId), cancellationToken);
    await _cache.RemoveAsync(CacheKeys.GlCodeMappingsCategory, CacheKeys.GlCodeMappings(tenantId, clientId), cancellationToken);
    await _cache.RemoveAsync(CacheKeys.ClientBillingRulesCategory, CacheKeys.ClientBillingRules(tenantId, clientId), cancellationToken);
}
```

---

## 8. Serialization
<!-- STD-MARKER: caching.8 -->

Both human developers and AI models must understand how `ICacheManager` serializes cache entries. Serialization is owned by `TCCoder.RedisCacheManager`; applications pass and receive typed objects and never handle bytes or JSON directly.

### 8.1 Approved Format
<!-- STD-MARKER: caching.8.1 -->

**JSON via `System.Text.Json`** is the serialization format for all cache entries, and it is applied internally by `RedisCacheManager`. Applications must not serialize values themselves before calling `SetAsync`, and must not use `Newtonsoft.Json`, `MessagePack` or any other format for cache values.

Cached types must be serializable by `System.Text.Json` with default options (public properties, a usable constructor or parameterless constructor, no reliance on custom converters that are not registered globally). Interfaces and abstract types must not be cached — use a concrete type such as `List<T>` or a record.

### 8.2 No Serialization Helpers
<!-- STD-MARKER: caching.8.2 -->

Applications must not define their own cache serialization helper, call `JsonSerializer` for cache values, or convert values to `byte[]` for the cache. `ICacheManager` accepts and returns typed values:

```csharp
// Required — typed values in and out; no JsonSerializer, no byte[]
await _cache.SetAsync(CacheKeys.ItemTypesCategory, id, data, ttl, cancellationToken);
var (found, value) = await _cache.TryGetAsync<List<ItemType>>(CacheKeys.ItemTypesCategory, id, cancellationToken);
```

### 8.3 Deserialization Failure Handling
<!-- STD-MARKER: caching.8.3 -->

Deserialization can fail when a cached entry was written by a prior application version with a different object shape — for example after a deploy that changes a cached model. `RedisCacheManager` catches the failure, logs a warning, and returns a miss from `TryGetAsync`. It does not throw to the caller.

The stale entry is not evicted on a failed read. The cache-aside flow in Section 9.1 repopulates the key on the resulting miss, which overwrites the stale entry. Application code must therefore always write the value back after a miss and must not skip the write for any reason other than the data source returning no data.

When a deploy changes the shape of a cached model, the `category` constant for that model should be given a new name (for example `UserProfileV2`) if the old entries must not be read at all.

---

## 9. Resilience and Observability
<!-- STD-MARKER: caching.9 -->

Both human developers and AI models must ensure that cache failures do not cause application failures. The cache is a performance layer — its unavailability must degrade performance, not break functionality.

### 9.1 Cache-Aside Pattern
<!-- STD-MARKER: caching.9.1 -->

The cache-aside pattern must be used for all cache reads — both long-lived and time-bounded. The application is responsible for loading data into cache on a miss and for keeping cache entries consistent with the backing store. The cache is never the system of record.

The required read flow is:
1. Attempt to read from cache.
2. On cache hit — deserialize and return cached value.
3. On cache miss — read from the data source, write to cache, return value.
4. On cache error — `TryGetAsync` returns a miss, so the flow continues as a cache miss. `ICacheManager` never throws and logs the failure itself.

`RedisCacheManager` retries transient Redis failures and converts any remaining failure into a miss (reads) or a no-op (writes and removals). Application code therefore does not wrap `ICacheManager` calls in `try`/`catch`. A `catch` block around a cache call hides nothing the package has not already handled and must not be added.

```csharp
// Required — cache-aside with ICacheManager; a cache failure is indistinguishable from a miss
public async Task<UserProfile> GetUserProfileAsync(int tenantId, int userId, CancellationToken cancellationToken)
{
    var id = CacheKeys.UserProfile(tenantId, userId);

    var (found, cached) = await _cache.TryGetAsync<UserProfile>(CacheKeys.UserProfileCategory, id, cancellationToken);
    if (found && cached is not null)
        return cached;

    var data = await _repository.GetUserProfileAsync(userId, cancellationToken);

    await _cache.SetAsync(
        CacheKeys.UserProfileCategory,
        id,
        data,
        TimeSpan.FromMinutes(_cacheOptions.CurrentValue.UserProfileExpirationMinutes),
        cancellationToken);

    return data;
}
```

### 9.2 Observability
<!-- STD-MARKER: caching.9.2 -->

`RedisCacheManager` publishes the cache telemetry. Applications must not duplicate it with per-call hit and miss logging.

| Event | Provided by `RedisCacheManager` as |
|---|---|
| Cache hit | Metric `redis_cache.hits` |
| Cache miss, including a Redis failure on read | Metric `redis_cache.misses` |
| Successful write | Metric `redis_cache.sets` |
| Successful removal | Metric `redis_cache.removals` |
| Redis failure, tagged by operation | Metric `redis_cache.errors` and a `Warning` log |
| Deserialization failure | `Warning` log, returned as a miss |
| Operation latency | Histogram `redis_cache.operation.duration` (ms), tagged by operation and outcome |

The metrics are published on the meter named `RedisCacheManager` (`RedisCacheMetrics.MeterName`). When the application exports metrics (for example with OpenTelemetry or Application Insights), it must add that meter so the cache metrics are collected.

The following remains the responsibility of the application:

| Event | Log Level | Required Message Fields |
|---|---|---|
| Billing run pre-load complete | `Information` | TenantId, ClientId, record counts loaded |

All log entries must use structured logging with named properties — no string interpolation in message templates.

---

## 10. Security Constraints
<!-- STD-MARKER: caching.10 -->

Both human developers and AI models must verify that all security constraints in this section are satisfied before a PR is opened. A violation of any rule in this section is a blocking defect.

### 10.1 No Sensitive Data in Cache
<!-- STD-MARKER: caching.10.1 -->

The following data categories must never be written to any cache:

- Passwords or password hashes
- Authentication tokens, JWT tokens, refresh tokens, API keys
- Personally identifiable information (PII) — full name + contact details together, government IDs, payment card data
- Any data governed by a data residency or data sovereignty requirement that prohibits it from being held in a shared store

### 10.2 Cache Connection Security
<!-- STD-MARKER: caching.10.2 -->

The Redis connection must use TLS. Non-TLS Redis connections are not permitted in any environment including local development when connecting to a shared or cloud-hosted Redis instance. `RedisCacheOptions.UseSsl` defaults to `true` and must not be set to `false` outside a local, non-shared Redis instance. Azure Cache for Redis enforces TLS by default — this must not be disabled.

---

## 11. Compliance Verification
<!-- STD-MARKER: caching.11 -->

Both human developers and AI models must run this checklist before marking any caching-related work slice complete. A PR must not be opened until every applicable item is checked.

**What is cached:**
- [ ] All data access operations are evaluated against the qualifying criteria in Section 2.1.
- [ ] Client data, user profiles, dropdown and reference data, and billing run reference data are served from cache.
- [ ] No prohibited data category (Section 2.3) is written to any cache — no PII, tokens, or financial transaction records.

**Cache provider:**
- [ ] Azure Cache for Redis is accessed only through `TCCoder.RedisCacheManager` — `IMemoryCache` and `IDistributedCache` are not used.
- [ ] `TCCoder.RedisCacheManager` is consumed as a `PackageReference` and registered with `AddRedisCacheManager(builder.Configuration)`.
- [ ] The access key is read from Key Vault through `KeyVaultUri` and `AccessKeySecretName` — no key or connection string is hardcoded or stored in configuration files.
- [ ] No application-side Redis connection, retry policy or `StackExchange.Redis` client usage exists.
- [ ] All services inject `ICacheManager`.

**Cache keys:**
- [ ] Every cache entry is classified as tenant-scoped or tenant-independent (Section 5).
- [ ] Tenant-scoped ids begin with TenantId — no exceptions.
- [ ] Tenant-independent entries meet all three conditions in Section 5.3 and are documented in `CacheKeys`.
- [ ] All categories and id builders are defined in a `CacheKeys` static class — no inline magic string categories or ids.
- [ ] No cache read path can return data belonging to a different tenant than the requesting tenant.

**Cache lifecycle:**
- [ ] Every cache entry is assigned to either the long-lived or time-bounded lifecycle pattern.
- [ ] Long-lived entries are released by explicit invalidation on write and carry the configured long-lived expiration as a backstop.
- [ ] Every `SetAsync` call passes `ttl` explicitly — the package `DefaultTtl` is not relied on.
- [ ] All expiration values are sourced from `CacheOptions` via `IOptionsMonitor<CacheOptions>` — not hardcoded.
- [ ] `IOptionsMonitor<T>` is used — not `IOptions<T>`.

**Invalidation:**
- [ ] Every write operation that modifies cached data calls `RemoveAsync` with a `CancellationToken`.
- [ ] No wildcard or pattern-based key deletion is used.
- [ ] Billing run cache is pre-loaded before processing begins and explicitly cleared on run completion.

**Serialization:**
- [ ] Typed values are passed to and from `ICacheManager`; no `JsonSerializer`, `byte[]` or custom serialization helper is used for cache values.
- [ ] Cached types are concrete and serializable with `System.Text.Json` defaults.
- [ ] After a miss the value is always written back, so a stale entry from a prior model shape is overwritten.

**Resilience and observability:**
- [ ] The cache-aside pattern is implemented with fallback to the data source on every miss.
- [ ] Cache calls are not wrapped in `try`/`catch`.
- [ ] The `RedisCacheManager` meter is added to the application's metrics configuration when metrics are exported.
- [ ] Billing run pre-load completion is logged at Information level with record counts.
- [ ] All log entries use structured logging with named properties.

**Security:**
- [ ] No sensitive data (passwords, tokens, PII) is written to any cache.
- [ ] Redis connections use TLS — `UseSsl` is not disabled.

---

## 12. Governance
<!-- STD-MARKER: caching.12 -->

This standard is owned by Troy Crowe. No changes to this file may be merged without Troy Crowe's explicit approval. Changes must be submitted as a pull request that includes a rationale comment explaining the reason for the update or deviation. Direct commits to `dev` or `main` are not permitted.
