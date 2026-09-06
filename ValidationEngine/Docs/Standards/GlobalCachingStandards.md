# Caching Standards

**Version:** 2.1.0
**Status:** Active
**Applies To:** All repositories under the GlobalStandards governance model
**Audience:** AI models and human developers
**Created:** 2025-01-01
**Last Modified:** 2026-08-17

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
| Any cache entry missing a TenantId key segment | Cross-tenant data leakage risk — TenantId is required in every key without exception; see Section 5 |

---

## 3. Cache Provider
<!-- STD-MARKER: caching.3 -->

Both human developers and AI models must use Azure Cache for Redis as the cache provider for all applications. In-process memory cache (`IMemoryCache`) must not be used. Redis is mandated for the following reasons:

- **Memory isolation** — cache memory is separate from application process memory; large cached data sets do not compete with application and batch processing memory
- **Multi-instance correctness** — all application instances share a single cache; no instance holds a stale or inconsistent local copy
- **Restart resilience** — the cache survives application deployments and restarts; entries do not need to be rebuilt from the database on every restart
- **Multi-tenancy readiness** — a shared external cache with key-based tenant partitioning is the correct foundation for a multi-tenant architecture

No other cache provider may be introduced without an approved deviation documented in the repository-local addendum.

### 3.1 Registration
<!-- STD-MARKER: caching.3.1 -->

```csharp
// Required — Redis registration in Program.cs
// Connection string and instance name must come from Key Vault-backed configuration
// They must never be hardcoded or committed to source control
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = builder.Configuration["Cache:InstanceName"];
    options.ConfigurationOptions = new ConfigurationOptions
    {
        ConnectRetry = 3,
        ReconnectRetryPolicy = new ExponentialRetry(5000),
        AbortOnConnectFail = false   // Required — must not crash on startup if Redis is temporarily unavailable
    };
});
```

### 3.2 Injection
<!-- STD-MARKER: caching.3.2 -->

All services that read from or write to the cache must inject `IDistributedCache`. Direct use of the StackExchange.Redis client inside application services is not permitted — all cache access must go through `IDistributedCache`.

```csharp
public class ReferenceDataService : IReferenceDataService
{
    private readonly IDistributedCache _cache;
    private readonly IOptionsMonitor<CacheOptions> _cacheOptions;

    public ReferenceDataService(IDistributedCache cache, IOptionsMonitor<CacheOptions> cacheOptions)
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

Cache keys must follow this hierarchy. **TenantId is always the first segment — no exceptions.** The application is not currently multi-tenant, but the key structure is established now so the cache layer requires no structural changes when multi-tenancy is introduced. Segments that do not apply to the data being cached are omitted after TenantId — the hierarchy narrows from left to right based on the scope of the data.

```
{TenantId}:{ClientId}:{DataCategory}
{TenantId}:{ClientId}:{ParentId}:{ChildDataCategory}
{TenantId}:{UserId}:{DataCategory}
{TenantId}:{DataCategory}
```

- **TenantId** — always required; always the first segment
- **ClientId** — required when the data belongs to a specific client; omitted when the data is not client-specific
- **UserId** — required when the data belongs to a specific user; omitted otherwise
- **ParentId** — required when a child collection belongs to a specific parent record
- **DataCategory** — the type of data being cached; always lowercase, hyphen-separated (e.g., `item-types`, `thresholds`, `user-profile`)

```csharp
// Required — all keys defined in a dedicated CacheKeys static class
public static class CacheKeys
{
    // Tenant-level data — not client-specific
    public static string ItemTypes(int tenantId)
        => $"{tenantId}:item-types";

    // Client-scoped data
    public static string ClientConfig(int tenantId, int clientId)
        => $"{tenantId}:{clientId}:config";

    public static string ClientThresholds(int tenantId, int clientId)
        => $"{tenantId}:{clientId}:thresholds";

    // User-scoped within a tenant
    public static string UserProfile(int tenantId, int userId)
        => $"{tenantId}:{userId}:user-profile";

    // Child collection scoped to a parent record within a client
    public static string ItemTypeDescriptions(int tenantId, int clientId, int itemTypeId)
        => $"{tenantId}:{clientId}:{itemTypeId}:item-type-descriptions";

    // Billing run reference data — client-scoped, held for the duration of the run
    public static string CarrierMappings(int tenantId, int clientId)
        => $"{tenantId}:{clientId}:carrier-mappings";

    public static string GlCodeMappings(int tenantId, int clientId)
        => $"{tenantId}:{clientId}:gl-code-mappings";

