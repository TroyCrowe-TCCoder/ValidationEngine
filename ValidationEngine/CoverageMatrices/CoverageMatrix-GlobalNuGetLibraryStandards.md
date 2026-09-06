# Coverage Matrix — GlobalNuGetLibraryStandards.md

**Status:** Reviewed
**Sign Off:** Troy Crowe
**Sign Off Date:** 2026-08-14
**Revision Date:**
**Columns**

| Column | Meaning |
|---|---|
| STD-MARKER | Rule id from the standards file. |
| Rule Summary | One-line paraphrase of the rule. |
| Category | Structure / Technology / Development / Infrastructure. |
| Confidence | Binary (deterministic pass/fail) / Heuristic (risk-flag, needs human confirmation) / Manual-only (no automation path). |
| Severity | Hard-stop / Warning / Manual-only-comment. |
| Frequency | Every-Commit (change-set scoped, `.csproj`/pipeline changes) / Periodic (recurring cadence, e.g. vulnerability/outdated-package scans). |
| Technique | Short detection approach. |
| Status | Missing / Existing / Acknowledged-future-work. |

**Applicability note:** This file's own exclusion table entry for CaptiveExpensesApi states "This solution does not author or publish a shared NuGet library" — so **every row in this matrix is out of scope for the CaptiveExpensesApi repository specifically**, but the file is `Status: Active` in the GlobalStandards corpus (unlike React/TypeScript) and applies fully to any repository that does author a shared library (e.g., a future `HttpClientManager` or `DataImportExportManager` repo). Detection is a mix of `.csproj`/MSBuild property scanning, Roslyn accessibility-modifier analysis, and `dotnet` CLI tool output parsing (`dotnet list package --vulnerable`/`--outdated`).

---

### Section 2 — Shared Library Scope

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| nuget-library.2.1 | A codebase used by multiple repositories must be implemented as a NuGet package rather than copied between repositories or committed into consuming solutions | Structure | Manual-only | Manual-only-comment | Periodic (architecture review) | Detecting "this code is duplicated across repositories and should be a shared package" requires cross-repository code-similarity analysis and an architectural judgment call — not a per-commit mechanical check | Missing |
| nuget-library.2.2 | Every shared library lives in its own dedicated repository with its own solution/test project (per `GlobalSolutionStructureStandards.md` Section 7); consuming repositories must not commit a shared-library source project into their solution | Structure | Binary | Hard-stop | Every-Commit | Duplicates `GlobalSolutionStructureStandards.md` solution-structure.7.1 detection exactly — config-scan flagging `.sln` project entries referencing an external shared-library `.csproj` path | Missing |

---

### Section 3 — Repository and Project Structure

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| nuget-library.3.1 | Shared library repository follows the standard `RepoName/RepoName.sln` + `RepoName/RepoName.csproj` + `RepoName.Tests/RepoName.Tests.csproj` layout | Structure | Binary | Hard-stop | Every-Commit | File-path scan — duplicates `GlobalSolutionStructureStandards.md` solution-structure.2/solution-structure.4 detection for the shared-library case specifically | Missing |
| nuget-library.3.2 | Repository name, solution name, project name, assembly name, and package ID must all match exactly for a single-library repository, unless an approved deviation documents an alternate naming rule | Technology | Binary | Hard-stop | Every-Commit | Config-scan: compare repository name, `.sln` base name, `.csproj` base name, `AssemblyName` MSBuild property, and `PackageId` MSBuild property for exact match — a five-way identity check, more comprehensive than the plain solution-structure.3.1/3.5 two-way checks it builds on | Missing |

---

### Section 4 — Package Metadata and Project Settings

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| nuget-library.4.1 | Required `.csproj` metadata: `PackageId`, `Version`, `Authors`, `Company`, `Description` (must describe the capability, not just repeat the package name), `RepositoryUrl`, `RepositoryType`, `GenerateDocumentationFile`, `Nullable`, `ImplicitUsings` | Technology | Heuristic | Hard-stop | Every-Commit (on `.csproj` changes) | Config-scan: verify presence of each required MSBuild property element; `Description` content-quality check ("does not just repeat the package name") requires simple text-similarity comparison against `PackageId` value — a light Heuristic layer on an otherwise Binary presence check | Missing |
| nuget-library.4.2 | `Deterministic` and `ContinuousIntegrationBuild` (conditioned on `$(CI)`) properties enabled | Technology | Binary | Hard-stop | Every-Commit | Config-scan: verify both MSBuild properties are present with the required values/condition | Missing |
| nuget-library.4.3 | SourceLink and symbol package generation enabled: `PublishRepositoryUrl`, `EmbedUntrackedSources`, `IncludeSymbols`, `SymbolPackageFormat=snupkg`, plus a `Microsoft.SourceLink.AzureDevOpsServer.Git` PackageReference with `PrivateAssets=All` | Technology | Binary | Hard-stop | Every-Commit | Config-scan: verify all listed MSBuild properties and the specific SourceLink PackageReference (with correct `PrivateAssets` attribute) are present | Missing |

