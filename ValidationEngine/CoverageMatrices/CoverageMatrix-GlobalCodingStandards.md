# Coverage Matrix — GlobalCodingStandards.md

**Status:** Reviewed
**Sign Off:** Troy Crowe
**Sign Off Date:** 2026-08-14
**Revision Date:** 2026-08-14
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

**Applicability note:** Applies to any repository with C# source files. Nearly all rules in this file are checkable against a single changed `.cs` file via Roslyn syntax/semantic analysis; a handful require solution-wide or repository-history context (duplicate package detection, architectural design document lookup) and are marked Periodic or Manual-only accordingly.

---

### Section 2 — Application Architecture

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| coding.2.1 | `Program.cs` contains only DI/middleware wiring; no business or orchestration logic | Structure | Heuristic | Warning | Every-Commit (when `Program.cs` changes) | Roslyn-syntax: flag method bodies, loops, or business-domain type usage inside `Program.cs` beyond `builder.Services.*`/`app.Use*`/`app.Map*` call chains — implemented as `CODE001` (`ProgramCompositionOnlyAnalyzer`) | Existing |
| coding.2.2 | Concerns separated across layers (Controllers/Services/Repositories/Options/Models/Diagnostics/Infrastructure); no layer reaches across more than one boundary | Structure | Heuristic | Warning | Every-Commit | Roslyn-semantic: flag controller classes referencing repository types directly, or infrastructure classes referencing business-domain models — requires a folder/namespace convention mapping to layers — implemented as `CODE002` (`ControllerDirectRepositoryAccessAnalyzer`) | Existing |
| coding.2.3 | Every class has exactly one clearly named responsibility | Development | Manual-only | Manual-only-comment | Every-Commit | Requires subjective judgment about cohesion; a class-size/method-count heuristic could flag candidates but cannot definitively assert a single-responsibility violation | Missing |
| coding.2.4 | No speculative abstractions added without a present, demonstrable need | Development | Manual-only | Manual-only-comment | Every-Commit | Requires judgment about whether an existing component could have satisfied the need; not mechanically checkable | Missing |
| coding.2.5 | Interfaces kept small/purposeful; not created solely to wrap a single never-substituted concrete class | Development | Heuristic | Warning | Every-Commit | Roslyn-semantic: flag interfaces with only one implementing class in the solution and no test-double/mock usage found — likely wrapper-only interface — implemented as `CODE003` (`WrapperOnlyInterfaceAnalyzer`) | Existing |
| coding.2.6 | Authorization policies defined in dedicated files, not inline in `Program.cs`/controllers; named policies registered at entry point | Structure | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag `AddAuthorization` policy-builder lambdas defined inline in `Program.cs` or controller files rather than referencing a separate policy-provider class — implemented as `CODE004` (`InlineAuthorizationPolicyAnalyzer`) | Existing |
| coding.2.7 | Data access via ADO.NET + repository pattern + stored procedures only; no ORM, no inline SQL | Technology | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag `DbContext`/EF Core usage, or raw SQL string literals passed to `SqlCommand`/`ExecuteQuery`-style calls instead of a stored-procedure name — duplicates `GlobalDatabaseStandards.md` Section 5 / `GlobalSecurityStandards.md` Section 4 detection — implemented as `CODE005` (`NoOrmUsageAnalyzer`) | Existing |

---