    public static string ClientBillingRules(int tenantId, int clientId)
        => $"{tenantId}:{clientId}:billing-rules";
}
```

### 4.2 Key Constant Rules
<!-- STD-MARKER: caching.4.2 -->

- All cache keys must be defined as constants or static members in a dedicated `CacheKeys` static class.
- Magic string keys scattered inline throughout the codebase are not permitted.
- The `CacheKeys` class must be the single source of truth for all key definitions in the application.

---

## 5. Tenant Isolation
<!-- STD-MARKER: caching.5 -->

Both human developers and AI models must verify that every cache entry begins with the TenantId key segment before the work slice is closed. A cache entry missing a TenantId is a violation regardless of whether the data appears sensitive. A cache entry that can return one tenant's data to a different tenant is a critical security violation and must be treated as a blocking defect.

### 5.1 TenantId Is Always Required
<!-- STD-MARKER: caching.5.1 -->

TenantId must be the first segment of every cache key without exception. The application is not currently multi-tenant, but this structure is enforced now so that the cache layer is correct and complete when multi-tenancy is introduced. Retrofitting cache keys across an established codebase is costly and error-prone — building it correctly from the start eliminates that risk entirely.

Tenant key partitioning is a security requirement, not only a data-correctness requirement. A cache entry for tenant-scoped data that does not include the tenant identifier in the key must be treated as a security violation and resolved before the PR is approved.

```csharp
// Required — TenantId is always the first segment, even in a single-tenant application
var key = CacheKeys.ClientThresholds(tenantId, clientId);
```

### 5.2 Tenant Isolation Verification
<!-- STD-MARKER: caching.5.2 -->

Before a PR is opened, both human developers and AI models must confirm that every cache key in new or changed code begins with TenantId and that no cache read path can return data belonging to a different tenant than the one making the request. Any cache key that does not begin with TenantId must be corrected before the PR is approved.

---

## 6. Cache Lifecycle
<!-- STD-MARKER: caching.6 -->

Both human developers and AI models must assign every cache entry to one of the two lifecycle patterns defined in this section and implement the correct release strategy for that pattern. A cache entry with no defined release strategy is not permitted.

### 6.1 Lifecycle Patterns
<!-- STD-MARKER: caching.6.1 -->

| Pattern | Data Type | Load | Release Strategy |
|---|---|---|---|
| **Long-lived** | Stable reference data, dropdown lists, data that changes only on explicit admin write | On first request or on application startup | Explicit invalidation on write only — no expiration set |
| **Time-bounded** | User profiles, session-scoped data, billing run data | On demand | Configured expiration; may also be invalidated on explicit write |

**Long-lived pattern** is the correct choice when the data changes only when an administrator explicitly modifies it. Setting a time-based expiration on this data causes unnecessary database round-trips and a staleness window that serves no purpose. Invalidation on write is the only release mechanism — the cache entry lives until the data changes.

**Time-bounded pattern** is the correct choice when the data has a natural staleness window — user profiles that may be updated externally, session-scoped data tied to a user's activity, or billing run data that is only valid for the duration of the run.

### 6.2 Long-Lived Cache Implementation
<!-- STD-MARKER: caching.6.2 -->

Any method implementing the long-lived pattern must be decorated with `[LongLivedCache]` so the rule can be verified mechanically. The attribute marks the code boundary where "this entry is long-lived" is an asserted fact rather than an inferred judgment call; it does not replace the design-time decision in Section 6.1, it records the outcome of that decision. A method marked `[LongLivedCache]` must not set `AbsoluteExpiration`, `AbsoluteExpirationRelativeToNow`, or `SlidingExpiration` on a `DistributedCacheEntryOptions` instance, and must not call `DistributedCacheEntryOptions.SetAbsoluteExpiration` or `SetSlidingExpiration`.

```csharp
// Required — no expiration set; entry lives until explicitly invalidated on write
// GetFromCacheAsync handles deserialization failures per Section 8.3
[LongLivedCache]
public async Task<IEnumerable<ItemType>> GetItemTypesAsync(int tenantId, CancellationToken cancellationToken)
{
    var key = CacheKeys.ItemTypes(tenantId);

    var cached = await GetFromCacheAsync<IEnumerable<ItemType>>(key, cancellationToken);
    if (cached is not null)
        return cached;

    var data = await _repository.GetItemTypesAsync(tenantId, cancellationToken);

    // No expiration — entry is held until explicitly removed on write
    await _cache.SetAsync(key, CacheSerializer.Serialize(data), cancellationToken);

    return data;
}
```

### 6.3 Time-Bounded Cache Implementation
<!-- STD-MARKER: caching.6.3 -->

All expiration values must be defined in application configuration and accessed via `IOptionsMonitor<CacheOptions>`. `IOptionsMonitor<T>` must be used — not `IOptions<T>`. `IOptions<T>` is a startup snapshot and does not pick up configuration changes without an application restart. `IOptionsMonitor<T>` re-evaluates `CurrentValue` on every access, picking up changes immediately.

Hardcoded expiration values are not permitted.

```csharp
// Required — expiration sourced from configuration via IOptionsMonitor, not hardcoded
// GetFromCacheAsync handles deserialization failures per Section 8.3
public async Task<UserProfile> GetUserProfileAsync(int tenantId, int userId, CancellationToken cancellationToken)
{
    var key = CacheKeys.UserProfile(tenantId, userId);

    var cached = await GetFromCacheAsync<UserProfile>(key, cancellationToken);
    if (cached is not null)
        return cached;

    var data = await _repository.GetUserProfileAsync(userId, cancellationToken);

    var options = new DistributedCacheEntryOptions()
        .SetAbsoluteExpiration(
            TimeSpan.FromMinutes(_cacheOptions.CurrentValue.UserProfileExpirationMinutes));

    await _cache.SetAsync(key, CacheSerializer.Serialize(data), options, cancellationToken);

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
}

