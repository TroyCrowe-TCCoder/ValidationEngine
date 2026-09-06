# Coverage Matrix — GlobalCachingStandards.md

**Status:** Reviewed
**Sign Off:** Troy Crowe
**Sign Off Date:** 2026-08-14
**Revision Date:** 2026-08-16
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

**Applicability note:** Applies to any repository whose change set touches C# service/data-access files that read from or write to a distributed cache. Rules trigger only when a changed file contains `IDistributedCache` usage, cache-key construction, or a `CacheKeys`/`CacheOptions`/`CacheSerializer`-named type.

---

### Section 2 — What Must Be Cached

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| caching.2.1 | Qualifying criteria for when a data-access operation should be cached (repeated load, expensive load, batch reference data, UI dropdown data); small single-table reads don't qualify on frequency alone | Development | Manual-only | Manual-only-comment | Every-Commit | Requires business judgment about access frequency/cost patterns; not structurally determinable from a single file diff | Missing |
| caching.2.2 | Required caching categories (client data, user profiles, dropdown/reference data, billing thresholds, billing run reference data) must be served from cache, not a DB round-trip per request | Development | Manual-only | Manual-only-comment | Every-Commit | Requires semantic classification of what a repository method's data represents; not reliably inferable from syntax alone | Missing |
| caching.2.3 | Must-not-cache categories (PII, auth tokens/credentials, financial transaction records, every-request-changing data, any key missing TenantId) | Development | Heuristic | Warning | Every-Commit | Roslyn analyzer: `GlobalStandards.Analyzers.Caching` rule `CACHE011` flags `IDistributedCache.SetAsync` call sites whose cached value (directly or via a `CacheSerializer.Serialize(...)` call) is a variable/member or type property/field whose name matches a known-sensitive naming pattern (Password, Token, SSN, CardNumber, etc.) — shared detection with caching.10.1 | Existing |

---

### Section 3 — Cache Provider

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| caching.3 | Azure Cache for Redis is the only approved provider; `IMemoryCache` must not be used | Technology | Binary | Hard-stop | Every-Commit | Roslyn analyzer: `GlobalStandards.Analyzers.Caching` rule `CACHE001` flags any `IMemoryCache` reference | Existing |
| caching.3.1 | Redis registered via `AddStackExchangeRedisCache` with connection string/instance name from configuration, `AbortOnConnectFail = false`, retry policy configured | Technology | Binary | Hard-stop | Every-Commit (when `Program.cs`/startup files change) | Roslyn-syntax: parse `AddStackExchangeRedisCache` call site for required option assignments; flag hardcoded connection strings | Missing |
| caching.3.2 | Services inject `IDistributedCache`; direct `StackExchange.Redis` client usage in application services is not permitted | Technology | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag `IConnectionMultiplexer`/`ConnectionMultiplexer` usage or injection inside non-infrastructure service classes | Missing |

---

### Section 4 — Cache Key Naming

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| caching.4.1 | Cache keys follow the `{TenantId}:{ClientId}:{DataCategory}`-style hierarchy; TenantId always first segment; DataCategory lowercase hyphen-separated | Development | Binary | Hard-stop | Every-Commit | Roslyn analyzer: `GlobalStandards.Analyzers.Caching` rule `CACHE005` parses string-interpolation/concatenation expressions inside `CacheKeys` class methods; flags methods missing a `tenantId` parameter or whose first key segment isn't the `tenantId` parameter — shared detection with caching.5.1 | Existing |
| caching.4.2 | All cache keys defined as constants/static members in a dedicated `CacheKeys` static class; no inline magic-string keys | Development | Binary | Hard-stop | Every-Commit | Roslyn analyzer: `GlobalStandards.Analyzers.Caching` rule `CACHE004` flags `_cache.GetAsync`/`SetAsync`/`RemoveAsync`/`RefreshAsync` call sites whose key argument is a raw string literal, interpolated string, or concatenation instead of a `CacheKeys.*`-sourced expression | Existing |

---

