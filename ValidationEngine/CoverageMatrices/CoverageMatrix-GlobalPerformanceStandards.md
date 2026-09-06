# Coverage Matrix — GlobalPerformanceStandards.md

**Status:** Reviewed
**Sign Off:** Troy Crowe
**Sign Off Date:** 2026-08-14
**Revision Date:**
**Columns**

| Column | Meaning |
|---|---|
| STD-MARKER | Rule id from the standards file. |
| Rule Summary | One-line paraphrase of the rule. |
| Category | Structure / Technology / Development / Procedural / Infrastructure. |
| Confidence | Binary (deterministic pass/fail) / Heuristic (risk-flag, needs human confirmation) / Manual-only (no automation path). |
| Severity | Hard-stop / Warning / Manual-only-comment (3-tier enforcement model). |
| Frequency | Every-Commit (validated against the files touched in the current change set) / Periodic (validated on a recurring, configurable cadence). |
| Technique | Short detection approach. |
| Status | Missing / Existing / Acknowledged-future-work. |

**Applicability note:** Applies to any repository with C# source files. Section 2 (performance-test-category classification and thresholds) is explicitly gated as a **Phase 2 enforcement requirement** per `GlobalTestingStandards.md` — the categories/thresholds are active reference data now, but the "must have a test before work is complete" gate is not yet enforced. Section 3 (implementation requirements) is enforceable today and is almost entirely Roslyn-checkable from a single changed file.

---

### Section 2 — Operation Categories Requiring Performance Tests (Phase 2 — not yet enforced)

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| performance.2.1 | High-frequency service calls (auth checks, permission lookups, cache reads, per-request middleware) require a BenchmarkDotNet test; threshold < 50ms p99 | Development | Heuristic | Manual-only-comment (until Phase 2 activation) | Periodic (suggested default: per-release, once Phase 2 is active) | Roslyn-semantic: classify methods called from middleware/auth-filter pipelines as high-frequency and verify a corresponding `[Benchmark]`-attributed test class exists — deferred until Phase 2 per the standard's own gating note | Acknowledged-future-work |
| performance.2.2 | Data transformation operations (mapping/projection/serialization over collections) require a BenchmarkDotNet test; threshold < 100ms for 1,000 records | Development | Heuristic | Manual-only-comment (until Phase 2 activation) | Periodic (suggested default: per-release, once Phase 2 is active) | Roslyn-semantic: classify extension methods/LINQ projections operating on `IEnumerable<T>` as transformation operations and verify a corresponding benchmark test exists — deferred until Phase 2 | Acknowledged-future-work |
| performance.2.3 | Repository operations (queries/commands on large data sets) require a BenchmarkDotNet test; threshold < 200ms at production-scale volume | Development | Heuristic | Manual-only-comment (until Phase 2 activation) | Periodic (suggested default: per-release, once Phase 2 is active) | Roslyn-semantic: classify repository methods with joins/pagination/bulk operations and verify a corresponding benchmark test exists — deferred until Phase 2 | Acknowledged-future-work |
| performance.2.4 | Batch processing operations (file upload/bulk import, chunked at 100 records) require a BenchmarkDotNet test; placeholder thresholds (< 500ms/chunk, < 5s/file up to 500 records) pending baselining before Phase 2 | Development | Heuristic | Manual-only-comment (until Phase 2 activation) | Periodic (suggested default: per-release, once Phase 2 is active) | Roslyn-semantic: classify bulk-import/batch-processing methods and verify a corresponding benchmark test exists — deferred until Phase 2; thresholds themselves are explicitly placeholder and not yet locked in | Acknowledged-future-work |

---

### Section 3 — Implementation Requirements

