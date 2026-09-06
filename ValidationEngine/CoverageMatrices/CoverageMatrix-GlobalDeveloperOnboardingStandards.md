# Coverage Matrix — GlobalDeveloperOnboardingStandards.md

**Status:** Reviewed
**Sign Off:** Troy Crowe
**Sign Off Date:** 2026-08-14
**Revision Date:** 2026-08-14
**Columns**

| Column | Meaning |
|---|---|
| STD-MARKER | Rule id from the standards file. |
| Rule Summary | One-line paraphrase of the rule. |
| Category | Procedural / Infrastructure / Environment. |
| Confidence | Binary (deterministic pass/fail) / Heuristic (risk-flag, needs human confirmation) / Manual-only (no automation path) / N/A (Aggregate) (pure rollup/index row; automation targets the underlying constituent rules instead). |
| Severity | Hard-stop / Warning / Manual-only-comment. |
| Frequency | One-Time (verified once at onboarding, not ongoing) / Periodic (re-verification cadence, e.g. tooling drift). |
| Technique | Short detection approach. |
| Status | Missing / Existing / Acknowledged-future-work. |

**Applicability note:** Per the CaptiveExpensesApi repository's own `copilot-instructions.md` exclusion table, `developer-onboarding.file` is excluded there with the stated reason: "Onboarding rules address one-time developer machine/tooling setup, verified once via a manually-run script; they do not apply to ongoing work by an already-onboarded active developer." **This is the governing interpretation applied here as well: this entire file is a one-time, per-developer-machine checklist, not a per-commit or per-PR code-validation target.** It is fundamentally different in kind from every other file matrixed so far — its "compliance verification" checklist is about environment/tooling/knowledge readiness of a *person*, not a property of a *codebase change*. Consequently, the "Frequency" column here uses `One-Time` rather than `Every-Commit`, and most rows are Manual-only or Heuristic since they verify human knowledge, device configuration, or local tooling state rather than repository content — a fifth detection-technique category (local-environment/tooling inspection) alongside Roslyn, file-tree/regex, ADO REST API, and ESLint.

---

### Section 2 — Onboarding Sequence

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| developer-onboarding.2 | Six-step ordered sequence (clone GlobalStandards → enable local validation enforcement → configure tools → learn standards/system rules → learn governance workflow → validate working readiness); steps must not be skipped and returned to later | Procedural | Manual-only | Manual-only-comment | One-Time | Sequencing/ordering of a human onboarding process is not mechanically verifiable — it is a checklist ordering guideline for a person or an onboarding-facilitation script to follow, not a repository-state check | Missing |

---

### Section 3 — Step 1: Clone the GlobalStandards Repository

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| developer-onboarding.3.1 | GlobalStandards repository cloned locally before cloning/modifying any governed application repository | Environment | Binary | Hard-stop | One-Time | Local filesystem check: verify a `GlobalStandards` directory exists as a sibling to the working repository (this specific check is achievable by a local pre-commit/setup script, distinguishing it from the fully Manual-only sequencing rule above) | Missing |
| developer-onboarding.3.2 | Local GlobalStandards clone remains available throughout the work session, not treated as a one-time read | Environment | Manual-only | Manual-only-comment | Periodic | "Remains available" over time and "treated as an active reference" is a behavioral/usage pattern, not a single-point-in-time file-presence check — beyond what a script can verify | Missing |
| developer-onboarding.3.3 | Repository's `.github/copilot-instructions.md` read before the first work session in that repository | Procedural | Manual-only | Manual-only-comment | One-Time | Whether a human "read" a file before starting work is not verifiable by any automated technique | Missing |
| developer-onboarding.3.4 | Developer acquires local access to all governed repositories required for the assigned work (target repo, standards baseline repo, dependency/sibling repos) | Environment | Manual-only | Manual-only-comment | One-Time | Requires knowing the assigned work scope and confirming access/clone state for an arbitrary set of repositories determined by the task — not mechanically derivable from a single repository's state | Missing |
| developer-onboarding.3.5 | Every governed repository (including GlobalStandards) cloned as a sibling directory under one common local root; no governed repository nested inside another's directory tree | Environment | Binary | Hard-stop | One-Time | Local filesystem check: verify the working repository's parent directory contains `GlobalStandards` and other known governed-repo names as siblings (not nested); this is the local-tooling prerequisite check explicitly required before developer-onboarding.4 (local validation enforcement) can function, and is the most cleanly automatable rule in this section | Missing |