### Section 5 — Tenant Isolation

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| caching.5.1 | TenantId must be the first segment of every cache key, without exception, even though the application isn't yet multi-tenant; tenant key partitioning is a security requirement, not only a correctness requirement | Development | Binary | Hard-stop | Every-Commit | Roslyn analyzer: `GlobalStandards.Analyzers.Caching` rule `CACHE005` flags any key-building method in the `CacheKeys` class whose first interpolated/concatenated segment isn't a `tenantId`-named parameter — duplicates caching.4.1 detection | Existing |
| caching.5.2 | Verify before PR that every new/changed cache key begins with TenantId (mechanically covered by caching.5.1 / `CACHE005`) and that no cache read path can leak cross-tenant data (a distinct, non-mechanical concern) | Development | Heuristic | Warning | Every-Commit | Key-structure half is already enforced via `CACHE005` (see caching.5.1) — no separate detection needed. Cross-tenant read-leak half requires tracing that a cache read's key expression is built using the *same* tenant context as the requesting caller, which is a control-flow/business-context judgment a single-file structural scan cannot reliably assert; remains Manual-only | Missing |

---

### Section 6 — Cache Lifecycle

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| caching.6.1 | Every cache entry assigned to one of two lifecycle patterns: Long-lived (invalidate-on-write only, no expiration) or Time-bounded (configured expiration) | Development | Manual-only | Manual-only-comment | Every-Commit | Requires judgment about the correct lifecycle classification for a given data type; the mechanical checks (caching.6.2/6.3) below cover the implementation-pattern half | Missing |
| caching.6.2 | Long-lived cache entries must not set an expiration; must be invalidated only on write; methods must be decorated with `[LongLivedCache]` | Development | Binary | Hard-stop | Every-Commit | Roslyn analyzer: `GlobalStandards.Analyzers.Caching` rule `CACHE016` flags `SetAbsoluteExpiration`/`SetSlidingExpiration` calls and `AbsoluteExpiration`/`AbsoluteExpirationRelativeToNow`/`SlidingExpiration` property assignments inside a method marked `[LongLivedCache]` | Existing |
| caching.6.3 | Time-bounded cache entries use `IOptionsMonitor<CacheOptions>` for expiration, never `IOptions<T>`, never hardcoded expiration values | Technology | Binary | Hard-stop | Every-Commit | Roslyn analyzer: `GlobalStandards.Analyzers.Caching` rule `CACHE006` flags `IOptions<CacheOptions>` (or any `*CacheOptions`-suffixed type) injection instead of `IOptionsMonitor<CacheOptions>`; flags `TimeSpan.From*` literals passed directly into `SetAbsoluteExpiration` without a `.CurrentValue`-sourced expression | Existing |
| caching.6.4 | Cache expiration values defined in configuration under a `Cache` section, bound to a strongly typed options class with no default values assigned in the class | Development | Binary | Hard-stop | Every-Commit (when the options class or `Program.cs`/config changes) | Roslyn analyzer: `GlobalStandards.Analyzers.Caching` rule `CACHE007` flags `*CacheOptions`-style class properties with a non-default initializer; verifies a matching `Configure<CacheOptions>(...)` registration exists | Existing |

---

### Section 7 — Invalidation and Pre-Loading

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| caching.7.1 | Write operations (create/update/delete) must invalidate affected cache entries in the same method via `RemoveAsync` with a `CancellationToken`, not deferred to a background process without documented approval | Development | Binary | Hard-stop | Every-Commit | Roslyn analyzer: `GlobalStandards.Analyzers.Caching` rule `CACHE010` flags write-verb-named methods (`Create*`, `Update*`, `Delete*`, `Remove*`, `Insert*`, `Save*`, `Add*`, `Patch*`) in a cache-aware service (a type injecting `IDistributedCache`) that have no `IDistributedCache.RemoveAsync(..., cancellationToken)` call in the same method body | Existing |
| caching.7.2 | Group invalidation must explicitly remove all affected keys by name; wildcard/pattern-based Redis key deletion (`KEYS` with wildcard) must not be used | Development | Binary | Hard-stop | Every-Commit | Roslyn analyzer: `GlobalStandards.Analyzers.Caching` rule `CACHE009` flags `IServer.Keys(...)` enumeration and raw `IDatabase.Execute`/`ExecuteAsync` calls issuing a `KEYS`/`SCAN` command | Existing |
| caching.7.3 | Billing run pre-loads required reference data at run start (thresholds, carrier mappings, GL codes, billing rules) before any invoice processing; explicitly clears run-scoped entries on completion | Development | Manual-only | Manual-only-comment | Every-Commit | Requires understanding of billing-run control flow ordering across multiple methods; not a single-file structural check | Missing |

