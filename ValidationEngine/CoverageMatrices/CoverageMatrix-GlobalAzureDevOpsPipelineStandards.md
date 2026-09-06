# Coverage Matrix — GlobalAzureDevOpsPipelineStandards.md

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

**Applicability note:** Per `StandardsChangeTriggerMatrix.csv`, this file's rules only come into play when `.azure-pipelines/**/*.yml` files change — not when application code changes. Most of this file's content (Step 0–13 rollout runbook, canonical template layout, YAML patterns) is a one-time-per-repository rollout procedure rather than an ongoing per-commit rule; only Sections 3.1–3.5.2 and 4 describe checkable, repeated-validation-worthy conditions. Category is **Infrastructure** for most rows since these validate ADO project/pipeline configuration state, not application source structure.

---

### Section 2 — Pipeline Behavior by Environment (Dev/QA/Production)

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| azure-devops-pipeline.2.1 | Dev pipeline defines the fixed stage sequence (doc-change skip, build, test, promoted-output creation) in the required order, with failure handling and test-gated feed publication | Infrastructure | Binary | Hard-stop | Every-Commit (when `.azure-pipelines/**/*.yml` files change) | YAML-structure scan: parse changed pipeline YAML and verify the required stages exist in order, each failure path posts a PR comment and marks the pipeline failed, and library feed-publish steps are declared after (depend on) the test stage, not before it | Missing |
| azure-devops-pipeline.2.1.1 | Repository exposes a local validation entry-point script mirroring the dev pipeline sequence | Structure | Binary | Hard-stop | Every-Commit | File-presence: repository-local `Scripts/Validate.ps1` (or per-type equivalent) exists | Missing |
| azure-devops-pipeline.2.1.2 | Promoted output metadata traces to commit SHA, PR, and validation build run | Infrastructure | Manual-only | Manual-only-comment | Periodic (suggested default: quarterly) | Requires inspecting actual pipeline run artifacts/metadata in ADO, not source-inspectable | Missing |
| azure-devops-pipeline.2.2 | QA environment behavior (not yet active; placeholder rules) | Infrastructure | Manual-only | Manual-only-comment | Periodic (suggested default: quarterly) | Not applicable until QA environment exists; acknowledged future work | Acknowledged-future-work |
| azure-devops-pipeline.2.3 | Production pipeline fixed sequence (window check, integrity validation, deploy, swap-validate, rollback) | Infrastructure | Manual-only | Manual-only-comment | Periodic (suggested default: quarterly) | Requires inspecting live pipeline YAML/ADO run behavior, not source-diff checkable | Missing |
| azure-devops-pipeline.2.3.1 | Window-check PowerShell pattern must match the required verbatim implementation | Technology | Binary | Hard-stop | Every-Commit (when `.azure-pipelines/**/*.yml` files change) | Text-scan: diff the window-check script block in changed pipeline YAML against the canonical pattern | Missing |
| azure-devops-pipeline.2.3.2 | Scheduled delivery-pickup mechanism exists as a file (e.g. `.azure-pipelines/Main/schedule.yml`), checks for a delivery-worthy promoted output, exits cleanly if none exists, and reuses the window-check pattern to route into the normal delivery sequence when one exists | Infrastructure | Binary | Hard-stop | Every-Commit (when `.azure-pipelines/**/*.yml` files change) | File-presence + YAML-structure scan: verify the scheduled-entrypoint file exists for API/Web App repositories and its structure checks for a promoted output, exits cleanly when absent, and shares/matches the window-check pattern from `azure-devops-pipeline.2.3.1` | Missing |
| azure-devops-pipeline.2.3.3 | Scheduled trigger configured directly in ADO (not solely YAML `schedules:` block), disabled until go-live | Infrastructure | Manual-only | Manual-only-comment | Periodic (suggested default: quarterly) | Requires inspecting live ADO pipeline trigger configuration via ADO API/portal, not source-inspectable | Missing |

---

