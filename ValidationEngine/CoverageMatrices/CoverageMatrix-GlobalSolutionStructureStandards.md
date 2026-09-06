# Coverage Matrix — GlobalSolutionStructureStandards.md

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

**Applicability note:** Applies to repository/solution/project-level structure across .NET solutions, database projects, shared-library repositories, and React applications. Detection here is dominated by **file-path/folder-structure scans** and **naming-convention regex** rather than Roslyn semantic analysis, since the rules govern where things live and what they're called, not code behavior. Section 10 (Azure Functions) is explicitly deferred — no Functions projects exist yet.

---

### Section 2 — Visual Studio Solution Folder Structure

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| solution-structure.2 | Solution file at repository root, one subdirectory per project, no project nested inside another project directory | Structure | Binary | Hard-stop | Every-Commit | File-path scan: verify `.sln`/`.slnx` is at repo root; verify each `.csproj` has exactly one top-level directory with no `.csproj` nested inside another project's folder tree | Missing |
| solution-structure.2.1 | Root-level `Standards/` folder reserved for repository-local deviation files only; **every consuming repository is prohibited from containing any copy, duplicate, mirror, or restatement of a global standards file** (the sole exclusion is the `GlobalStandards` repository itself, since that is where the canonical files natively live — not a permitted exception to duplicate elsewhere); any duplicate found in a consuming repository must be deleted immediately upon discovery; `copilot-instructions.md` must link to the root governance standard first | Procedural | Heuristic | Hard-stop | Every-Commit | File-path scan: verify `Standards/` folder naming and PascalCase compliance; text-diff/similarity scan comparing any file under a consuming repo's docs folders against canonical `GlobalStandards/Docs/Standards/*.md` content to detect duplication; verify `copilot-instructions.md` contains a link to the governance standard as its first reference | Missing |
| solution-structure.2.2 | Root-level `Working/` folder holds session carry-over files only, is `.gitignore`d, not a scratch dump; stale/completed files must be deleted immediately; optional `Working/Backlog.md` index follows the same retention rule; every `Working/` file must declare its retention state via a `Status:` metadata line (e.g., `Active`, `In Review`, `Approved`, `Stale — pending deletion`), which must be updated as state changes and any `Stale`-marked file must be deleted | Procedural | Heuristic | Warning | Periodic (suggested default: per-session or weekly) | File-path scan: verify `Working/` is listed in `.gitignore`; content scan: verify each file under `Working/` contains a `Status:` metadata line near the top and flag files missing it; flag any file explicitly marked `Stale` as a deletion candidate. Whether a non-`Stale` status is still accurate (i.e., genuinely "actively carrying work forward") remains a Heuristic/human judgment call, but the presence, format, and `Stale`-flagging of the status line are now mechanically checkable | Missing |
| solution-structure.2.3 | Scripts placed in exactly one of three mutually exclusive locations based on invocation: `.azure-pipelines/scripts/` (referenced by pipeline YAML via external file path), `Tools/` (local developer scripts, never pipeline-referenced), `Working/` (temporary session files) | Infrastructure | Heuristic | Warning | Every-Commit (when script files change) | Cross-reference scan: verify scripts under `.azure-pipelines/scripts/` are referenced by at least one pipeline YAML `File:` path; verify scripts under `Tools/` have no pipeline YAML reference | Missing |

---