---

### Section 4 — Step 2: Enable Local Standards Validation Enforcement

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| developer-onboarding.4.1 | PowerShell-based local standards-validation process installed/configured/enabled in the developer's environment before implementation work begins | Environment | Binary | Hard-stop | One-Time | Local script check: verify the required PowerShell validation script/module (e.g., a git pre-commit hook invoking a `.ps1` entry point) is installed and registered in the local git hooks configuration (`.git/hooks/pre-commit` or equivalent) | Missing |
| developer-onboarding.4.2 | Local validation executes standards-compliance checks before local check-in completes; check-in blocked on failure; pipeline validation required but must not be the first detection point when local enforcement can prevent it; supports standards compliance validation, pre-check during local check-in, and local verification before PR creation | Infrastructure | Binary | Hard-stop | One-Time (setup verification) / Periodic (functional re-test) | Functional test: attempt a local commit with a deliberately non-compliant change in a sandboxed test and verify the local hook blocks it — this is a "test the safety mechanism" style check rather than a static-state check, more expensive than a simple presence check but genuinely automatable as a onboarding-completion smoke test | Missing |
| developer-onboarding.4.3 | If required PowerShell validation scripts are not yet implemented in a repository or standards baseline, that gap is treated as missing governed capability, not permission to skip the requirement | Procedural | Manual-only | Manual-only-comment | Periodic | This is a policy statement about how to interpret an absent capability (an organizational/process decision), not a checkable technical state | Missing |
| developer-onboarding.4.6 | Validation enforcement treated as a prerequisite for work (heading title implies local validation must be active before implementation work is undertaken) | Procedural | Binary | Hard-stop | One-Time | Duplicates developer-onboarding.4.1's local-hook-presence check as a gating precondition — same technique, reinforcing framing | Missing |

---

