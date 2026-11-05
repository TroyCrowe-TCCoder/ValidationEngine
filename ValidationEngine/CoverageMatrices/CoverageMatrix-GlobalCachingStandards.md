# Coverage Matrix — GlobalCachingStandards.md

**Status:** Reviewed
**Sign Off:** Troy Crowe
**Sign Off Date:** 2026-08-14
**Revision Date:** 2026-11-01
**Columns**

| Column | Meaning |
|---|---|
| STD-MARKER | Rule id from the standards file. |
| Rule Summary | One-line paraphrase of the rule. |
| Category | Structure / Technology / Development / Procedural / Infrastructure. |
| Confidence | Binary (deterministic pass/fail) / Heuristic (risk-flag, needs human confirmation) / Manual-only (no automation path) / N/A (Aggregate) (pure rollup/index row; automation targets the underlying constituent rules instead). |
| Severity | Hard-stop / Warning / Manual-only-comment (3-tier enforcement model). |
| Frequency | Every-Commit (validated against the files touched in the current change set) / Periodic (validated on a recurring, configurable cadence). |
| Technique | Short detection approach. |
| Status | Missing / Existing / Acknowledged-future-work. |

**Applicability note:** Applies to any repository whose change set touches C# service/data-access files that read from or write to the cache. Rules trigger only when a changed file contains `ICacheManager` usage, cache category/id construction, or a `CacheKeys`/`CacheOptions`-named type. Analyzers in `GlobalStandards.Analyzers.Caching` were written against `IDistributedCache` (standard version 2.1.0); standard version 3.0.0 requires `ICacheManager`, so rows whose analyzer no longer matches the standard are marked `Acknowledged-future-work` until the analyzer is ported.

---

### Section 2 — What Must Be Cached

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| caching.2.1 | Qualifying criteria for when a data-access operation should be cached (repeated load, expensive load, batch reference data, UI dropdown data); small single-table reads don't qualify on frequency alone | Development | Manual-only | Manual-only-comment | Every-Commit | Requires business judgment about access frequency/cost patterns; not structurally determinable from a single file diff | Missing |
| caching.2.2 | Required caching categories (client data, user profiles, dropdown/reference data, billing thresholds, billing run reference data) must be served from cache, not a DB round-trip per request | Development | Manual-only | Manual-only-comment | Every-Commit | Requires semantic classification of what a repository method's data represents; not reliably inferable from syntax alone | Missing |
shared detection with caching.10.1. Targets the `value` argument of `ICacheManager.SetAsync(category, id, value, ttl, ct)` | Existing |

---

### Section 3 — Cache Provider

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
rule `CACHE001` flags any `IMemoryCache` reference; rule `CACHE017` flags any `IDistributedCache` reference; `CACHE002` flags `AddStackExchangeRedisCache` (use `AddRedisCacheManager`); `CACHE003` flags direct StackExchange.Redis clients | Existing |
| caching.3.1 | Redis registered with `AddRedisCacheManager(builder.Configuration)`; access key read from Key Vault via `KeyVaultUri`/`AccessKeySecretName`; no application-side Redis connection or retry configuration; unique `InstanceName` per app unless a cache is intentionally shared | Technology | Binary | Hard-stop | Every-Commit (when `Program.cs`/startup files change) | Roslyn-syntax: verify an `AddRedisCacheManager` call exists when `ICacheManager` is injected; flag `AddStackExchangeRedisCache`, `ConnectionMultiplexer.Connect`, and hardcoded access keys/connection strings. Config-scan: flag an `AccessKey`/connection string in configuration files | Missing |
| caching.3.2 | Services inject `ICacheManager`; direct `StackExchange.Redis`, `IDistributedCache` or `IMemoryCache` usage in application services is not permitted | Technology | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag `IConnectionMultiplexer`/`ConnectionMultiplexer`/`IDistributedCache` usage or injection inside non-infrastructure service classes | Missing |

---