### Section 3 — Pull Request Standards

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| azure-devops-pipeline.3.1 | PR created with descriptive title, structured description (self-link, change list, summary), auto-complete enabled at creation | Procedural | Manual-only | Manual-only-comment | Every-Commit (at PR-creation time, not local commit) | Requires ADO REST API inspection of the PR object at creation time — not checkable from a local staged-diff/pre-commit script; belongs to a PR-time check, not this process | Missing |
| azure-devops-pipeline.3.2 | Required approver enforced via ADO branch policy per target branch (dev/qa gated, main not gated) | Infrastructure | Manual-only | Manual-only-comment | Periodic (suggested default: quarterly) | ADO branch-policy configuration fact, not repository state; requires ADO project-config audit | Missing |
| azure-devops-pipeline.3.3 | Automated PR chain (carry-forward) behavior | Infrastructure | Manual-only | Manual-only-comment | Periodic (suggested default: quarterly) | ADO project/pipeline configuration fact, not source-inspectable | Missing |
| azure-devops-pipeline.3.4 | PR chain configuration per repository (`devNextBranch`/`qaNextBranch` tokens in `common.yml`) | Technology | Binary | Hard-stop | Every-Commit (when `.azure-pipelines/**/*.yml` files change) | Text-scan: parse `common.yml` for required token keys and values | Missing |
| azure-devops-pipeline.3.5 | Canonical template layout conventions (folder grouping under `Docs/Templates/Pipelines/{RepositoryType}/{Environment}/`, no stray `shared/`/`variables/` folders) | Structure | Heuristic | Warning | Every-Commit (GlobalStandards repo only — this rule applies to the template store itself, not consuming repositories) | File-path convention scan of the GlobalStandards template directory tree | Missing |
| azure-devops-pipeline.3.5.1 | Pipeline template files use two-digit zero-padded numeric prefix + PascalCase stage name, contiguous, matching YAML `stage`/`dependsOn` order | Structure | Binary | Hard-stop | Every-Commit (when template files change) | File-naming regex + YAML parse: verify prefix sequence and cross-check against declared `dependsOn` order | Missing |
| azure-devops-pipeline.3.5.2 | Each repository type has a `Validate.ps1` template + `Readme.md` under its `Scripts/` folder | Structure | Binary | Hard-stop | Every-Commit (GlobalStandards repo only) | File-presence check per repository-type template folder | Missing |

---

### Section 4 — Compliance Verification (Checklist)

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| azure-devops-pipeline.4 | Compliance checklist covering health-check response, rollback stage condition, DACPAC deploy-task flags, destructive-change detection, diff-report/script ordering, `enableAutoCorruptionRecovery` setting | Technology | Binary (partial) | Hard-stop | Every-Commit (when `.azure-pipelines/**/*.yml` files change) | Text-scan/YAML-parse of changed pipeline files for the specific literal patterns called out in the checklist (e.g., `BlockOnPossibleDataLoss=True` present, `BlockOnPossibleDataLoss=False` absent, `failed('Verify_Prod')` condition present) | Missing |

---

### Section 5 — Governance (Excluded)

`azure-devops-pipeline.5` (ownership/PR-approval boilerplate) is excluded — not code-checkable, procedural ownership statement only.

---

### Step 0–13 Rollout Runbook (Excluded from ongoing validation)

The "Step 0 — Determine Your Application Type" through "Step 13 — Verify the Full Delivery Flow" runbook content (unmarked, no STD-MARKER ids) is a one-time repository rollout procedure, not an ongoing per-commit/per-build rule. Consistent with `GlobalSolutionStructureStandards`'s exclusion of its own one-time migration Section 13, this runbook belongs to a repository-onboarding/rollout checklist process, not to this coverage matrix. Section 3.5.1's numbered-file-prefix rule is the one piece of this runbook content that *is* independently checkable on an ongoing basis (any time a template file is added/renamed), so it was pulled out and included above as its own marker rather than excluded with the rest of the runbook.

---

**Notes:**
1. This file is overwhelmingly **Infrastructure/ADO-configuration** in nature — most rules require inspecting the live Azure DevOps project (branch policies, scheduled triggers, PR objects, pipeline run history) rather than anything visible in a git diff. This mirrors the `StandardsValidationMatrix.README.md`'s own three-way split of `GlobalRepositoryStandards` content (in-scope local-state checks vs. PR-time ADO-API checks vs. config-audit-only checks) — the same split applies here.
2. Only Section 2.3.1 (window-check script pattern), 3.4 (PR-chain token config), 3.5.1 (numbered template prefixes), 3.5.2 (Validate.ps1 template presence), and Section 4 (the DACPAC/pipeline-YAML checklist) are genuinely source-file-diffable and belong at Pre-checkin/Pipeline tier once implemented. Section 2.1.1 (local validation entry-point script presence) is also source-diffable at the consuming-repository level.
3. Section 3.1 (PR creation requirements) explicitly belongs to a PR-time ADO-REST-API check per the `StandardsValidationMatrix.README.md`'s existing determination for `GlobalRepositoryStandards` Sections 4.2/4.3 — flagged here as the same category of out-of-process-scope-but-real-future-check.
4. The unmarked Step 0–13 rollout runbook is excluded from this matrix as a one-time procedure, consistent with how `GlobalSolutionStructureStandards.md` Section 13 (Remediation Paths) was excluded in the applicability matrix.