### Section 3 — Coding Style

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| coding.3.1 | Repository-specific conventions followed before falling back to standard .NET conventions | Procedural | Manual-only | Manual-only-comment | Every-Commit | Requires knowledge of undocumented repository conventions not captured in any lintable ruleset; a `.editorconfig` diff-check could catch some cases but not all | Missing |
| coding.3.2 | Descriptive naming for all classes/methods/parameters/variables/files; no abstract or abbreviated names | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag identifiers under a minimum length threshold or matching a single-letter/abbreviation denylist (excluding conventional loop variables/LINQ lambdas) — implemented as `CODE006` (`NonDescriptiveNamingAnalyzer`) | Existing |
| coding.3.3 | Methods short with one clear purpose; no scroll-to-read methods | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag method bodies exceeding a configurable line-count threshold — implemented as `CODE007` (`ExcessiveMethodLengthAnalyzer`) | Existing |
| coding.3.4 | Guard clauses at top of every method with preconditions; fail-fast, no deep-nested precondition validation | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag methods where parameter-null/range checks appear after other statements, or where validation is nested more than one level deep — implemented as `CODE008` (`DeferredGuardClauseAnalyzer`) | Existing |
| coding.3.5 | Unmodified-after-construction DTOs implemented as immutable types (`init`-only properties or records) | Development | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag DTO-suffixed/Model-suffixed classes with mutable (`set`) properties and no post-construction mutation justification — implemented as `CODE009` (`MutableDtoAnalyzer`) | Existing |
| coding.3.6 | Object mapping performed exclusively in the target type's constructor, accepting the source object as a parameter | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag manual property-by-property mapping code outside a type's own constructor (e.g., in a service method or a separate mapper class) — implemented as `CODE010` (`MappingOutsideConstructorAnalyzer`) | Existing |
| coding.3.7 | Minimum required visibility for every member; no widening without a concrete present reason | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag `public`/`internal` members with no external callers found in the solution (candidate for narrowing) — implemented as `CODE011` (`NonMinimalVisibilityAnalyzer`) | Existing |
| coding.3.8 | Comments explain why, not what; no restating what code already says; rationale-bearing comments link to the specific knowledge-base entry instead of duplicating it inline | Development | Manual-only | Manual-only-comment | Every-Commit | Requires semantic judgment about comment content relative to code, and whether "why" content is duplicated elsewhere vs. linked; not reliably automatable | Missing |
| coding.3.9 | All `using` statements sorted; unused `using` statements removed before commit | Development | Binary | Hard-stop | Every-Commit | Roslyn-syntax/analyzer: standard IDE0005 (unused usings) + `.editorconfig` using-sort-order analyzer, already built into Roslyn/dotnet-format | Existing |
| coding.3.10 | No third-party library introduced without prior approval recorded in the architectural design document | Procedural | Manual-only | Manual-only-comment | Every-Commit (when `.csproj`/`package.json` changes) | Requires cross-referencing a new package reference against the repository's architectural design document — not automatable without that document being machine-readable | Missing |
| coding.3.11 | No new NuGet/npm package introduced without verifying it isn't already available in the solution | Structure | Heuristic | Warning | Every-Commit (when `.csproj`/`package.json` changes) | Solution-wide scan: compare newly added package name/capability against existing `PackageReference`/`dependencies` entries across all projects — requires solution-wide context, not single-file | Missing |
| coding.3.12 | All approved packages sourced from the centralized Azure Artifacts feed; no direct nuget.org/npmjs.com pulls | Infrastructure | Binary | Hard-stop | Every-Commit (when `NuGet.config`/`.npmrc` changes) | Config-scan: verify `NuGet.config`/`.npmrc` package-source entries point only to the organization's Azure Artifacts feed | Missing |
| coding.3.13 | Purpose-built lean models used for bounded-shape data; full domain entities not passed to narrow use cases | Development | Manual-only | Manual-only-comment | Every-Commit | Requires judgment about data-shape boundedness relative to the consuming use case; not mechanically checkable from a single diff | Missing |
| coding.3.13.1 | Extension methods used for transformations consumed in more than one place; grouped one class per domain type under `Extensions/` | Development | Heuristic | Warning | Every-Commit | Roslyn-semantic: flag near-identical transformation logic (same source type shape, same output shape) duplicated across more than one method/class instead of a shared extension method; flag extension methods not grouped in a single `{DomainType}Extensions` class under `Extensions/` — implemented as `CODE012` (`ScatteredExtensionMethodsAnalyzer`) | Existing |
| coding.3.14 | Constants and magic values — repeated string/numeric literals extracted to named constants | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag identical string/numeric literals appearing more than N times across a changed file or class (duplicates `coding.9` watchlist item) — implemented as `CODE013` (`RepeatedLiteralAnalyzer`) | Existing |

---