### Section 4 — Cache Key Naming

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
rule `CACHE005` parses id-building methods in the `CacheKeys` class and requires a leading `tenantId` segment unless the method carries a `tenant-independent` comment (caching.5.3) — shared detection with caching.5.1 | Existing |
rule `CACHE004` flags `ICacheManager` `TryGetAsync`/`SetAsync`/`RemoveAsync` calls whose `category` or `id` argument is a raw string literal, interpolated string or concatenation | Existing |

---

### Section 5 — Tenant Isolation

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
duplicates caching.4.1 detection. Skips ids documented as tenant-independent (caching.5.3) | Existing |
| caching.5.2 | Tenant Isolation Verification: verify before PR that every new/changed tenant-scoped id begins with TenantId and that no cache read path can leak cross-tenant data (a distinct, non-mechanical concern) | Development | Heuristic | Warning | Every-Commit | Id-structure half is covered by caching.5.1 / `CACHE005` — no separate detection needed. Cross-tenant read-leak half requires tracing that a cache read's id expression is built using the *same* tenant context as the requesting caller, which is a control-flow/business-context judgment a single-file structural scan cannot reliably assert; remains Manual-only | Missing |
| caching.5.3 | Tenant-independent data may omit TenantId only when the entry contains no tenant-owned data, every caller is entitled to identical content, and the classification is recorded in `CacheKeys` with a comment; when in doubt it is tenant-scoped | Development | Heuristic | Warning | Every-Commit | Roslyn analyzer (future): flag a `CacheKeys` id builder with no `tenantId` parameter that has no tenant-independent comment/attribute. Whether the data is genuinely tenant-independent is a business judgment and remains a manual review item | Missing |

---

### Section 6 — Cache Lifecycle

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| caching.6.1 | Every cache entry assigned to one of two lifecycle patterns: Long-lived (invalidate-on-write plus a configured long expiration as a backstop) or Time-bounded (configured expiration); `ttl` is always passed explicitly | Development | Manual-only | Manual-only-comment | Every-Commit | Requires judgment about the correct lifecycle classification for a given data type; the mechanical checks (caching.6.2/6.3) below cover the implementation-pattern half | Missing |
rule `CACHE016` flags `ICacheManager.SetAsync` calls in a `[LongLivedCache]` method whose `ttl` argument is omitted or not sourced from `CacheOptions.LongLivedExpirationMinutes` | Existing |
rule `CACHE006` flags `IOptions<CacheOptions>` (or any `*CacheOptions`-suffixed type) injection instead of `IOptionsMonitor<CacheOptions>`, `ICacheManager.SetAsync` calls that omit `ttl`, and hardcoded `TimeSpan.From*` literals passed as `ttl` | Existing |
| caching.6.4 | Cache expiration values defined in configuration under a `Cache` section, bound to a strongly typed options class with no default values assigned in the class | Development | Binary | Hard-stop | Every-Commit (when the options class or `Program.cs`/config changes) | Roslyn analyzer: `GlobalStandards.Analyzers.Caching` rule `CACHE007` flags `*CacheOptions`-style class properties with a non-default initializer; verifies a matching `Configure<CacheOptions>(...)` registration exists | Existing |

---

### Section 7 — Invalidation and Pre-Loading

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
no `RemoveAsync(..., cancellationToken)` call in the same method body. Keyed to types that use `ICacheManager` | Existing |
| caching.7.2 | Group invalidation must explicitly remove all affected keys by name; wildcard/pattern-based Redis key deletion (`KEYS` with wildcard) must not be used | Development | Binary | Hard-stop | Every-Commit | Roslyn analyzer: `GlobalStandards.Analyzers.Caching` rule `CACHE009` flags `IServer.Keys(...)` enumeration and raw `IDatabase.Execute`/`ExecuteAsync` calls issuing a `KEYS`/`SCAN` command | Existing |
| caching.7.3 | Billing run pre-loads required reference data at run start (thresholds, carrier mappings, GL codes, billing rules) before any invoice processing; explicitly clears run-scoped entries on completion | Development | Manual-only | Manual-only-comment | Every-Commit | Requires understanding of billing-run control flow ordering across multiple methods; not a single-file structural check | Missing |