// Required — registration in Program.cs
builder.Services.Configure<CacheOptions>(builder.Configuration.GetSection("Cache"));
```

The `appsettings.json` section below is the single source of truth for all expiration values:

```json
{
  "Cache": {
    "UserProfileExpirationMinutes": 30,
    "BillingRunExpirationMinutes": 120
  }
}
```

---

## 7. Invalidation and Pre-Loading
<!-- STD-MARKER: caching.7 -->

Both human developers and AI models must implement explicit cache invalidation for every cache entry that corresponds to data that can be modified through the application. A cache entry that has no defined release strategy is a violation.

### 7.1 Invalidation on Write
<!-- STD-MARKER: caching.7.1 -->

Any write operation (create, update, delete) that changes data currently held in cache must explicitly remove the affected cache entries as part of the same logical operation. Invalidation must use `RemoveAsync` from `IDistributedCache` and must always pass a `CancellationToken`. Invalidation must occur in the same service method as the write — it must not be deferred to a background process unless the staleness window is explicitly documented and approved.

```csharp
// Required — invalidate immediately after the write succeeds
public async Task UpdateClientConfigAsync(int tenantId, int clientId, ConfigModel config, CancellationToken cancellationToken)
{
    await _repository.UpdateClientConfigAsync(config, cancellationToken);

    await _cache.RemoveAsync(CacheKeys.ClientConfig(tenantId, clientId), cancellationToken);
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

    await _cache.RemoveAsync(CacheKeys.ItemTypes(tenantId), cancellationToken);
    await _cache.RemoveAsync(CacheKeys.ItemTypeDescriptions(tenantId, clientId, itemTypeId), cancellationToken);
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

```csharp
// Required — pre-load all billing run reference data before processing begins
// All four data types must be loaded; processing must not start until this method completes
public async Task PreloadBillingRunCacheAsync(int tenantId, int clientId, CancellationToken cancellationToken)
{
    var expiry = new DistributedCacheEntryOptions()
        .SetAbsoluteExpiration(
            TimeSpan.FromMinutes(_cacheOptions.CurrentValue.BillingRunExpirationMinutes));

    var thresholds = await _repository.GetClientThresholdsAsync(clientId, cancellationToken);
    await _cache.SetAsync(CacheKeys.ClientThresholds(tenantId, clientId),
        CacheSerializer.Serialize(thresholds), expiry, cancellationToken);

    var carrierMappings = await _repository.GetCarrierMappingsAsync(clientId, cancellationToken);
    await _cache.SetAsync(CacheKeys.CarrierMappings(tenantId, clientId),
        CacheSerializer.Serialize(carrierMappings), expiry, cancellationToken);

    var glCodes = await _repository.GetGlCodeMappingsAsync(clientId, cancellationToken);
    await _cache.SetAsync(CacheKeys.GlCodeMappings(tenantId, clientId),
        CacheSerializer.Serialize(glCodes), expiry, cancellationToken);

    var billingRules = await _repository.GetClientBillingRulesAsync(clientId, cancellationToken);
    await _cache.SetAsync(CacheKeys.ClientBillingRules(tenantId, clientId),
        CacheSerializer.Serialize(billingRules), expiry, cancellationToken);

    _logger.LogInformation(
        "Billing run cache pre-loaded for TenantId {TenantId} ClientId {ClientId}. " +
        "Thresholds: {ThresholdCount}, CarrierMappings: {CarrierCount}, GlCodes: {GlCount}, BillingRules: {RuleCount}",
        tenantId, clientId,
        thresholds.Count(), carrierMappings.Count(), glCodes.Count(), billingRules.Count());
}

// Required — explicitly release all run-scoped cache entries on run completion
public async Task ClearBillingRunCacheAsync(int tenantId, int clientId, CancellationToken cancellationToken)
{
    await _cache.RemoveAsync(CacheKeys.ClientThresholds(tenantId, clientId), cancellationToken);
    await _cache.RemoveAsync(CacheKeys.CarrierMappings(tenantId, clientId), cancellationToken);
    await _cache.RemoveAsync(CacheKeys.GlCodeMappings(tenantId, clientId), cancellationToken);
    await _cache.RemoveAsync(CacheKeys.ClientBillingRules(tenantId, clientId), cancellationToken);
}
```

---

## 8. Serialization
<!-- STD-MARKER: caching.8 -->

Both human developers and AI models must use the approved serialization format defined in this section for all cache entries. All cache entries are serialized — Redis stores values as byte arrays.

### 8.1 Approved Format
<!-- STD-MARKER: caching.8.1 -->

**JSON via `System.Text.Json`** is the required serialization format for all cache entries. `Newtonsoft.Json` must not be used for cache serialization in new code. `MessagePack` or other binary formats may not be introduced without an approved deviation.

### 8.2 Serialization Helper
<!-- STD-MARKER: caching.8.2 -->

A shared cache serialization helper must be used rather than inline `JsonSerializer` calls scattered throughout the codebase. The helper must encapsulate serialization, deserialization, and `byte[]` conversion.

```csharp
// Required — shared helper; not inline JsonSerializer calls in every service
public static class CacheSerializer
{
    public static byte[] Serialize<T>(T value)
        => JsonSerializer.SerializeToUtf8Bytes(value);

    public static T? Deserialize<T>(byte[]? data)
        => data is null ? default : JsonSerializer.Deserialize<T>(data);
}
```

### 8.3 Deserialization Failure Handling
<!-- STD-MARKER: caching.8.3 -->

Deserialization can fail when a cached entry was written by a prior application version with a different object shape — for example after a deploy that changes a cached model. Both human developers and AI models must handle deserialization failures by evicting the stale entry, logging a warning, and falling through to the data source. Deserialization failures must never throw to the caller.

```csharp
// Required — handle deserialization failure; evict stale entry and fall through to data source
private async Task<T?> GetFromCacheAsync<T>(string key, CancellationToken cancellationToken)
    where T : class
{
    var cached = await _cache.GetAsync(key, cancellationToken);
    if (cached is null) return null;

    try
    {
        return CacheSerializer.Deserialize<T>(cached);
    }
    catch (JsonException ex)
    {
        _logger.LogWarning(ex, "Cache deserialization failed for {Key}. Evicting stale entry and falling through to data source.", key);
        await _cache.RemoveAsync(key, cancellationToken);
        return null;
    }
}
```

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
4. On cache error — log a warning and fall through to the data source. Do not throw.

```csharp
// Required — cache-aside with IDistributedCache, deserialization, and fallback on error
public async Task<UserProfile> GetUserProfileAsync(int tenantId, int userId, CancellationToken cancellationToken)
{
    var key = CacheKeys.UserProfile(tenantId, userId);

    try
    {
        var result = await GetFromCacheAsync<UserProfile>(key, cancellationToken);
        if (result is not null)
        {
            _logger.LogDebug("Cache hit for {Key}", key);
            return result;
        }

        _logger.LogDebug("Cache miss for {Key}", key);
    }
    catch (Exception ex)
    {
        // Cache failure must not break the operation — log and fall through to data source
        _logger.LogWarning(ex, "Cache read failed for {Key}. Falling through to data source.", key);
    }

    var data = await _repository.GetUserProfileAsync(userId, cancellationToken);

    try
    {
        var options = new DistributedCacheEntryOptions()
            .SetAbsoluteExpiration(
                TimeSpan.FromMinutes(_cacheOptions.CurrentValue.UserProfileExpirationMinutes));

        await _cache.SetAsync(key, CacheSerializer.Serialize(data), options, cancellationToken);
    }
    catch (Exception ex)
    {
        // Cache write failure must not break the operation — log and continue
        _logger.LogWarning(ex, "Cache write failed for {Key}.", key);
    }

    return data;
}
```

### 9.2 Observability
<!-- STD-MARKER: caching.9.2 -->

Cache hits, misses, and errors must be logged at the following levels. Consistent logging enables performance diagnostics, cache effectiveness monitoring, and issue investigation — particularly during billing runs where cache hit rates are operationally significant.

| Event | Log Level | Required Message Fields |
|---|---|---|
| Cache hit | `Debug` | Key |
| Cache miss | `Debug` | Key |
| Cache read error | `Warning` | Key, Exception |
| Cache write error | `Warning` | Key, Exception |
| Deserialization failure | `Warning` | Key, Exception |
| Billing run pre-load complete | `Information` | TenantId, ClientId, record counts loaded |

All log entries must use structured logging with named properties — no string interpolation in message templates.

Any method calling an `IDistributedCache` operation (`GetAsync`, `SetAsync`, `RemoveAsync`) must contain at least one `ILogger`/`ILogger<T>` call in the same method body so the presence of observability logging is mechanically checkable.

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

### 10.2 Distributed Cache Connection Security
<!-- STD-MARKER: caching.10.2 -->

The Redis connection must use TLS. Non-TLS Redis connections are not permitted in any environment including local development when connecting to a shared or cloud-hosted Redis instance. Azure Cache for Redis enforces TLS by default — this must not be disabled.

---

## 11. Compliance Verification
<!-- STD-MARKER: caching.11 -->

Both human developers and AI models must run this checklist before marking any caching-related work slice complete. A PR must not be opened until every applicable item is checked.

**What is cached:**
- [ ] All data access operations are evaluated against the qualifying criteria in Section 2.1.
- [ ] Client data, user profiles, dropdown and reference data, and billing run reference data are served from cache.
- [ ] No prohibited data category (Section 2.3) is written to any cache — no PII, tokens, or financial transaction records.

**Cache provider:**
- [ ] Azure Cache for Redis is used as the cache provider — `IMemoryCache` is not used.
- [ ] Redis is registered with `AbortOnConnectFail = false` and a connect retry policy.
- [ ] The Redis connection string and instance name come from Key Vault-backed configuration — not hardcoded.
- [ ] All services inject `IDistributedCache` — no direct StackExchange.Redis client usage in services.

**Cache keys:**
- [ ] Every cache key begins with TenantId — no exceptions.
- [ ] All cache keys follow the defined hierarchy structure.
- [ ] All cache keys are defined in a `CacheKeys` static class — no inline magic string keys.
- [ ] No cache read path can return data belonging to a different tenant than the requesting tenant.

**Cache lifecycle:**
- [ ] Every cache entry is assigned to either the long-lived or time-bounded lifecycle pattern.
- [ ] Long-lived entries have no expiration set and are released only by explicit invalidation on write.
- [ ] Time-bounded entries have an explicit expiration configured.
- [ ] All expiration values are sourced from `CacheOptions` via `IOptionsMonitor<CacheOptions>` — not hardcoded.
- [ ] `IOptionsMonitor<T>` is used — not `IOptions<T>`.

**Invalidation:**
- [ ] Every write operation that modifies cached data calls `RemoveAsync` with a `CancellationToken`.
- [ ] No wildcard or pattern-based key deletion is used.
- [ ] Billing run cache is pre-loaded before processing begins and explicitly cleared on run completion.

**Serialization:**
- [ ] All cache entries use `System.Text.Json` serialization via the `CacheSerializer` helper.
- [ ] Deserialization failures are caught, logged, the stale entry evicted, and execution falls through to the data source.

**Resilience and observability:**
- [ ] The cache-aside pattern is implemented with fallback to the data source on cache read or write error.
- [ ] Cache hits, misses, read errors, write errors, and deserialization failures are logged at the required levels.
- [ ] Billing run pre-load completion is logged at Information level with record counts.
- [ ] All log entries use structured logging with named properties.

**Security:**
- [ ] No sensitive data (passwords, tokens, PII) is written to any cache.
- [ ] Redis connections use TLS — non-TLS connections are not permitted.

---

## 12. Governance
<!-- STD-MARKER: caching.12 -->

This standard is owned by Troy Crowe. No changes to this file may be merged without Troy Crowe's explicit approval. Changes must be submitted as a pull request that includes a rationale comment explaining the reason for the update or deviation. Direct commits to `dev` or `main` are not permitted.