**Note:** `performance.3.1` (async/non-blocking rule) was removed as an exact duplicate of `GlobalCodingStandards.md` coding.8.2 (Prohibited Blocking Calls) and no longer appears in the standards file or this matrix. `performance.3.6` (caching rule) was removed as a duplicate of `GlobalCachingStandards.md` and no longer appears in the standards file or this matrix.

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| performance.3.2
| performance.3.3 | Repository operations must not produce N+1 query patterns; one-round-trip-per-collection-item must be refactored to a single set-based or batched query | Development | Heuristic | Hard-stop | Every-Commit | Roslyn-semantic: flag `foreach`/LINQ loops that call an async repository method once per iteration (a query-per-item call site) | Missing |
| performance.3.4 | Independent operations must run in parallel per the decision-rule table: `Task.WhenAll` for a fixed set of independent async I/O-bound methods; `Parallel.Invoke` for a fixed set of independent sync CPU-bound methods; `Parallel.ForEach` (with `MaxDegreeOfParallelism`) for the same sync method per collection item; `Parallel.ForEachAsync` (with `MaxDegreeOfParallelism`) for the same async method per collection item; `AsParallel()` (with `WithDegreeOfParallelism`) for PLINQ transformations over large in-memory collections; any deviation requires a documented PR justification | Development | Heuristic | Hard-stop | Every-Commit | Roslyn-syntax: flag sequential `await` chains on independent async calls with no data dependency between them (candidate for `Task.WhenAll`); flag `Parallel.ForEach`/`Parallel.ForEachAsync`/`AsParallel()` usage missing the required `MaxDegreeOfParallelism`/`WithDegreeOfParallelism` option | Missing |
| performance.3.5 | Repository operations returning unbounded collections must be paginated; no method may return an entire table/unconstrained result set | Development | Heuristic | Hard-stop | Every-Commit | Roslyn-semantic: flag repository methods returning `IEnumerable<T>`/`IReadOnlyList<T>` with no page-size/limit parameter and no corresponding paged stored-procedure call pattern — cross-references `GlobalDatabaseStandards.md` database.6.1 paging requirement | Missing |
| performance.3.7
| performance.3.8 | Every external I/O call (database, HTTP client) must have an explicit configured timeout; no call may block indefinitely | Development | Heuristic | Hard-stop | Every-Commit | Roslyn-syntax: flag `HttpClient`/`SqlCommand` usage with no `Timeout`/`CommandTimeout` configuration present, or a client constructed without a timeout policy — duplicates `GlobalDatabaseStandards.md` database.5.2's `CommandTimeout`-from-configuration check for the SQL case, and extends the same pattern to `HttpClientManager` calls | Missing |

---

### Section 4 — Thresholds Reference (Excluded — pure data table restating Section 2 thresholds; no independent rule content)

### Section 5 — Compliance Verification (Excluded — restates Sections 2–3 as a checklist per established pattern)

### Section 6 — Governance (Excluded)

`performance.6` (ownership/PR-approval boilerplate) is excluded — not code-checkable, procedural ownership statement only.

---

**Notes:**
1. This is the first matrix where an entire top-level section (Section 2 — the BenchmarkDotNet test-coverage requirement) is explicitly and deliberately **not yet enforced** by the standard itself ("Phase 2 enforcement requirement"). All four Section 2 rows are marked **Acknowledged-future-work** rather than Missing, since building detection for them now would be premature — the standard's own text says the "must have a test" gate activates later. The categories and thresholds remain useful now as reference data for anyone writing a benchmark voluntarily.
2. Section 3 originally contained two rules that duplicated detection already captured elsewhere and have since been removed from the standards file: performance.3.1 duplicated `GlobalCodingStandards.md` coding.8.2 (blocking calls); performance.3.6 duplicated the full `GlobalCachingStandards.md` matrix. performance.3.7 remains and duplicates both `GlobalCodingStandards.md` coding.8.4 and `GlobalSecurityStandards.md` security.7.1 — this is the **third** independent statement of the CancellationToken-forwarding rule across three different standards files, reinforcing the case for a single shared detection rule referenced by marker ID everywhere it appears.
3. performance.3.2 (avoid unnecessary allocations) is the one rule in this file that is fundamentally a measurement/profiling concern rather than a static-analysis concern — "when the allocation cost is measurable" cannot be determined by reading source code alone. This is analogous to performance work generally requiring the Profiler Agent rather than a static rule engine; flagged Manual-only for that reason.
4. performance.3.4's parallel-execution decision-rule table is the most detailed implementation guidance in the file and the best candidate for a genuinely useful custom Roslyn analyzer, since the five patterns (`Task.WhenAll`, `Parallel.Invoke`, `Parallel.ForEach`, `Parallel.ForEachAsync`, `AsParallel()`) each have a distinct, recognizable call-site shape and a specific required option (`MaxDegreeOfParallelism`/`WithDegreeOfParallelism`) that is easy to check is present or absent.
5. performance.3.5 (pagination) and performance.3.8 (timeouts) both have a natural cross-reference into `GlobalDatabaseStandards.md` (database.6.1's paging requirement and database.5.2's `CommandTimeout` requirement respectively) — worth noting for the shared detection-rule library but not merged here since each standards file states its own version of the rule independently.