---

### Section 5 — Public API and Registration Rules

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| nuget-library.5.1 | Only public types required by consumers are exposed; implementation types not part of the supported contract are `internal`; public types are `sealed` unless inheritance is part of the intended contract | Development | Heuristic | Warning | Every-Commit | Roslyn-semantic: enumerate all `public` type declarations and flag those with no external usage signal (heuristic: no XML-doc `<summary>` suggesting intended public consumption, or types only referenced internally) as candidates for `internal`; flag non-`sealed` public classes with no derived types in the same assembly as candidates for `sealed` — both require judgment on "intended library contract," keeping this Heuristic | Missing |
| nuget-library.5.2 | Every public service exposed for DI has a corresponding public interface | Development | Binary | Hard-stop | Every-Commit | Roslyn-semantic: for each public class registered via a `ServiceCollectionExtensions` method (matching `services.Add*<TInterface, TImplementation>` or `services.AddScoped/Singleton/Transient<T>()` patterns), verify a corresponding public interface exists and is used in the registration | Missing |
| nuget-library.5.3 | Single `ServiceCollectionExtensions` entry point for service registration; consumers call the shared registration method rather than wiring up internal services manually | Development | Binary | Hard-stop | Every-Commit | Roslyn-syntax: verify a public static class matching `*ServiceCollectionExtensions` exists with an `Add{LibraryName}` extension method on `IServiceCollection`; verify no other public types in the library expose direct DI-registration helper methods that bypass this single entry point | Missing |
| nuget-library.5.4 | Configurable shared library exposes a dedicated options type bound through the standard options pattern (`IOptions<T>`) | Development | Binary | Hard-stop | Every-Commit | Roslyn-semantic: flag configurable behavior (constructor/method parameters that vary by environment/consumer) not backed by an `IOptions<T>`-bound options class — detection scoped to libraries whose `ServiceCollectionExtensions` method accepts configuration delegates | Missing |
| nuget-library.5.5 | Every public type/method/property/constructor has XML documentation comments | Development | Binary | Hard-stop | Every-Commit | Roslyn-syntax + `GenerateDocumentationFile` build-warning scan (CS1591 "missing XML comment") set to error for public members; standard, well-supported compiler-level check | Missing |
| nuget-library.5.6 | Nullable reference types enabled; public APIs declare nullability explicitly; `ArgumentNullException` thrown (via `ArgumentNullException.ThrowIfNull` or equivalent) for required null arguments | Development | Binary | Hard-stop | Every-Commit | Config-scan: verify `<Nullable>enable</Nullable>`; Roslyn-analyzer (CS860x nullable warnings treated as errors for public API surface) combined with a pattern-scan for `ArgumentNullException.ThrowIfNull`/`if (x is null) throw new ArgumentNullException` guard clauses on public method parameters — overlaps with `GlobalCodingStandards.md`'s general guard-clause rules, applied specifically to public library entry points here | Missing |

---

### Section 6 — Dependency Rules

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| nuget-library.6.1 | Shared library references only packages required by its own implementation; no speculative dependencies | Development | Manual-only | Manual-only-comment | Periodic (dependency review) | "Speculative" (not currently used) dependency detection requires cross-referencing declared `PackageReference` entries against actual namespace usage in source — feasible as a Heuristic (unused-reference scan) but the "speculative vs. genuinely needed" judgment for edge cases (e.g., reflection-based usage) keeps final classification Manual-only | Missing |
| nuget-library.6.2 | Shared library must not take a dependency dragging in an unnecessary heavyweight hosting stack or other bulky transitive dependency set when the library must remain broadly reusable | Development | Heuristic | Warning | Periodic | `dotnet list package --include-transitive` output scan cross-referenced against a denylist of known heavyweight packages (e.g., `Microsoft.AspNetCore.App` framework references, `Microsoft.EntityFrameworkCore` when unnecessary) — "unnecessary" and "broadly reusable" judgment keeps this Heuristic | Missing |
| nuget-library.6.3 | NuGet dependencies declare an explicit version; `Version="*"` or other floating-latest patterns prohibited | Technology | Binary | Hard-stop | Every-Commit | Config-scan: regex on `.csproj` `PackageReference` `Version` attributes flagging `*`, `*-*`, or other wildcard/floating-latest patterns | Missing |

