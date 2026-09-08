# Coverage Matrix — GlobalRepositoryOnboardingStandards.md

**Status:** Reviewed
**Sign Off:** Troy Crowe
**Sign Off Date:** 2026-08-14
**Revision Date:** 2026-08-14
**Columns**

| Column | Meaning |
|---|---|
| STD-MARKER | Rule id from the standards file. |
| Rule Summary | One-line paraphrase of the rule. |
| Category | Infrastructure / Structure / Procedural. |
| Confidence | Binary (deterministic pass/fail) / Heuristic (risk-flag, needs human confirmation) / Manual-only (no automation path) / N/A (Aggregate) (pure rollup/index row; automation targets the underlying constituent rules instead). |
| Severity | Hard-stop / Warning / Manual-only-comment. |
| Frequency | One-Time (verified once, at repository creation). This entire standard is a creation-time baseline; no rule in this file recurs after onboarding is complete. |
| Technique | Short detection approach. |
| Status | Missing / Existing / Acknowledged-future-work. |

**Applicability note:** This file was rebuilt to remove scope creep (the former Sections 4-7 covering standards-alignment remediation, verification/completion sign-off, existing-repository remediation, and AI-assisted onboarding process rules). It is now a flat creation-time baseline checklist only: what must exist in a repository at the moment it is created and brought under governance. Ongoing compliance for an already-onboarded repository is governed by `GlobalRepositoryStandards.md` and the other active standards files, not this one. Per `CaptiveExpensesApi`'s `copilot-instructions.md` exclusion table, `repository-onboarding.file` is excluded there because onboarding rules address one-time repository creation state, verified once; they do not apply to an already-onboarded, established repository.

---

### Section 1 — Purpose

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| repository-onboarding.1 | Defines this file as a one-time, single-pass creation-time baseline checklist; not a remediation, verification, or ongoing-compliance process | Procedural | Manual-only | Manual-only-comment | One-Time | Purely definitional/scoping statement — no independently observable repository-state fact to check | Missing |

---

### Section 2 — Repository Governance Entry Points

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| repository-onboarding.2 | `.github/copilot-instructions.md` exists and links first to the designated global root governance standard, with repository-local deviation documents listed after; root-level `Standards/` folder exists containing only repository-local deviation files; `README.md` and `.gitignore` exist at repository root | Structure | Binary | Hard-stop | One-Time | File-path scan: verify `.github/copilot-instructions.md` exists and its first markdown link target matches the designated root governance standard path; verify `Standards/` folder exists at repo root and contains only files matching the addendum-file naming pattern; verify `README.md` and `.gitignore` exist — duplicates and extends `GlobalSolutionStructureStandards.md` solution-structure.2.1 detection | Missing |

---

### Section 3 — Branch Model

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| repository-onboarding.3 | `main` is the root branch, `dev` is the integration branch, created from `main` immediately after repository creation | Infrastructure | Binary | Hard-stop | One-Time | ADO REST API: verify both `refs/heads/main` and `refs/heads/dev` branches exist — duplicates `GlobalRepositoryStandards.md` repository.2.1 detection exactly | Missing |

---

### Section 4 — Branch Policies

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| repository-onboarding.4 | Branch protection configured in ADO: `dev` blocks direct commits, requires the governed validation pipeline, enforces required-reviewer policy (unless deviation); `main` blocks direct commits, allows only approved non-approval promotion policies for `dev -> main` (unless deviation adds an approval gate); no mandated merge strategy for either branch unless a deviation restricts it | Infrastructure | Binary | Hard-stop | One-Time | ADO branch-policy query — duplicates `GlobalRepositoryStandards.md` repository.7.1/7.2 detection exactly, applied as a creation-time gate rather than an ongoing audit | Missing |

---

### Section 5 — Solution and Test Baseline

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| repository-onboarding.5 | Solution file at repository root; each project in its own top-level directory; required test project exists containing at least one or two representative starter tests, before new feature work begins | Structure | Heuristic | Hard-stop | One-Time | File-path scan: verify `.sln`/`.slnx` exists at repo root, each `.csproj` resides in its own top-level folder, and a test project (naming convention `{Project}.Tests`) exists with at least 1-2 test methods — duplicates `GlobalSolutionStructureStandards.md` structural checks; confirming tests are "representative" versus placeholder needs human judgment, keeping this Heuristic | Missing |

---

### Section 6 — Approved Template Assets

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| repository-onboarding.6 | Approved reusable template assets used instead of ad hoc recreation: pipeline templates from `Docs/Templates/Pipelines/`, PR templates/automation from `Docs/Templates/PRs/`, Azure rollout artifacts preserving the two-layer source/packages model, template-owned scripts remaining with their template | Structure | Heuristic | Warning | One-Time | File-content diff: compare the repository's pipeline YAML/PR-template files against the canonical templates in `GlobalStandards/Docs/Templates/` for structural similarity; confirming an asset was genuinely copied/adapted from the template versus independently authored requires human judgment, keeping this Heuristic | Missing |