### Section 3 — Naming Conventions

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| solution-structure.3.1 | Single-project repositories: repository name, solution name, and project name must all match exactly | Structure | Binary | Hard-stop | Every-Commit | File-path scan: compare repository name (from ADO/git remote), `.sln` base name, and `.csproj` base name for exact match when only one project exists | Missing |
| solution-structure.3.2 | Application layers (controllers, models, services, etc.) implemented as folders within a single project following the domain-design folder layout (`Authorizations/`, `Controllers/`, `Extensions/`, `Interfaces/`, `Models/`, `Options/`, `Repositories/`, `Services/`, `Utilities/` for API; Web adds MVC folders, omits `Repositories/`); `Utilities/` is the default home until a category is large/distinct/consistent enough for its own folder | Structure | Heuristic | Warning | Every-Commit | File-path scan: verify the standard folder set exists at the project root for API/Web project types; flag Web projects containing a `Repositories/` folder; the "large/distinct/consistent enough" threshold for promoting code out of `Utilities/` is a judgment call | Missing |
| solution-structure.3.3 | Acronym suffixes fully uppercase (`API`, `DB`); non-acronym descriptive names PascalCase | Structure | Binary | Hard-stop | Every-Commit | Regex: validate project/repository/folder names against the PascalCase-with-uppercase-acronym pattern (e.g., flag `...Api`, `...Db` instead of `...API`, `...DB`) | Missing |
| solution-structure.3.4 | Namespace mirrors folder structure; `using` statements placed inside the namespace block, not outside | Development | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag `using` directives declared outside the `namespace { }` block (file-scoped namespace declarations combined with top-level usings would also need review against this pattern); verify namespace segments match the containing folder path | Missing |
| solution-structure.3.5 | Assembly names match project names exactly | Technology | Binary | Hard-stop | Every-Commit | Config-scan: compare `AssemblyName` MSBuild property (or default derived from `.csproj` filename) against the project file name | Missing |
| solution-structure.3.6 | PascalCase default for all new folder/file names (including `Working/`, `Standards/`); lowercase/other casing permitted only when required by a system/platform/third-party convention that is explicitly documented | Structure | Heuristic | Warning | Every-Commit | Regex: flag new folder/file names not matching PascalCase, with an allowlist exception for documented conventional lowercase names (e.g., `wwwroot`, `.github`, `appsettings.json`) | Missing |

---

### Section 4 — Test Projects

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| solution-structure.4 | Every solution must include at least one test project in the same solution/repository as the code under test; only the primary application project and the test project may exist — any consumed class library lives in its own repository per Section 7, not as an in-solution project — no other new projects without documented architectural need | Structure | Binary | Hard-stop | Every-Commit | File-path scan: verify a `.Tests.csproj` exists in the solution; flag new `.csproj` additions beyond the primary app and test project with no adjacent architectural-decision documentation — duplicates `GlobalTestingStandards.md` testing.2.2 detection | Missing |

---

**Section 5 removed (2026 revision):** `GlobalSolutionStructureStandards.md` Section 5 ("Internal Class Libraries", in-repo project pattern) was eliminated as stale once the organization moved to NuGet packages for all consumed class libraries. Any class library consumed by a solution — whether used by one solution or many — now falls under Section 7 (Shared Class Libraries) below. The sole exemption is a library that is not consumed by any other solution as a dependency at all (a standalone tool, e.g. the `ValidationEngine`, run directly rather than referenced as a package); such a library still lives in its own repository but is outside Section 7's NuGet-consumption rule.

---

### Section 6 — Database Projects

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| solution-structure.6 | SQL Server Database Projects follow the same `SolutionName/ProjectName/` structure as all other solutions, with expected subfolders (`Deployment/`, `MigrationScripts/`, `Proposals/`, `Reports/`, `Runbooks/`, `Security/`, `Tests/`) present as needed | Structure | Heuristic | Warning | Every-Commit | File-path scan: verify `.sqlproj` follows the standard solution/project folder pattern; presence of the named subfolders is optional per the standard's own text ("may or may not exist"), so only the top-level structure is a hard check | Missing |

---

### Section 7 — Shared Class Libraries

**Applicability split:** Rules 7.1, 7.3, and 7.4 are verified **against the shared library's own repository** (its structure, versioning, and local-dev handling) — they are not applicable checks to run against a repository that merely consumes the library. Rule 7.2 is the **only rule in this section applicable to a consuming solution's repository**: when scanning a consuming solution (e.g., CaptiveExpensesApi) for Section 7 compliance, only the `PackageReference`-not-`ProjectReference` consumption check applies.