---

### Section 7 — Versioning Rules

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| nuget-library.7.1 | Semantic versioning `MAJOR.MINOR.PATCH` per the increment table (breaking=MAJOR, backward-compatible addition=MINOR, fix=PATCH); a published version must never be reused | Technology | Heuristic | Hard-stop | Every-Commit (on version bump) / Periodic (publish-time check) | Config-scan verifies the `<Version>` format is valid semver (Binary); verifying the *correctness* of the increment size relative to the actual API diff requires a public-API-surface diff between the current and previous published version (Heuristic); "never reused" is Binary and checkable against the Azure Artifacts feed's published-version list at publish time | Missing |
| nuget-library.7.2 | Pre-release versions use explicit semantic labels (`-alpha.1`, `-beta.1`, `-rc.1`); production repositories must not consume pre-release packages unless an approved deviation allows it | Technology | Binary | Hard-stop | Every-Commit | Config-scan: regex on consuming-repository `PackageReference` `Version` values flagging any pre-release label suffix (`-alpha`, `-beta`, `-rc`, etc.) when the consuming repository is a production application (cross-reference against repo classification) | Missing |
| nuget-library.7.3 | Package version declared solely through the project file `<Version>` property (or injected into it during the governed pack step); version must not be split across unrelated variables or duplicated | Technology | Binary | Hard-stop | Every-Commit | Config-scan: verify no competing version-declaration mechanism exists (e.g., a separate `version.txt`, hardcoded version string in source, or duplicate `<AssemblyVersion>`/`<FileVersion>` values inconsistent with `<Version>`) | Missing |

---

### Section 8 — Build, Pack, and Publish

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| nuget-library.8.1 | Class-library pipeline pattern (per `GlobalAzureDevOpsPipelineStandards.md`); `feature/* → dev` validation builds/tests and creates the NuGet package artifact; shared library repositories must not define or require a production rollout pipeline (not an environment-bound deployment artifact) | Infrastructure | Heuristic | Hard-stop | Every-PR | Pipeline-YAML scan: verify the dev pipeline includes a `dotnet pack` step producing an artifact; flag presence of a `main.yml`/production pipeline containing deploy/swap/environment-targeting tasks in a repository classified as a class library — duplicates and extends `GlobalAzureDevOpsPipelineStandards.md`'s class-library pipeline-pattern detection | Missing |
| nuget-library.8.2 | Package creation uses `dotnet pack`; `nuget pack` prohibited for SDK-style projects | Infrastructure | Binary | Hard-stop | Every-PR | Pipeline-YAML scan: regex for `dotnet pack` command presence and absence of any `nuget pack`/`nuget.exe pack` invocation | Missing |
| nuget-library.8.3 | Internal packages published to the org's Azure Artifacts feed; must not be published to `nuget.org` unless the repository is explicitly governed as an approved open-source package repository | Infrastructure | Binary | Hard-stop | Every-PR | Pipeline-YAML scan: verify `dotnet nuget push` (or `NuGetCommand@2` task) targets the internal Azure Artifacts feed URL/source name, not `nuget.org`/`api.nuget.org`, unless the repository carries a documented open-source-package classification | Missing |
| nuget-library.8.4 | Published package version is immutable; must not be overwritten/replaced/deleted as a normal correction path; corrections released as a new semver version | Infrastructure | Binary | Hard-stop | Periodic (feed audit) | Azure Artifacts REST API / `az artifacts` CLI query: enumerate published versions per feed and detect any unlist/delete/overwrite events outside an explicitly documented emergency-correction exception | Missing |

---

