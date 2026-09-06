# Performance Standards

**Version:** 1.1.0
**Status:** Active
**Applies To:** All repositories under the GlobalStandards governance model
**Audience:** AI models and human developers
**Created:** 2025-01-01
**Last Modified:** 2026-07-12

---
<!-- STD-MARKER: performance.file -->


## 1. Purpose
<!-- STD-MARKER: performance.1 -->

This standard defines the performance rules that must be applied across all repositories. Both human developers and AI models must apply every rule in this file when writing, reviewing, or generating code. Compliance is verified during pull request review and the pre-merge compliance process. A work slice that introduces a performance violation must not be marked complete and a PR must not be opened until the violation is resolved.

This file covers which operation categories require performance tests, the thresholds those operations must meet, and the implementation patterns required to achieve acceptable performance. Performance tests are defined and governed in [`GlobalTestingStandards.md`](../standards/GlobalTestingStandards.md#11-performance-tests). This file owns the thresholds and implementation requirements that those tests verify.

---

## 1.1 Guiding Principle
<!-- STD-MARKER: performance.1.1 -->

All code must be written with performance as a first-class concern, not an afterthought. Both human developers and AI models must default to memory- and CPU-efficient patterns — including selecting the appropriate collection type for the task, minimizing unnecessary allocations, and avoiding redundant work — even in code that falls outside the specific categories and thresholds enumerated in Section 2. The categories in Section 2 define where measurable performance tests are mandatory; this principle applies to all code, not only those categories.

This principle also incorporates fail-fast behavior. Code must detect and reject invalid input, invalid state, or unrecoverable conditions as early as possible rather than allowing execution to continue and expend resources on work that cannot succeed. Validating preconditions before performing expensive work — rather than after — is both a performance requirement and a reliability requirement: it prevents wasted CPU cycles, allocations, and I/O on operations that were already destined to fail.

---

## 2. Operation Categories Requiring Performance Tests
<!-- STD-MARKER: performance.2 -->

Both human developers and AI models must classify every new or changed operation against the categories below and verify that a BenchmarkDotNet performance test exists for it before the work slice is marked complete.

The following operation categories must have performance tests written using BenchmarkDotNet.

> **Phase 2 Note:** Performance tests are a Phase 2 enforcement requirement as defined in [`GlobalTestingStandards.md`](../standards/GlobalTestingStandards.md#11-performance-tests). The categories and thresholds defined in this section are active and must be used when performance tests are written. The requirement to have them written before a feature is complete will be enforced when Phase 2 is activated.

### 2.1 High-Frequency Service Calls
<!-- STD-MARKER: performance.2.1 -->

Any operation called on every request or at high volume qualifies as a high-frequency service call. Examples include authentication checks, permission lookups, caching layer reads, and any middleware operation that runs on every HTTP request.

**Threshold:** Must complete in under 50ms at the 99th percentile under expected load.

### 2.2 Data Transformation Operations
<!-- STD-MARKER: performance.2.2 -->

Any extension method, mapping operation, or projection that processes a collection qualifies as a data transformation operation. Examples include manual mapping methods, LINQ projections over large result sets, and custom serialization or formatting routines applied to collections.

**Threshold:** Must complete in under 100ms for a collection of 1,000 records.

### 2.3 Repository Operations
<!-- STD-MARKER: performance.2.3 -->

Any stored procedure call, query, or command that operates on large data sets qualifies as a repository operation. Examples include paginated list queries, bulk inserts, and any repository method that joins across multiple tables.

**Threshold:** Must complete in under 200ms for queries operating on representative production-scale data volumes.

### 2.4 Batch Processing Operations
<!-- STD-MARKER: performance.2.4 -->

Any operation that processes a file upload or bulk data set — for example, invoice file processing or bulk record imports — qualifies as a batch processing operation. Batch operations are chunked at 100 records per chunk. The rules governing bulk insert implementation, chunk sizing, transaction boundaries, and error handling are defined in [`GlobalDatabaseStandards.md`](../standards/GlobalDatabaseStandards.md).

> **Threshold Note:** Thresholds for batch processing operations will be baselined against the first real implementation and locked in at that time. Placeholder thresholds are defined below and must be revised before Phase 2 enforcement is activated.

**Threshold — per chunk (100 records):** Must complete in under 500ms.
**Threshold — full file (up to 500 records):** Must complete in under 5 seconds.

---

## 3. Implementation Requirements
<!-- STD-MARKER: performance.3 -->

Both human developers and AI models must apply every rule in this section to all new and changed code. A violation found during a PR review must be corrected before the PR is approved.

### 3.1 Async and Non-Blocking
<!-- STD-MARKER: performance.3.1 -->

All I/O-bound operations — database calls, HTTP calls, file reads — must be implemented using `async`/`await`. Blocking calls using `.Result`, `.Wait()`, or `Task.Run` to wrap synchronous code are not permitted. Both human developers and AI models must scan every new or changed method for these patterns before the work slice is considered complete. Any match is a violation that must be corrected before the PR is opened.

### 3.2 Avoid Unnecessary Allocations
<!-- STD-MARKER: performance.3.2 -->

Operations in the high-frequency and data transformation categories must avoid unnecessary heap allocations in hot paths. When the allocation cost is measurable, `Span<T>`, `Memory<T>`, or array pooling must be used in place of standard heap allocations. Both human developers and AI models must evaluate every method in these categories for unnecessary allocations before the work slice is considered complete.

### 3.3 No N+1 Query Patterns
<!-- STD-MARKER: performance.3.3 -->

Repository operations must not produce N+1 queries. Any query that results in one database round-trip per item in a collection must be refactored to use a single set-based query or a batched approach before the PR is approved. Both human developers and AI models must inspect every repository method that iterates over a collection and verify that it does not produce a query per item. A method that does must be refactored before the work slice is closed.

### 3.4 Parallel Execution for Independent Operations
<!-- STD-MARKER: performance.3.4 -->

When two or more operations are independent of each other — meaning neither depends on the result of the other — they must be executed in parallel. Sequential awaiting of independent operations is a performance violation and must be corrected before the PR is approved. Both human developers and AI models must evaluate every multi-operation method for this condition before the work slice is considered complete.

The correct parallel pattern is not a matter of preference. It is determined by the nature of the work. The rules below define which pattern must be used for each scenario. Any deviation requires a documented justification in the PR description.

#### Choosing the Right Pattern

The primary decision is whether the work is **I/O-bound** or **CPU-bound**.

- **I/O-bound** means the operation spends most of its time waiting — for a database response, a network call, an API response, or a file read. The thread is idle during that wait. Use async patterns.
- **CPU-bound** means the operation spends most of its time computing — transforming data, running calculations, processing records. The thread is actively working. Use parallel patterns.

| | `Task.WhenAll` | `Parallel.Invoke` | `Parallel.ForEach` | `Parallel.ForEachAsync` | `AsParallel()` |
|---|---|---|---|---|---|
| **Work type** | I/O-bound | CPU-bound | CPU-bound | I/O-bound | CPU-bound |
| **Shape** | Fixed set of async methods | Fixed set of sync methods | Same sync method per collection item | Same async method per collection item | LINQ over in-memory collection |
| **Blocks thread** | No | Yes | Yes | No | Yes |
| **Bounded concurrency required** | No | No | Yes — `MaxDegreeOfParallelism` | Yes — `MaxDegreeOfParallelism` | Yes — `WithDegreeOfParallelism` |
| **Do not use on UI thread** | No | Yes | Yes | No | Yes |

#### Decision Rules — Apply in Order

An AI model or developer must apply these rules in order when writing or reviewing a multi-operation method:

1. If the operations are **async and I/O-bound** and are a **fixed named set** → use `Task.WhenAll`.
2. If the operations are **synchronous and CPU-bound** and are a **fixed named set** → use `Parallel.Invoke`.
3. If the operation is **synchronous** and must be called **once per item in a collection** → use `Parallel.ForEach` with `MaxDegreeOfParallelism` set.
4. If the operation is **async** and must be called **once per item in a collection** → use `Parallel.ForEachAsync` with `MaxDegreeOfParallelism` set.
5. If the operation is a **LINQ transformation over a large in-memory collection** → use `AsParallel()` with `WithDegreeOfParallelism` set.
6. If none of the above apply → document the justification in the PR before proceeding.

#### Parallel.Invoke — Fixed Set of Independent Synchronous Methods

**When to use:** A known, fixed set of independent CPU-bound synchronous methods that can run simultaneously. This is the synchronous counterpart to `Task.WhenAll`. Do not use on a UI thread — it blocks the calling thread until all methods complete.

```csharp
// Required — fixed set of independent synchronous methods run in parallel
Parallel.Invoke(
    () => LoadClientData(),
    () => LoadStatusData(),
    () => LoadRegionData()
);
```

#### Task.WhenAll — Fixed Set of Independent Async Methods

**When to use:** A known, fixed set of independent I/O-bound async methods that can run simultaneously and whose results must be aggregated before proceeding. This is the standard pattern for populating a UI screen or service response that requires data from multiple repositories or services at once. Does not block the calling thread.

```csharp
// Required — independent async methods run simultaneously and results aggregated
var clientsTask = GetClientsAsync();
var statusesTask = GetStatusesAsync();
var regionsTask = GetRegionsAsync();

await Task.WhenAll(clientsTask, statusesTask, regionsTask);

var clients = clientsTask.Result;
var statuses = statusesTask.Result;
var regions = regionsTask.Result;
```

#### Parallel.ForEach — Same Synchronous Method Called Per Item in a Collection

**When to use:** The same synchronous CPU-bound method must be called once for every item in a collection and each call is independent. `MaxDegreeOfParallelism` must always be set to prevent thread pool exhaustion in an ASP.NET context.

```csharp
// Required — same synchronous method called in parallel for each item
var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

Parallel.ForEach(records, options, record =>
{
    ValidateRecord(record);
});
```

#### Parallel.ForEachAsync — Same Async Method Called Per Item in a Collection

**When to use:** The same async I/O-bound method must be called once for every item in a collection and each call is independent — for example, processing chunks of records during a bulk data upload. `MaxDegreeOfParallelism` must always be set to control how many async operations are in flight simultaneously and prevent connection pool exhaustion.

```csharp
// Required — same async method called in parallel for each item with bounded concurrency
var options = new ParallelOptions { MaxDegreeOfParallelism = 4 };

await Parallel.ForEachAsync(recordChunks, options, async (chunk, cancellationToken) =>
{
    await _repository.BulkInsertAsync(chunk, cancellationToken);
});
```

#### PLINQ AsParallel() — Parallel LINQ Transformation Over an In-Memory Collection

**When to use:** A LINQ transformation or projection must be applied to a large in-memory collection where the per-item work is CPU-bound. The collection must be large enough to justify the parallelism overhead — PLINQ has partitioning and coordination cost that makes it slower than sequential LINQ on small collections. `WithDegreeOfParallelism` must always be set.

```csharp
// Required — bounded parallel LINQ transformation over a large in-memory collection
var totals = records
    .AsParallel()
    .WithDegreeOfParallelism(Environment.ProcessorCount)
    .Select(r => CalculateLineItemTotal(r))
    .ToList();
```

### 3.5 Pagination
<!-- STD-MARKER: performance.3.5 -->

Any repository operation that returns a collection of unbounded size must be paginated. No method may return an entire table or an unconstrained result set to the application layer. Both human developers and AI models must verify that every repository method returning a collection has an explicit page size limit or upper bound before the work slice is considered complete.

### 3.6 Caching
<!-- STD-MARKER: performance.3.6 -->

Data that is frequently read and rarely changes must be cached. Caching is not optional for this category of data — it is a performance requirement. Reference data, dropdown lists, configuration values, and any data set that is loaded repeatedly across requests without changing between loads must be served from cache rather than from a database round-trip on every request.

Full caching rules — including cache type selection, tenant isolation, key naming, expiration, invalidation strategies, serialization, resilience, and security constraints — are defined in [`GlobalCachingStandards.md`](../standards/GlobalCachingStandards.md).

### 3.7 Cancellation Tokens
<!-- STD-MARKER: performance.3.7 -->

Every async method must accept and forward a `CancellationToken`. Any async operation that accepts a `CancellationToken` parameter must have one passed to it. Cancellation tokens are a performance requirement because operations that ignore cancellation hold threads and resources after the caller has abandoned the request. They are also a security and reliability requirement — the full rule, including the top-level entry point exception, is defined in [`GlobalSecurityStandards.md`](../standards/GlobalSecurityStandards.md).

### 3.8 Timeouts
<!-- STD-MARKER: performance.3.8 -->

Every external I/O call must have an explicit timeout configured. A call with no timeout is not permitted — it will block indefinitely if the dependency is slow or unresponsive and must be corrected before the PR is approved. Both human developers and AI models must verify that every database call and HTTP client call in new or changed code has an explicit timeout before the work slice is considered complete.

**Database timeouts** must be configured via `CommandTimeout` on the command or connection. The value must come from application configuration — hardcoded timeout values are not permitted.

```csharp
// Required — explicit database timeout read from configuration, not hardcoded
_command.CommandTimeout = _options.DatabaseCommandTimeoutSeconds;
```

**HTTP client timeouts** must be configured via the `timeout` parameter on `IRequestProcessor` methods — not by setting `HttpClient.Timeout` directly. `IRequestProcessor` owns cancellation token forwarding for all outbound HTTP calls; per-request timeout is a specific form of cancellation and is managed by the library internally by composing the caller-supplied `CancellationToken` with the timeout deadline. Setting `HttpClient.Timeout` alongside `IRequestProcessor`'s timeout parameter creates two competing timeout mechanisms on the same request with no predictable winner and is a violation. Timeout values must come from application configuration.

```csharp
// Required — HTTP timeout supplied to IRequestProcessor; library composes the linked token source internally
var timeout = TimeSpan.FromSeconds(_options.ScannerRequestTimeoutSeconds);
return await _requestProcessor.PostAsync(client, request, cancellationToken, timeout);

// VIOLATION — sets a competing timeout directly on the HttpClient instance
httpClient.Timeout = TimeSpan.FromSeconds(30);
```

---

## 4. Thresholds Reference
<!-- STD-MARKER: performance.4 -->

Both human developers and AI models must use the thresholds in this table when writing or reviewing performance tests. A performance test that does not reference these thresholds is non-compliant and must be corrected before the PR is approved.

| Operation Category | Threshold |
|---|---|
| High-frequency service calls | < 50ms at p99 |
| Data transformation operations | < 100ms for 1,000 records |
| Repository operations | < 200ms at production-scale volume |
| Batch processing — per chunk (100 records) | < 500ms (placeholder — to be baselined) |
| Batch processing — full file (up to 500 records) | < 5 seconds (placeholder — to be baselined) |

Thresholds are verified by performance tests defined in [`GlobalTestingStandards.md`](../standards/GlobalTestingStandards.md#11-performance-tests). A performance test that does not use the thresholds in this table is non-compliant.

---

## 5. Compliance Verification
<!-- STD-MARKER: performance.5 -->

- [ ] All high-frequency service calls have a BenchmarkDotNet performance test.
- [ ] All data transformation operations have a BenchmarkDotNet performance test.
- [ ] All repository operations have a BenchmarkDotNet performance test.
- [ ] All performance tests use the thresholds defined in Section 4 of this file.
- [ ] No I/O-bound operation uses `.Result`, `.Wait()`, or `Task.Run` to wrap synchronous code.
- [ ] No repository operation produces an N+1 query pattern.
- [ ] All repository operations that return collections are paginated.
- [ ] All independent async operations that aggregate data from multiple sources use `Task.WhenAll`.
- [ ] All fixed sets of independent synchronous method calls use `Parallel.Invoke`.
- [ ] All CPU-bound parallel operations use `Parallel.ForEach` with `MaxDegreeOfParallelism` set.
- [ ] All async parallel collection operations use `Parallel.ForEachAsync` with `MaxDegreeOfParallelism` set.
- [ ] All parallel LINQ transformations use `AsParallel()` with `WithDegreeOfParallelism` set.
- [ ] Any parallel pattern used outside its defined scenario has a documented justification in the PR.
- [ ] All frequently read, rarely changed data is served from cache rather than a database round-trip — full caching compliance is verified against [`GlobalCachingStandards.md`](../standards/GlobalCachingStandards.md).
- [ ] Every async method accepts and forwards a `CancellationToken`.
- [ ] No async operation omits a `CancellationToken` except at a documented top-level entry point.
- [ ] Every external I/O call has an explicit timeout configured via application configuration.

---

## 6. Governance
<!-- STD-MARKER: performance.6 -->

This standard is owned by Troy Crowe. No changes to this file may be merged without Troy Crowe's explicit approval. Changes must be submitted as a pull request that includes a rationale comment explaining the reason for the update or deviation. Direct commits to `dev` or `main` are not permitted.