### Section 4 — SOLID Principles

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| coding.4.1 | Single Responsibility (SOLID) | Development | Manual-only | Manual-only-comment | Every-Commit | Duplicates coding.2.3 — same detection limitation (subjective cohesion judgment) | Missing |
| coding.4.2 | Open/Closed Principle | Development | Manual-only | Manual-only-comment | Every-Commit | Requires architectural judgment about extensibility design; not mechanically checkable | Missing |
| coding.4.3 | Interface Segregation | Development | Heuristic | Warning | Every-Commit | Duplicates coding.2.5 detection technique — covered by `CODE003` (`WrapperOnlyInterfaceAnalyzer`) | Existing |
| coding.4.4 | Dependency Inversion | Development | Heuristic | Warning | Every-Commit | Roslyn-semantic: flag concrete-class field/constructor-parameter types in service classes where an interface abstraction would be expected per the layer (Section 2.2) — implemented as `CODE014` (`ConcreteDependencyAnalyzer`) | Existing |
| coding.4.5 | Dependencies registered in DI container at entry point; no service locator pattern inside services; constructor injection only | Structure | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag `IServiceProvider.GetService`/`GetRequiredService` calls made inside non-entry-point classes (service-locator anti-pattern) — implemented as `CODE015` (`NoServiceLocatorAnalyzer`) | Existing |

---

### Section 5 — Functional Programming

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| coding.5 | Apply functional patterns (pure functions, immutable data, composition) where they fit naturally alongside SOLID; do not force them where they conflict with existing structure | Development | Manual-only | Manual-only-comment | Every-Commit | Requires judgment about where a functional pattern is a natural fit versus forced; not mechanically checkable | Missing |

---

### Section 6 — Testing Requirement (Cross-Reference)

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| coding.6 | Automated tests required for all new/changed public behavior before a work slice is marked complete | Development | Heuristic | Hard-stop | Every-Commit | Delegates to `GlobalTestingStandards.md` for full rules; this row's own check is: does the change set include a corresponding test-project change for new/changed public members — requires solution-wide test-project mapping. **Assessed and rejected as a `GlobalStandards.Analyzers.Coding` candidate (see `AnalyzerRolloutPlan.md` STD-016)** — a single-compilation `DiagnosticAnalyzer` cannot see git-diff/change-set context or a separate test-project's compilation; routed to Custom ValidationEngine instead | Acknowledged-future-work |

---

### Section 7 — Error Handling

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| coding.7 | Global error handler registered as outermost pipeline layer (cross-reference to `GlobalSolutionStructureStandards.md`) | Structure | Binary | Hard-stop | Every-Commit (when `Program.cs` changes) | Roslyn-syntax: verify `UseExceptionHandler`/global exception-handling middleware registration exists and is the first middleware in the pipeline — duplicates `GlobalSolutionStructureStandards.md` Section 12 detection — implemented as `CODE016` (`GlobalErrorHandlerAnalyzer`) | Existing |
| coding.7.1 | Most precise exception type used; `Exception`/`ApplicationException` not thrown directly | Development | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag `throw new Exception(...)`/`throw new ApplicationException(...)` — implemented as `CODE017` (`ImpreciseExceptionTypeAnalyzer`) | Existing |
| coding.7.2 | All inputs validated at entry points using guard clauses; not deferred to deep implementation code | Development | Heuristic | Warning | Every-Commit | Duplicates coding.3.4 detection, scoped specifically to controller actions/service entry methods/job entry methods — covered by `CODE008` (`DeferredGuardClauseAnalyzer`) | Existing |
| coding.7.3 | No swallowed exceptions; every caught exception logged or rethrown | Development | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag empty `catch` blocks or catch blocks with no `_logger.Log*` call and no `throw`/`throw;` statement — implemented as `CODE018` (`SwallowedExceptionAnalyzer`) | Existing |
| coding.7.4 | API error responses formatted via `ProblemDetails` middleware or global exception handler; not inline in controllers | Structure | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag controller action methods manually constructing error-response objects instead of relying on `ProblemDetails`/global handler — implemented as `CODE019` (`InlineErrorResponseAnalyzer`) | Existing |
| coding.7.5 | No raw stack traces surfaced to clients on unhandled exceptions | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag `ex.ToString()`/`ex.StackTrace` values returned directly in an API response body — implemented as `CODE020` (`ExceptionDetailsExposedAnalyzer`) | Existing |
| coding.7.6 | `throw;` used to preserve stack when rethrowing; `throw ex;` not used | Development | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag `throw ex;` where `ex` is the caught exception variable (standard analyzer rule, CA2200) | Existing |
| coding.7.7 | User-facing error messages are clear/actionable and do not expose internal details, exception messages, or stack traces | Development | Heuristic | Warning | Every-Commit | Duplicates coding.7.5 detection; additionally flag raw `ex.Message` values returned directly in an API response body or view model — covered by `CODE020` (`ExceptionDetailsExposedAnalyzer`) | Existing |