---

### Section 7 — Local Validation Entry Point

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| repository-onboarding.7 | Documented local validation script or equivalent command entry point exposed before PRs are opened, mirroring the dev-pipeline gates | Infrastructure | Binary | Hard-stop | One-Time | File-path scan: verify a documented local validation entry point (e.g., a `Validate.ps1`/`Scripts/*.ps1` wrapper invoking the installed `validation-engine` global dotnet tool) is referenced from the repository README/copilot-instructions — duplicates `GlobalRepositoryStandards.md` repository.5 and `GlobalAzureDevOpsPipelineStandards.md` local-validation-entry-point detection | Missing |

---

### Section 8 — Validation Pipeline

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| repository-onboarding.8 | Validation pipeline triggers on PRs targeting `dev`, builds the repository, runs required automated tests/repository-type validation checks, produces the traceable promoted output when required, and is registered as the required branch policy on `dev` | Infrastructure | Binary | Hard-stop | One-Time | Pipeline YAML inspection + ADO branch-policy query: verify PR trigger on `dev`, build/test stages present, and the pipeline is registered as a required policy — duplicates `GlobalAzureDevOpsPipelineStandards.md` Section 2.1 detection | Missing |

---

### Section 9 — Promoted Output and Traceability

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| repository-onboarding.9 | Repositories producing a deployable artifact/package/release bundle document promoted output mapping released content to originating commit SHA, source PR, and validation build/run identifier | Infrastructure | Heuristic | Hard-stop | One-Time | Pipeline artifact/publish-stage inspection: verify build metadata (commit SHA, PR number, run ID) is attached to the published artifact; applicability first requires determining whether the repository type produces a deployable artifact, which needs human/AppTypeDetector confirmation, keeping this Heuristic | Missing |

---

### Section 10 — Release or Publication Pipeline

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| repository-onboarding.10 | Repositories producing a deployable artifact/package/release bundle implement the governed release/publication path; Web API/Web App repos use staging-validate-swap-validate and expose `/healthcheck`; database repos follow governed DACPAC delivery; library repos publish through the governed library release path; any repo requiring an ADO service connection uses the approved shared configuration | Infrastructure | Heuristic | Hard-stop | One-Time | Release pipeline YAML inspection for staging-swap sequence and `/healthcheck` endpoint presence; DACPAC/library publish-step inspection for repository types that apply; service-connection name/config comparison against approved shared list — repository-type branching requires classification (Heuristic) before applying the type-specific Binary check | Missing |

---

### Section 11 — Repository-Local Deviations

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| repository-onboarding.11 | Any approved repository-specific deviation adopted at creation is documented in a repository-local standards file under `Standards/` and linked from `.github/copilot-instructions.md` | Structure | Binary | Warning | One-Time | File-path scan: verify each deviation referenced in `copilot-instructions.md` has a corresponding file under `Standards/` — duplicates `GlobalSolutionStructureStandards.md` solution-structure.2.1 detection | Missing |

---

### Section 12 — GlobalStandards ValidationEngine Wiring

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| repository-onboarding.12 | `GlobalStandards` cloned as a sibling with a functional local pre-commit hook; pipeline YAML checks out `GlobalStandards` and runs the shared standards-validation stage against the PR merge-base diff; pipeline build identity holds PR-comment permissions; `AppTypeDetector` classifies the repository correctly and `CodeChanges` gate patterns match the repository's actual layout | Infrastructure | Heuristic | Hard-stop | One-Time | File-system check for sibling clone + pre-commit hook script; pipeline YAML inspection for `GlobalStandards` repository resource and shared templates; ADO permission query for build identity; manual/`-Mode Manual` ValidationEngine run to confirm correct app-type classification — the classification-correctness check requires human confirmation, keeping this Heuristic | Missing |

---

### Section 13 — Compliance Verification

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| repository-onboarding.13 | Checklist aggregating Sections 2-12 into a single pass/fail confirmation list for repository creation | Procedural | N/A (Aggregate) | N/A | N/A | Pure aggregation/index of the Section 2-12 checks; automation should evaluate the underlying section markers directly rather than this rollup row | Missing |

---

### Section 14 — Governance

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| repository-onboarding.14 | This standard is owned by Troy Crowe; no changes may be merged without explicit approval; changes must be submitted as a PR with a rationale comment; direct commits to `dev`/`main` are not permitted | Procedural | Manual-only | Manual-only-comment | One-Time | Ownership/approval-process statement — not an independently observable repository-state fact; enforcement relies on ADO branch-policy required-reviewer configuration already covered under Section 4 | Missing |