---

### Section 8 — Serialization

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| caching.8.1 | `System.Text.Json` is the required cache serialization format; `Newtonsoft.Json` must not be used for cache serialization; no other binary formats without approved deviation | Technology | Binary | Hard-stop | Every-Commit | Roslyn analyzer: `GlobalStandards.Analyzers.Caching` rule `CACHE008` flags `Newtonsoft.Json.JsonConvert` usage inside a `CacheSerializer`-named class or any class that injects/uses `IDistributedCache` | Existing |
| caching.8.2 | A shared `CacheSerializer` helper must be used rather than inline `JsonSerializer` calls scattered across services | Development | Heuristic | Warning | Every-Commit | Roslyn analyzer: `GlobalStandards.Analyzers.Caching` rule `CACHE012` flags `System.Text.Json.JsonSerializer.Serialize`/`Deserialize` calls made directly inside a non-`CacheSerializer` class that injects/uses `IDistributedCache` | Existing |
| caching.8.3 | Deserialization failures handled by evicting the stale entry, logging a warning, and falling through to the data source | Development | Heuristic | Warning | Every-Commit | Roslyn analyzer: `GlobalStandards.Analyzers.Caching` rule `CACHE013` flags `CacheSerializer.Deserialize` call sites not enclosed in a try block whose catch clause calls `IDistributedCache.RemoveAsync` to evict the stale entry | Existing |

---

### Section 9 — Resilience and Observability

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| caching.9.1 | Cache-aside pattern required for all reads: try cache, on miss read source and populate cache, on cache error log warning and fall through — never throw on cache failure | Development | Heuristic | Warning | Every-Commit | Roslyn analyzer: `GlobalStandards.Analyzers.Caching` rule `CACHE014` flags `IDistributedCache` call sites (`GetAsync`/`SetAsync`/`RemoveAsync`/etc.) with no surrounding try/catch, or whose catch clause rethrows instead of falling through to the data source | Existing |
| caching.9.2 | Cache hit/miss/error events logged at specified levels (Debug/Debug/Warning/Warning/Warning/Information) with required fields, using structured logging (no string interpolation) | Development | Heuristic | Warning | Every-Commit | Roslyn analyzer: `GlobalStandards.Analyzers.Caching` rule `CACHE015` flags methods calling an `IDistributedCache` operation (`GetAsync`/`SetAsync`/`RemoveAsync`) with no `ILogger`/`ILogger<T>` call in the same method body; string-interpolated log message templates are covered separately by `GlobalLoggingStandards.md`'s structured-logging detection | Existing |

---

### Section 10 — Security Constraints

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| caching.10.1 | Passwords/hashes, auth tokens/JWTs/API keys, PII, and data-residency-restricted data must never be written to any cache | Development | Heuristic | Warning | Every-Commit | Roslyn analyzer: `GlobalStandards.Analyzers.Caching` rule `CACHE011` flags `IDistributedCache.SetAsync` call sites whose cached value matches a known-sensitive naming pattern (duplicates caching.2.3 detection — same technique, two markers) | Existing |
| caching.10.2 | Redis connection must use TLS in all environments; TLS must not be disabled | Technology | Binary | Hard-stop | Every-Commit (when connection configuration changes) | Roslyn-syntax/config-scan: flag Redis connection strings/`ConfigurationOptions` with `Ssl=False` or equivalent TLS-disabling settings | Missing |

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
4. caching.10.2 (TLS) and caching.3.1 (Redis registration options) both key off changes to startup/configuration files rather than arbitrary service files — narrower trigger scope than most other rows in this matrix.