| STD-MARKER | Rule Summary | Checked Against | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|---|
| solution-structure.7.1 | Shared class library (any library consumed by a solution as a dependency, whether used by one solution or many) lives in its own dedicated repository with its own solution/test project; never committed as a source project reference in a consuming solution's `.sln`; exempt is a library not consumed by any other solution as a dependency at all (a standalone tool, e.g. `ValidationEngine`, run directly rather than referenced) — such a library is not required to be built/published as a NuGet package at all | Library's own repository | Structure | Binary | Hard-stop | Every-Commit | Config-scan: flag `.sln`/`.slnx` project entries referencing a `.csproj` path outside the consuming repository (a source-level project reference to an external shared-library repo); the standalone-tool exemption requires confirming the library is never referenced via `PackageReference`/`ProjectReference` by any other repository, a cross-repository dependency-graph check — if confirmed exempt, absence of a NuGet publish step for that repository is expected and not a violation | Missing |
| solution-structure.7.2 | Shared libraries built/published as NuGet packages to the org's Azure Artifacts feed; consumed via `PackageReference`, never a project reference or direct DLL reference; each library repo has a pipeline that builds/tests on PR to `dev` and builds/publishes/swaps/validates/reverts on merge to `main` | Both: publish/pipeline half checked in the library's own repository; `PackageReference`-not-`ProjectReference` consumption half is the only Section 7 check applicable to a consuming solution's repository | Infrastructure | Heuristic | Hard-stop | Every-Commit (on `.sln`/`.csproj` changes) | Config-scan: flag `ProjectReference` elements pointing to a shared-library project instead of `PackageReference`; flag direct `<Reference>` to a `.dll` file for a known shared library name; pipeline build/publish workflow verification is Infrastructure/Manual-only | Missing |
| solution-structure.7.3 | Semantic versioning (MAJOR.MINOR.PATCH) for shared libraries; consuming solutions use a floating minor/patch version range (`[MAJOR.0, MAJOR+1.0)`) pinned to a fixed major version; explicit update required to adopt a new major version; application projects do not require deliberate assembly versioning | Library's own repository (version authoring); the floating-range specifier itself appears in a consuming solution's `.csproj` but the versioning discipline is the library's responsibility | Technology | Binary | Hard-stop | Every-Commit (on `.csproj` changes) | Config-scan: verify `PackageReference` version specifiers for known shared-library packages use the `[MAJOR.0, MAJOR+1.0)` floating range pattern rather than a pinned exact version or an unbounded floating range | Missing |
| solution-structure.7.4 | Temporary local source-project reference during active shared-library development must never be committed; `.snupkg` symbol packages published alongside NuGet packages for debugger step-through | Library's own repository (a developer working on the library itself who temporarily wires a source reference into a consuming solution for debugging) | Development | Heuristic | Hard-stop | Every-Commit | Config-scan: flag committed `.sln`/`.slnx` files containing a shared-library source project reference (same detection as solution-structure.7.1, applied specifically to the "temporary during development" scenario); `.snupkg` publishing is an Infrastructure/pipeline-configuration fact | Missing |

---

### Section 8 — Configuration Files

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| solution-structure.8 | Every project includes at minimum `appsettings.json` and `appsettings.Development.json`; additional environment-specific files added as environments are provisioned; no secrets committed to any configuration file | Structure | Binary | Hard-stop | Every-Commit | File-path scan: verify `appsettings.json` and `appsettings.Development.json` exist in every application project; secret-content detection duplicates `GlobalSecurityStandards.md` security.2.1/2.2 | Missing |

---

### Section 9 — Repository .gitignore

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| solution-structure.9 | `.gitignore` at repository root using the standard Visual Studio baseline (`bin/`, `obj/`, `.vs/`, `*.user`, secret-bearing `appsettings.*.json` overrides, `Working/`); every governed repository (except GlobalStandards itself) must keep this baseline consistent, propagated when the standard changes | Structure | Binary | Hard-stop | Every-Commit (when `.gitignore` changes) | Config-scan: verify `.gitignore` exists at repo root and contains the required baseline entries (`bin/`, `obj/`, `.vs/`, `*.user`, `Working/`) — cross-repository propagation-consistency check is Periodic/Infrastructure since it requires comparing against a canonical baseline across all governed repositories | Missing |

---

### Section 10 — Azure Functions (Excluded — status: no Azure Functions projects exist at time of writing; standard explicitly defers rule authorship until the first Functions project is created)

---