---

### Section 8 — Async/Await Rules

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| coding.8.1 | Every `async` method ends with `Async` suffix | Development | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag `async` methods returning `Task`/`Task<T>` without an `Async`-suffixed name (standard analyzer rule, VSTHRD200/CA1849-adjacent) | Existing |
| coding.8.2 | No blocking calls on async code (`.Result`, `.Wait()`, `.GetAwaiter().GetResult()`, `Thread.Sleep` in async code) | Development | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag the specific prohibited call patterns (standard analyzer rule, VSTHRD002/VSTHRD105-adjacent) | Existing |
| coding.8.3 | No `async void` except required event handlers; those must wrap body in try/catch that logs all exceptions | Development | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag `async void` methods not matching an event-handler signature pattern, or event handlers lacking a try/catch with logging — implemented as `CODE021` (`AsyncVoidAnalyzer`) | Existing |
| coding.8.4 | Every `async` method accepts and forwards `CancellationToken` to every awaited call that accepts one; not substituted with `CancellationToken.None` except at top-level entry | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag async methods missing a `CancellationToken` parameter, or awaited calls passing `CancellationToken.None`/no token where an overload accepting one exists — duplicates a check already likely needed for `GlobalSecurityStandards.md` — implemented as `CODE022` (`CancellationTokenForwardingAnalyzer`) | Existing |
| coding.8.5 | Library code uses `ConfigureAwait(false)` on all `await` calls; application code does not require it | Development | Binary | Hard-stop | Every-Commit (class-library projects only) | Roslyn-syntax: flag `await` expressions in class-library projects missing `.ConfigureAwait(false)`; scope check to project type via `.csproj` `OutputType`/project-reference detection — implemented as `CODE023` (`ConfigureAwaitAnalyzer`) | Existing |

---

### Section 9 — Code Smell Watchlist

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| coding.9 | Checklist restating coding.2.1, 2.2, 2.3, 2.4, 3.7, 3.14, 8.1, 8.2, 8.3, 8.4 as a pre-close review list | Procedural | N/A (Aggregate) | N/A | N/A | Pure aggregation/index of the referenced sections; automation should evaluate the underlying markers directly rather than this rollup row | Missing |

---

### Section 10 — Compliance Verification (Excluded — content not read in this pass; largely restates Sections 2–9 as a checklist per established pattern)

### Section 11 — Governance (Excluded)

`coding.11` (ownership/PR-approval boilerplate) is excluded — not code-checkable, procedural ownership statement only.

---

**Notes:**
1. This file has the highest proportion of **Existing** (already-covered-by-standard-Roslyn-analyzer) rules of any matrix drafted so far: coding.3.9 (unused/sorted usings), coding.7.6 (`throw ex;`), coding.8.1 (Async suffix), coding.8.2 (blocking calls) all map directly to well-known .NET analyzer diagnostics (IDE0005, CA2200, and Microsoft.VisualStudio.Threading.Analyzers rules). These should be wired up as enabled analyzers/`.editorconfig` severities rather than custom detection logic.
2. Several rules are explicitly duplicated within this file itself: coding.2.3/coding.4.1 (Single Responsibility), coding.2.5/coding.4.3 (Interface Segregation), coding.3.4/coding.7.2 (guard clauses), coding.7.5/coding.7.7 (no internal-detail leakage). Flagged as duplicate coverage rather than merged, matching the pattern already used in prior matrices.
3. coding.2.7 (ADO.NET/no-ORM/no-inline-SQL) and coding.7 (global error handler) both cross-reference other standards files (`GlobalDatabaseStandards.md`, `GlobalSecurityStandards.md`, `GlobalSolutionStructureStandards.md`) for full detail — detection technique should be built once and shared across matrices rather than duplicated per file.
4. A meaningful cluster of rules (coding.2.3, 2.4, 3.1, 3.8, 3.10, 3.13, 4.1, 4.2, 5) require subjective architectural or semantic judgment that cannot be reliably automated — these are Manual-only and expected to remain PR-review-dependent regardless of tooling investment.
5. coding.6 (Testing Requirement) is a cross-reference row; its full rule set lives in `GlobalTestingStandards.md` and will be covered in that file's own matrix (Step 4 of this batch).