### Section 5 — Step 3: Configure Development Tools

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| developer-onboarding.5.1 | Required tooling installed and functioning: Git, the repository's required .NET SDK/language runtime, required IDE/editor tooling, access to organization package sources/feeds | Environment | Binary | Hard-stop | One-Time | Local environment scan: verify `git --version`, `dotnet --version` (matching the repository's `global.json`/`TargetFramework`), and configured NuGet package source credentials are present and functional — a scripted local-diagnostic check, not a repository-content check | Missing |
| developer-onboarding.5.2 | Developer authenticates via the documented device login flow for interactive Azure/Azure DevOps access; onboarding confirms the developer can complete that flow successfully | Environment | Manual-only | Manual-only-comment | One-Time | Verifying a successful interactive device-login flow requires the developer to actually perform an interactive auth step — not something a static check can confirm without live credential verification, which is out of scope for a validation engine | Missing |
| developer-onboarding.5.3 | Confirm access to the required ADO org/project, governed repositories in scope, and package sources/internal feeds; confirm package restore succeeds from Azure Artifacts (or other governed source) when applicable | Environment | Binary | Hard-stop | One-Time | Local diagnostic: run `dotnet restore` (or equivalent) against the target repository and verify success/failure — a genuinely automatable functional check, distinguishing this from the Manual-only auth-flow rule above | Missing |
| developer-onboarding.5.4 | Developer confirms they can restore packages, build the solution, and run applicable tests locally for at least one governed repository in scope, before taking a work item | Infrastructure | Binary | Hard-stop | One-Time | Local diagnostic: run `dotnet restore && dotnet build && dotnet test` against a target repository and verify all three succeed | Missing |
| developer-onboarding.5.5 | Developer knows how to run the repository's documented local validation script/entry point, which executes the required pre-PR gates (restore, build, test, validation checks for that repository type) | Infrastructure | Binary | Hard-stop | One-Time | Local diagnostic: locate and execute the repository's documented validation entry point (e.g., a `Tools/*.ps1` script referenced from the repo's README/copilot-instructions) and verify it runs to completion | Missing |
| developer-onboarding.5.6 | Developer's workspace makes both the standards repository and the governed repository accessible during the work session; must not work from an environment where standards cannot be reviewed while changes are made | Environment | Binary | Hard-stop | One-Time | Local filesystem check — overlaps with developer-onboarding.3.5's sibling-clone-layout check; same technique, restated as a workspace-accessibility requirement | Missing |

---

### Section 6 — Step 4: Learn the Standards and System Rules

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| developer-onboarding.6.1 | Developer reads the standards governing the assigned repository type/work area before implementation, at minimum: Governance, Repository, Azure DevOps Pipeline, Solution Structure, Coding standards, plus any applicable repository-type-specific standards | Procedural | Manual-only | Manual-only-comment | One-Time | Whether a human has "read and understood" a set of documents is not verifiable by any automated technique; at most, a quiz/comprehension-check tool could approximate this, which is out of scope | Missing |
| developer-onboarding.6.2 | Developer understands how the target repository enters the standards chain via `.github/copilot-instructions.md` and how repository-local `Standards/` deviation files alter the baseline | Procedural | Manual-only | Manual-only-comment | One-Time | Comprehension of the governance-chain mechanism is a knowledge check, not a technical state — though the mechanism itself (`file-specification.2.15` addendum structure) is independently machine-verifiable, the developer's *understanding* of it is not | Missing |
| developer-onboarding.6.3 | Developer understands system structure: primary project(s), test project, local validation entry point location, pipeline files/entry points, shared library/database/related-repository dependencies | Procedural | Manual-only | Manual-only-comment | One-Time | Same knowledge-verification limitation as 6.1/6.2 — the underlying facts (project layout, pipeline files) are independently discoverable, but confirming the developer has internalized them is not mechanically checkable | Missing |
| developer-onboarding.6.4 | Developer completes the applicable repository-type readiness action: run the app locally and exercise a representative workflow (+ `/healthcheck` check for Web API/App); identify DB entry/validation/deployment-verification path for DB repos; build+test+identify package identity for shared libraries; run local validation and identify workflow files for documentation-only repos | Infrastructure | Heuristic | Warning | One-Time | For Web API/App repos: a scripted smoke test can call the local `/healthcheck` endpoint after a local run and verify a 2xx response (Binary for that sub-case); the broader "identify the entry point/package identity/workflow files" requirement for other repo types requires human confirmation of comprehension, keeping the overall rule Heuristic | Missing |
| developer-onboarding.6.5 | Developer identifies applicable standards for the assigned work slice before making changes; standards discovery after implementation begins is not the normal pattern | Procedural | Manual-only | Manual-only-comment | Every-PR (behavioral, not code-state) | Whether standards were identified "before" vs. "after" implementation is a temporal/behavioral fact about the developer's process, not observable from the final code diff alone | Missing |

---

### Section 7 — Step 5: Learn the Governance Workflow

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| developer-onboarding.7.1 | Developer understands and follows the `feature/* → dev → main` branch flow; direct commits to protected branches not permitted | Procedural | Manual-only | Manual-only-comment | One-Time (knowledge) | Knowledge-verification limitation as above; the actual branch-flow *enforcement* (as opposed to the developer's understanding of it) is already covered by `GlobalRepositoryStandards.md` repository.2.3/repository.3 | Missing |
| developer-onboarding.7.2 | Developer understands PR-based promotion, branch-policy gating, the `feature/*→dev` (validate + produce traceable output) vs. `dev→main` (approval/promotion record) workflow split, and delivery-pipeline responsibilities for environment-bound repository types | Procedural | Manual-only | Manual-only-comment | One-Time (knowledge) | Same knowledge-verification limitation; the mechanics themselves are covered by `GlobalRepositoryStandards.md` Sections 4–6 | Missing |
| developer-onboarding.7.3 | Developer responsible for ensuring local build/test/standards checks pass before creating/updating a PR; pipeline validation confirms compliance but does not replace this responsibility | Procedural | Manual-only | Manual-only-comment | Every-PR (behavioral) | Whether the developer personally validated before pushing (as opposed to relying solely on the pipeline) is a process/behavioral fact not directly observable from repository state, though a proxy Heuristic (checking whether a local pre-commit hook ran, if instrumented) could partially approximate it | Missing |
| developer-onboarding.7.4 | Developer runs the repository's documented local validation script/entry point before creating a PR and before pushing updates addressing PR feedback; skipping local validation and relying on the pipeline as first check is not permitted | Procedural | Manual-only | Manual-only-comment | Every-PR (behavioral) | Same behavioral-verification limitation as 7.3 — not observable from the final pushed diff alone without local-hook instrumentation/telemetry | Missing |
| developer-onboarding.7.5 | Developer knows which standards apply to their work slice before beginning implementation; standards must guide the work, not merely audit it afterward | Procedural | N/A (Aggregate) | N/A | N/A | Pure restatement of developer-onboarding.6.5's "standards discovery timing" rule for the governance-workflow-learning context; automation should evaluate 6.5 directly rather than this duplicate row | Missing |
| developer-onboarding.7.6 | Developer understands promotion is based on the already-validated change set; for deployable-artifact repos, traceability from feature work through promoted output and the `dev→main` promotion record is preserved; for shared library repos, the promoted output is the governed NuGet package artifact, not a production rollout deployment | Procedural | Manual-only | Manual-only-comment | One-Time (knowledge) | Knowledge-verification limitation; the actual traceability mechanism is independently covered by `GlobalRepositoryStandards.md` repository.5/repository.6 and `GlobalNuGetLibraryStandards.md` nuget-library.8 | Missing |

---

### Section 8 — Step 6: Validate Working Readiness

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| developer-onboarding.8.1 | Developer successfully clones a governed repository, restores dependencies, builds, runs tests, runs the local validation entry point, reviews the repository entry point/local deviations, and completes the applicable repository-type readiness action (Section 6.4) | Infrastructure | Binary | Hard-stop | One-Time | Composite local diagnostic: scripted sequence of `git clone` → `dotnet restore` → `dotnet build` → `dotnet test` → local validation script execution, each step's exit code checked — the most comprehensive and cleanly automatable "onboarding smoke test" in the file, combining several earlier sub-checks (5.3, 5.4, 5.5) into one end-to-end verification | Missing |
| developer-onboarding.8.2 | Developer demonstrates ability to create and push a feature branch following repository branch-naming rules | Environment | Binary | Hard-stop | One-Time | Local/ADO check: verify a test feature branch was created matching the `feature/<descriptive-name>` pattern (per `GlobalRepositoryStandards.md` repository.2.2) and successfully pushed to the remote | Missing |
| developer-onboarding.8.3 | Onboarding includes either a real PR for an approved onboarding change or a documented walkthrough of PR creation/validation flow; developer understands PR title/description/auto-complete/review/validation behavior in the repositories they will use | Procedural | Heuristic | Warning | One-Time | If a real onboarding PR is created, its existence and adherence to `GlobalRepositoryStandards.md` repository.4.2/4.3 format is Binary-checkable via ADO REST API; if a "documented walkthrough" alternative is used instead, verifying comprehension reverts to Manual-only — the rule's dual-path nature makes overall classification Heuristic | Missing |

---

### Section 9 — Expectations for AI-Assisted Development

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| developer-onboarding.9 | When using AI assistance: developer ensures the repository entry point file is read first, verifies AI-generated output complies with applicable standards before accepting, does not allow AI-generated changes to bypass branch/PR/review/validation controls, and remains accountable for all accepted changes regardless of authorship | Procedural | Heuristic | Warning | Every-PR | The branch/PR/review/validation-control-bypass sub-clause is enforceable exactly the same way as any other change — via the standard `GlobalRepositoryStandards.md` PR/branch-policy checks (Binary, since AI-authored changes flow through the same PR gates as human-authored ones); the "verified AI output against standards before accepting" and "read entry point first" sub-clauses are behavioral/knowledge facts about the developer's process and are Manual-only — overall Heuristic given the mixed enforceability | Missing |

---

### Section 10 — Compliance Verification (Excluded as an independent detection target)

The Section 10 checklist restates Sections 2–9 as a pre-onboarding-completion checklist, plus one additional standalone item not explicitly numbered elsewhere in the body: "The developer has installed or updated the Pester PowerShell module to version 5.0 or later for the current user" — this maps to the tooling requirement implied by developer-onboarding.4.1/5.1 (PowerShell-based local validation tooling) and is independently Binary-checkable via `Get-Module -ListAvailable Pester` version inspection. No other independent detection techniques are introduced beyond what is captured above.

### Section 11 — Governance (Excluded)

`developer-onboarding.11` (ownership/approval boilerplate) is excluded — not code-checkable, procedural ownership statement only.

---

**Notes:**
1. **This file is fundamentally a different kind of standard than every other file matrixed so far**: it verifies a *developer's* environment, knowledge, and behavior at a single point in time (onboarding), not an ongoing property of *code* or *repository configuration*. The CaptiveExpensesApi repository's own exclusion rationale for `developer-onboarding.file` (one-time setup, verified once, does not apply to an already-onboarded active developer) is the correct governing lens — this matrix is drafted for completeness of the corpus and for use if/when an "onboarding wizard" or environment-diagnostic script is built, not for inclusion in an ongoing per-commit/per-PR validation engine.
2. A meaningful subset of rules *are* genuinely automatable as a **one-time local-environment diagnostic script** (developer-onboarding.3.1, 3.5, 4.1, 5.1, 5.3, 5.4, 5.5, 8.1, 8.2, and the Pester-version checklist item) — these could be packaged as a single `Test-DeveloperOnboarding.ps1` smoke-test script that a new developer runs once, distinct from the ongoing standards-validation engine that governs code changes. This is a natural, low-effort automation opportunity worth flagging separately from the main validation-engine effort.
3. The majority of remaining rules (knowledge of standards content, comprehension of governance workflow, "read before starting" temporal facts, AI-output-verification behavior) are **Manual-only by nature** — they describe what a competent, compliant developer should know and do, not a property of any artifact a tool can inspect. No amount of additional tooling investment converts these to Binary/Heuristic; they remain a training/review responsibility.
4. Several rules explicitly cross-reference and duplicate detection already captured in other matrices (developer-onboarding.7.1/7.2/7.6 restate `GlobalRepositoryStandards.md` branch/PR/promotion mechanics; developer-onboarding.6.4's `/healthcheck` check overlaps with `GlobalAzureDevOpsPipelineStandards.md`'s production-environment health-check rule). Per `GlobalFileSpecificationStandards.md` file-specification.2.2, these are acceptable here since the onboarding file's *purpose* (confirm developer understanding of an existing mechanism) is legitimately distinct from the *mechanism's own enforcement* rule living in its owning file — this is a case of intentional, appropriate cross-referencing rather than problematic duplication.