### Section 11 — React Application Structure

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| solution-structure.11 | React applications use TypeScript; no solution file (not a VS project) | Technology | Binary | Hard-stop | Every-Commit | Config-scan: verify `tsconfig.json` presence and `.ts`/`.tsx` file extensions rather than `.js`/`.jsx` in a React repository | Missing |
| solution-structure.11.1 | Feature-first folder layout under `src/features/`; each feature owns its own `components/`, `hooks/`, `services/`, `types/`; shared code in `src/shared/`; root app setup (routing, providers, state) in `src/app/` | Structure | Binary | Hard-stop | Every-Commit | File-path scan: verify `src/features/`, `src/shared/`, `src/app/` top-level structure exists; verify each feature folder contains the expected subfolder set | Missing |
| solution-structure.11.2 | Component files PascalCase; hook/utility files camelCase; feature folder names camelCase; type/interface files PascalCase; each feature exposes a barrel `index.ts` | Development | Binary | Hard-stop | Every-Commit | Regex: validate file-naming casing per artifact type against the table (component/type files PascalCase; hook/utility/feature-folder camelCase); verify `index.ts` barrel file exists per feature | Missing |
| solution-structure.11.3 | Max three folder levels inside `features/[featureName]/`; tests colocated with `.test.tsx`/`.test.ts` suffix; imports use absolute paths via `tsconfig.json`, no relative paths traversing more than one directory level | Development | Binary | Hard-stop | Every-Commit | File-path scan: measure folder depth under each feature directory and flag depth > 3; regex: flag `../../` (two-or-more-level) relative import paths in `.ts`/`.tsx` files; verify test files are colocated with their subject file rather than in a separate test-mirror tree | Missing |

---

### Section 12 — Global Error Handling

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| solution-structure.12 | Global error handler registered as first middleware in `Program.cs` (API) or via built-in ASP.NET Core exception-handling middleware (Web), before any other request-processing middleware; handler class placed in `Utilities/` unless a dedicated folder exists | Structure | Binary | Hard-stop | Every-Commit (when `Program.cs` changes) | Roslyn-syntax: verify the exception-handling middleware registration (`UseExceptionHandler`/equivalent) is the first `app.Use*` call in the middleware pipeline — duplicates `GlobalCodingStandards.md` coding.7 and `GlobalSecurityStandards.md` cross-reference detection for the same underlying rule, now the third independent statement of this requirement across three files | Missing |

---

### Section 13 — Remediation (Excluded — procedural runbook for bringing a non-compliant repository into compliance; not an ongoing code rule, applies only during a one-time remediation effort)

### Section 14 — Compliance Verification (Excluded — restates Sections 2–12 as a checklist per established pattern; individual line-item markers `solution-structure.14.1`–`14.26` map 1:1 to rules already captured above and in cross-referenced files, no independent detection needed)

### Section 15 — Governance (Excluded)

`solution-structure.15` (ownership/PR-approval boilerplate) is excluded — not code-checkable, procedural ownership statement only.

---

**Notes:**
1. This file is dominated by **file-path/folder-structure and naming-convention checks** rather than Roslyn semantic analysis — a notable shift from the C#-behavior-focused matrices (coding, security, performance). A dedicated "structure scanner" (walking the repo file tree and comparing against expected folder/naming patterns) would cover the large majority of this file's rules cheaply, likely more cheaply than any other matrix produced so far.
2. solution-structure.12 (global error handler as first middleware) is the **third** independent statement of this same requirement across three standards files — `GlobalCodingStandards.md` Section 7 (which states the error-handling *content* rules), `GlobalSecurityStandards.md` (which cross-references it), and this file (which states the *placement/registration* rule). Strongly reinforces the case for a single shared detection rule referenced by marker ID across all three.
3. solution-structure.4 (test project existence) duplicates `GlobalTestingStandards.md` testing.2.2 exactly — both require a `.Tests.csproj` project to exist; this file adds the "no other new projects without approval" constraint on top.
4. solution-structure.2.1 (no duplicated standards content in consuming repos) is a uniquely meta rule for this project's own context — it directly governs how `CoverageMatrix-*.md` files and the standards themselves must be referenced from consuming repositories like `CaptiveExpensesApi`. Detection here would require a cross-repository text-similarity scan, which is a more involved tooling investment than most other rules in this matrix.
5. Section 11 (React) rules are all Binary/Hard-stop and mechanically checkable via simple file-path and regex scans — no TypeScript/JSX AST parsing needed for the specific rules stated here (folder layout, naming casing, depth, import-path style), though a proper TS-aware analyzer would be more robust than regex for the relative-import-depth check.

