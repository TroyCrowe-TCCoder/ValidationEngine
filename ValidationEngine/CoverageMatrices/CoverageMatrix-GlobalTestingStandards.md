# Coverage Matrix — GlobalTestingStandards.md

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
| Confidence | Binary (deterministic pass/fail) / Heuristic (risk-flag, needs human confirmation) / Manual-only (no automation path). |
| Severity | Hard-stop / Warning / Manual-only-comment (3-tier enforcement model). |
| Frequency | Every-Commit (validated against the files touched in the current change set) / Periodic (validated on a recurring, configurable cadence). |
| Technique | Short detection approach. |
| Status | Missing / Existing / Acknowledged-future-work. |

**Applicability note:** Applies to xUnit test projects (`*.Tests.csproj`) and their production project counterparts. This file has an explicit two-phase enforcement model: **Phase 1** (Sections 2–7, 9–10, 12–14's Phase 1 checklist) is enforced now; **Phase 2** (Section 8 Workflow Tests, Section 11 Performance Tests, and the Phase 2 checklist items) applies only to repositories with a dedicated automated environment and an explicit repository-local enforcement record activating them. Detection technique is overwhelmingly Roslyn-syntax against test-project files, with a smaller Infrastructure-only slice for Phase 2 environment provisioning.

---

### Section 2 — Test Project Structure

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| testing.2.1 | xUnit is the required test framework; no other framework may be introduced | Technology | Binary | Hard-stop | Every-Commit (when `.csproj` changes) | Config-scan: flag `PackageReference` to `NUnit`/`MSTest`/other test-framework packages instead of `xunit` | Missing |
| testing.2.2 | Test code placed in a dedicated `[ProjectName].Tests` project; never added to a production project | Structure | Binary | Hard-stop | Every-Commit | File-path scan: flag `[Fact]`/`[Theory]`-attributed test methods or files referencing `Xunit` located outside a project whose name ends in `.Tests` — duplicates `GlobalSolutionStructureStandards.md` Section 4 test-project-placement detection | Missing |
| testing.2.3 | Test files mirror the production project's folder structure; `Fakes/` folder holds reusable fake implementations, not embedded per-test | Structure | Heuristic | Warning | Every-Commit | File-path scan: compare test-project folder structure against the production project's folder structure for a 1:1 mapping; flag fake implementation classes defined inline inside a test file instead of under `Fakes/` | Missing |

---

### Section 3 — Naming Conventions

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| testing.3.1 | Test classes/files named `[SubjectName]Tests` | Development | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag test class names not matching the `{SubjectName}Tests` pattern relative to the production type under test | Missing |
| testing.3.2 | Test methods named `[MethodUnderTest]_[Scenario]_[ExpectedResult]` | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag `[Fact]`/`[Theory]` method names not matching the three-part underscore-separated naming pattern (exact scenario/expected-result wording cannot be validated, only the structural shape) | Missing |
| testing.3.3 | Fake implementations named `Fake[InterfaceName]` with `I` prefix removed | Development | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag classes implementing an interface `I{Name}` for test-double purposes whose class name is not `Fake{Name}` | Missing |
| testing.3.4 | Observability-only test classes named `[TypeName]ObservabilityTests`; methods named `[MethodOrOperation]When[Dependency]ThrowsThenRethrowsAndLogsError`; must be a separate class from the subject's ordinary `[SubjectName]Tests`, not a replacement for Sections 3.1/3.2 | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag test classes/methods whose sole assertions target logger-mock invocations and rethrown-exception behavior but are named using the ordinary `Tests`/`[MethodUnderTest]_[Scenario]_[ExpectedResult]` pattern instead of the observability-specific pattern | Missing |
| testing.3.5 | Guard-clause test methods named `[Service][Method]When[Condition]Then[ExpectedGuardException]`, distinct from Section 3.2's general pattern | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag `Assert.Throws<ArgumentNullException>`/similar guard-verification tests named using the ordinary pattern instead of the guard-specific pattern | Missing |

---

### Section 4 — Test Categories and Traits

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| testing.4 | Every test method must carry at least one `[Trait("Category", ...)]` using only the defined values (`Unit`, `Contract` — Phase 1; `Workflow`, `Regression`, `Performance` — Phase 2); no test may have zero traits or an undefined trait value; CI runs `Unit` on every PR build, other categories only in activated stages | Development | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag `[Fact]`/`[Theory]` methods with no `[Trait("Category", ...)]` attribute, or a trait value outside the defined allowlist | Missing |

---

### Section 5 — Arrange-Act-Assert

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| testing.5 | Every test method follows Arrange-Act-Assert structure; more than one Act or Assert requires splitting into separate test methods; each test verifies exactly one behavior | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: verify `// Arrange`/`// Act`/`// Assert` comment markers are present in that order; flag test methods containing more than one distinct "act" invocation of the subject-under-test method, or more than one logically distinct `Assert.*` group | Missing |

---

### Section 6 — Test Independence and Workflow Tests

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| testing.6.1 | Unit tests fully independent — no shared state, no execution-order dependency, no reliance on system clock/random values/external services; each test sets up everything in its own Arrange block | Development | Heuristic | Hard-stop | Every-Commit | Roslyn-semantic: flag test classes with mutable static/shared fields referenced across multiple `[Fact]` methods; flag direct use of `DateTime.Now`/`Random` (non-seeded) instead of an injected/fixed clock or seed within `Unit`-tagged tests | Missing |
| testing.6.2 | Workflow tests may carry ordered state within a single test method (steps depend on prior step outcome) but must not depend on state from a separate test method/class | Development | Heuristic | Warning | Every-Commit | Roslyn-semantic: flag test classes where a `[Fact]`/`[Theory]` method reads or writes a mutable instance/static field also written by another test method in the same class without a shared, intentional ordering attribute (e.g. no `[Collection]`/explicit sequencing construct) — catches the common cross-method leakage pattern; whether flagged intra-method ordering is legitimate still requires PR-review judgment | Missing |

---

### Section 7 — Unit Tests

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| testing.7.1 | Every public method requires dedicated tests for: happy path, sad path, boundary conditions, edge cases, each guard clause, each distinct outcome, exception scenarios | Development | Manual-only | Manual-only-comment | Every-Commit | Requires semantic coverage-gap analysis (which scenarios exist vs. which are missing per method) beyond simple code-coverage percentage; a code-coverage-percentage gate is a partial, cheap proxy but cannot assert scenario completeness | Missing |
| testing.7.2 | `Assert.Throws<T>` used with the most specific exception type available, not the base `Exception` type | Development | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag `Assert.Throws<Exception>` (base type) calls in test code | Missing |
| testing.7.3 | Async subjects tested with `async Task` test methods; `.Result`/`.Wait()` not used in test code (content truncated at read boundary but consistent with the Section 14 checklist item) | Development | Binary | Hard-stop | Every-Commit | Duplicates `GlobalCodingStandards.md` coding.8.2 blocking-call detection, scoped to test-project files; Roslyn-syntax: flag `.Result`/`.Wait()` on `Task`-returning subject-under-test calls inside test methods, and flag test methods with async subjects not declared `async Task` | Missing |

---

### Section 8 — Workflow Tests (Phase 2 — not yet enforced except where repository-local enforcement record activates it)

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| testing.8.1 | Workflow tests require a dedicated environment, provisioned/deprovisioned automatically via infrastructure-as-code within the pipeline; no persistent manually-managed environment; five-step required pipeline process (provision, deploy, test, deprovision regardless of outcome, gate promotion) | Infrastructure | Manual-only | Manual-only-comment (Acknowledged-future-work until repository activates Phase 2) | Periodic (once Phase 2 activated) | Requires inspecting live pipeline IaC/stage configuration — not source-diffable; gated entirely behind repository-local Phase 2 activation record | Acknowledged-future-work |
| testing.8.2 | Workflow tests set up and clean up all required test data as part of the test itself; no dependency on pre-existing environment data; teardown occurs regardless of pass/fail | Development | Heuristic | Manual-only-comment (Acknowledged-future-work until repository activates Phase 2) | Periodic (once Phase 2 activated) | Roslyn-syntax: flag `Workflow`-tagged test methods with no corresponding teardown/cleanup logic (e.g., missing `IDisposable`/finally-block cleanup) — deferred until Phase 2 activation | Acknowledged-future-work |

---

### Section 9 — Contract Tests

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| testing.9 | Contract tests (Phase 1, tagged `Contract`) verify observable API contract (status codes, response shape, rejection behavior) at the application boundary — not business logic in isolation, not real infrastructure | Development | Heuristic | Warning | Every-Commit | Roslyn-semantic: verify every controller action has at least one corresponding `[Trait("Category","Contract")]` test in the `Contracts/` folder | Missing |
| testing.9 (scope sub-rule) | Contract tests must cover: happy-path status code, 401 unauthenticated, 403 forbidden (wrong role/client), 400 structurally invalid input, 404 not found, other documented endpoint-specific contract behavior; must use `WebApplicationFactory<T>` in-process, never a deployed environment; real infrastructure dependencies replaced with fakes/stubs via `WebApplicationFactory` configuration; files placed in `Contracts/` mirroring controller structure | Development | Heuristic | Hard-stop | Every-Commit | Roslyn-semantic: for each controller action, verify presence of Contract-tagged tests asserting each of the five required status-code scenarios; flag Contract tests not using `WebApplicationFactory<T>`/referencing a live HTTP endpoint URL; file-path scan for `Contracts/` folder mirroring | Missing |

---

### Section 10 — Regression Tests

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| testing.10.1 | A regression test must be added when a defect is fixed and can be reproduced automatically; must reproduce the defect scenario, verify corrected behavior, be tagged `Regression`, placed with the matching test type | Procedural | Heuristic | Warning | Every-Commit (on bug-fix PRs) | ADO-integration check: for a PR linked to a work item of type "Bug", require the diff to include at least one new or modified test carrying `[Trait("Category","Regression")]` — mechanically enforceable once work-item linkage is available; the deeper judgment ("does the test actually reproduce the defect and can it be automated") remains manual and is not asserted by this check | Missing |
| testing.10.2 | `Regression` is a cross-cutting trait added in addition to the primary category trait (`Unit`, `Contract`, or `Workflow` when Phase 2 active) — never a standalone test style | Development | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag `[Trait("Category","Regression")]` tests with no additional primary-category trait (`Unit`/`Contract`/`Workflow`) also present | Missing |
| testing.10.3 | Regression tests must isolate the exact prior-failure boundary; a broad happy-path test alone is not a sufficient regression guard | Development | Manual-only | Manual-only-comment | Every-Commit | Requires judgment about whether the test actually isolates the historical defect condition — not mechanically checkable | Missing |

---

### Section 11 — Performance Tests (Phase 2 — not yet enforced except where repository-local enforcement record activates it)

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| testing.11 | Performance tests (tagged `Performance`) required for high-frequency service calls, data transformation operations, repository operations; must use BenchmarkDotNet, not a raw `Stopwatch` single-run; must not run on every PR build | Development | Heuristic | Manual-only-comment (Acknowledged-future-work until repository activates Phase 2) | Periodic (once Phase 2 activated) | Duplicates `GlobalPerformanceStandards.md` Section 2 detection exactly (same three operation categories, same BenchmarkDotNet requirement); Roslyn-syntax: flag `Stopwatch`-based timing assertions in test code as a non-compliant substitute — deferred until Phase 2 activation | Acknowledged-future-work |

---

### Section 12 — Test Doubles

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| testing.12.1 | A fake implements only the interface members the tests require, using simple in-memory storage (e.g., `List<T>`) — not a replica of the real implementation's infrastructure concerns (ADO.NET, stored procedures, connection management) | Development | Heuristic | Warning | Every-Commit | Roslyn-semantic: flag `Fake*` classes referencing `SqlConnection`/`SqlCommand`/`HttpClient` or other real-infrastructure types instead of simple in-memory collections | Missing |
| testing.12.2 | Test data belongs in the test's Arrange block, not baked into the fake; the fake is the reusable piece, data is test-specific | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag `Fake*` classes in the `Fakes/` folder with a non-empty constructor or field initializer that pre-populates domain data | Missing |
| testing.12.3 | Mocks (NSubstitute, the approved library) appropriate only to verify a specific interaction occurred (call count/arguments); must not test internal implementation details | Technology | Heuristic | Warning | Every-Commit | Config-scan: flag mocking libraries other than `NSubstitute` (e.g., `Moq`) referenced in the test project; Roslyn-syntax: flag `Substitute.For<T>()` usage with no corresponding `.Received(...)` interaction-verification assertion, suggesting a fake would have sufficed | Missing |
| testing.12.4 | Fakes preferred by default; mocks used only when interaction verification is specifically needed; a test using a mock where a fake would suffice must be refactored | Development | Heuristic | Warning | Every-Commit | Duplicates testing.12.3 detection — same underlying check (mock-usage-with-no-interaction-verification flags both markers) | Missing |

---

### Section 13 — CI Gate

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| testing.13 | All unit tests must pass in CI before a PR may be merged; workflow/regression/performance tests run only in repository-specific activated stages outside the PR build | Infrastructure | Binary | Hard-stop | Every-Commit | Pipeline-gate check: verify the PR-validation pipeline stage fails the build on any `Unit`-tagged test failure — this is an ADO pipeline configuration fact, not a source-diffable rule, though the underlying "tests pass" signal comes directly from the `dotnet test` exit code already surfaced in CI | Existing (standard `dotnet test`/xUnit CI integration; already a conventional build-gate pattern) |

---

### Section 14 — Compliance Verification (Excluded — restates Sections 2–13 as a Phase 1/Phase 2 checklist per established pattern; no independent detection needed)

### Section 15 — Governance (Excluded)

`testing.15` (ownership/PR-approval boilerplate) is excluded — not code-checkable, procedural ownership statement only.

---

**Notes:**
1. This file has the **most granular naming-convention rule set** seen across all matrices so far (Section 3: five distinct naming patterns for test classes/methods depending on whether they test ordinary behavior, observability/rethrow-and-log behavior, or guard clauses). All five are structurally checkable via Roslyn-syntax regex against method/class names, though the "does the name accurately describe the scenario" judgment (e.g., in testing.3.2) has an inherent Heuristic ceiling — the *shape* of the name is checkable, its *semantic accuracy* is not.
2. Consistent with `GlobalPerformanceStandards.md`, this file has an explicit two-phase enforcement model. Section 8 (Workflow Tests) and Section 11 (Performance Tests) are marked **Acknowledged-future-work**, matching the same treatment given to `GlobalPerformanceStandards.md` Section 2 — both defer to a repository-local Phase 2 activation record rather than running unconditionally. testing.11 is also an explicit **exact duplicate** of `GlobalPerformanceStandards.md` performance.2.1–2.4 (same three operation categories, same BenchmarkDotNet requirement) — worth consolidating into one shared rule definition referenced by both files' markers.
3. testing.7.1 (required coverage scenarios per public method) is the closest this file comes to a code-coverage-percentage-style gate, but it is explicitly **Manual-only** — the rule is about scenario completeness (happy/sad/boundary/edge/guard/outcome/exception), not raw line/branch coverage percentage. A coverage-percentage tool (e.g., Coverlet) could serve as a cheap proxy/warning signal but cannot assert this rule is actually satisfied.
4. testing.2.2/2.3 duplicate detection already expected in `GlobalSolutionStructureStandards.md` Section 4 (test project placement) — flagged for the shared-detection-library consolidation effort noted in prior matrices.
5. testing.13 (CI gate) is the second rule across all matrices (after `GlobalCodingStandards.md` coding.3.9/coding.7.6/coding.8.1/coding.8.2) marked **Existing** — `dotnet test` failing the build on any failed test is a long-standing, already-implemented CI convention rather than something requiring new tooling.