### Section 9 — Consumption Rules

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| nuget-library.9.1 | Consuming repository references a shared library via `<PackageReference>` only; not a committed project reference or direct DLL reference | Structure | Binary | Hard-stop | Every-Commit | Config-scan — duplicates `GlobalSolutionStructureStandards.md` solution-structure.7.1/7.2 detection exactly for the consumption side | Missing |
| nuget-library.9.2 | Fixed-major floating version range (`[MAJOR.0, MAJOR+1.0)`) so non-breaking minor/patch updates adopt automatically without allowing automatic major upgrades | Technology | Binary | Hard-stop | Every-Commit | Config-scan — duplicates `GlobalSolutionStructureStandards.md` solution-structure.7.3 detection exactly | Missing |
| nuget-library.9.3 | Package upgrade workflow: review release notes/change record, update the reference, run required validation before merge, record the upgrade in the PR description | Procedural | Manual-only | Manual-only-comment | Every-PR | Steps 1 and 4 (reviewing release notes, recording in PR description) require semantic/textual judgment not mechanically verifiable; step 2 (reference updated) is Binary-checkable via `.csproj` diff; step 3 (validation run before merge) duplicates the `GlobalRepositoryStandards.md` repository.5 pipeline-gate check — overall Manual-only due to steps 1/4 | Missing |
| nuget-library.9.4 | Temporary local source-project attachment for debugging must not be committed; committed state must return to package-only consumption before PR is opened | Structure | Binary | Hard-stop | Every-PR | Config-scan — duplicates `GlobalSolutionStructureStandards.md` solution-structure.7.4 detection exactly, applied at PR-diff time | Missing |

---

### Section 10 — Package Health Verification

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| nuget-library.10.1 | `dotnet list package --vulnerable` run as part of governed validation; a `High`/`Critical` severity vulnerability finding fails validation unless an approved deviation documents a temporary exception | Infrastructure | Binary | Hard-stop | Every-PR | Pipeline-YAML scan: verify a `dotnet list package --vulnerable` (or equivalent, e.g. `dotnet-outdated`/Dependabot alert integration) step exists and its exit code/output-parsing gate fails the build on High/Critical findings — directly parallels `GlobalSecurityStandards.md`'s dependency-vulnerability-scanning rule, likely a duplicate-coverage candidate worth cross-checking | Missing |
| nuget-library.10.2 | Outdated package dependencies reviewed on a recurring basis; required upgrades tracked via backlog when not completed immediately; major-version lag must not accumulate without an explicitly tracked follow-up | Infrastructure | Manual-only | Manual-only-comment | Periodic | `dotnet list package --outdated` output can be Heuristically parsed to flag packages with a major-version lag (structural detection), but "tracked through backlog work" and "not allowed to accumulate without follow-up" require verifying an actual backlog/work-item record exists — not mechanically verifiable from the codebase alone | Missing |

---

### Section 11 — Compliance Verification (Excluded as an independent detection target)

The Section 11 checklist restates Sections 2–10 as a pre-merge checklist. No independent detection techniques are introduced; each maps 1:1 to a rule already captured above.

### Section 12 — Governance (Excluded)

`nuget-library.12` (ownership/approval boilerplate) is excluded — not code-checkable, procedural ownership statement only.

---

**Notes:**
1. This file has **heavy duplicate coverage with `GlobalSolutionStructureStandards.md` Section 7** (Shared Class Libraries) — nuget-library.2.2, 3.1, 9.1, 9.2, and 9.4 restate solution-structure.7.1–7.4 near-verbatim, applied specifically to the NuGet-authoring/consumption context. Per `GlobalFileSpecificationStandards.md` file-specification.2.2 (single responsibility, no rule duplicated across files), these pairs are strong candidates for consolidation — the Solution Structure file appears intended to state the general "where shared libraries live and how they're referenced" rule, while this file elaborates the NuGet-specific packaging mechanics, but the actual rule text overlaps rather than purely cross-referencing in several spots.
2. Sections 4 and 5 (package metadata, build properties, public API surface, DI registration, XML docs, nullability) are the most cheaply automatable portion of this file — nearly all Binary, detectable via a combination of `.csproj` MSBuild-property scanning and standard Roslyn/compiler-warning gates (`CS1591` for missing XML docs, nullable-reference-type warnings). A single MSBuild `Directory.Build.props`/analyzer-ruleset applied uniformly across shared-library repositories would enforce most of these automatically at build time rather than requiring a separate validation-engine check.
3. nuget-library.10.1 (vulnerability scanning via `dotnet list package --vulnerable`) likely duplicates a rule already captured in the accepted `CoverageMatrix-GlobalSecurityStandards.md` matrix's dependency/package-security section — worth cross-checking during matrix review to confirm whether this is intentional domain-specific restatement (NuGet-library-specific packaging concern) or an unintended duplicate needing consolidation per file-specification.2.2.
4. Section 8 (Build, Pack, Publish) and Section 9 (Consumption) rules are almost entirely **pipeline-YAML and Azure Artifacts feed-API detectable**, aligning with the ADO-centric detection category already established in the `GlobalRepositoryStandards.md` and `GlobalAzureDevOpsPipelineStandards.md` matrices, rather than requiring new tooling investment.