---

### Section 8 — Serialization

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
rule `CACHE008` flags `Newtonsoft.Json.JsonConvert` usage inside a `CacheSerializer`-named class or any class that uses `ICacheManager` | Existing |
rule `CACHE012` flags `JsonSerializer` calls in classes that use `ICacheManager` | Existing |
Handled inside the package. The remaining application duty (always write back after a miss) is covered by the cache-aside review in caching.9.1. Analyzer `CACHE013` (required a try/catch that evicts the stale entry) was obsolete and has been retired | Existing |

---

### Section 9 — Resilience and Observability

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
rule `CACHE014` flags try/catch blocks that wrap `ICacheManager` calls. Detecting a `TryGetAsync` miss path with no `SetAsync` write-back is not yet implemented | Existing |
rule `CACHE015` (reversed) flags methods that call `ICacheManager` and also log a cache hit/miss message, which duplicates package telemetry. Verifying `AddMeter("RedisCacheManager")` when metrics export is configured is not yet implemented; string-interpolated log message templates are covered separately by `GlobalLoggingStandards.md`'s structured-logging detection | Existing |

---

### Section 10 — Security Constraints

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
duplicates caching.2.3 detection — same technique, two markers). Targets `ICacheManager.SetAsync` | Existing |
| caching.10.2 | Redis connection must use TLS in all environments; `UseSsl` must not be disabled outside a local, non-shared Redis instance | Technology | Binary | Hard-stop | Every-Commit (when connection configuration changes) | Config-scan: flag `"UseSsl": false` in the `RedisCacheManager` configuration section (and `Ssl=False` in any Redis connection string) outside local development settings | Missing |

---

### Section 11 — Compliance Verification (Checklist)

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| caching.11 | Compliance checklist — restates Sections 2–10; not independently checkable beyond its constituent rules | Procedural | N/A (Aggregate) | N/A | N/A | Pure aggregation/index of Sections 2–10 checks; automation should evaluate the underlying section markers directly rather than this rollup row | Missing |

---

### Section 12 — Governance (Excluded)

`caching.12` (ownership/PR-approval boilerplate) is excluded — not code-checkable, procedural ownership statement only.

---

**Notes:**
1. caching.2.3 and caching.10.1 intentionally duplicate detection techniques because the source standard restates the same rule from different angles (both cover "sensitive data must not be cached"). This mirrors the same pattern already accepted in the logging and agent-team matrices — flagged as a duplicate-coverage note rather than merged, since both markers exist independently in the source document and each still has an independent checkable criterion. The former `caching.10.2` ("TenantId is a security requirement") was retired as a standalone marker — it added no independent checkable criterion beyond `caching.5.1`/`CACHE005` and its normative language was folded directly into `caching.5.1` in the source standard.
2. caching.9.2 (Observability) intentionally overlaps with `GlobalLoggingStandards.md`'s structured-logging rules — detection technique should reuse whatever logging-call-site/structured-logging detector is built for that file.
3. caching.2.1, caching.2.2, caching.6.1, and caching.7.3 require business/semantic judgment about data-access patterns and multi-step control flow that a single-file structural scan cannot reliably assert — marked Manual-only rather than forcing a low-confidence heuristic.
4. caching.10.2 (TLS) and caching.3.1 (Redis registration) both key off changes to startup/configuration files rather than arbitrary service files — narrower trigger scope than most other rows in this matrix.
5. Standard version 3.0.0 replaced `IDistributedCache` with `TCCoder.RedisCacheManager` (`ICacheManager`). The caching analyzers have been ported to `ICacheManager`: `CACHE013` is retired, `CACHE014` and `CACHE015` are reversed, and `CACHE017` is new (flags `IDistributedCache`). `CACHE007` (caching.6.4) and `CACHE009` (caching.7.2) are unaffected.

