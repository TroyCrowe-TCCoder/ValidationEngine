# Coverage Matrix — GlobalRepositoryStandards.md

**Status:** Reviewed
**Sign Off:** Troy Crowe
**Sign Off Date:** 2026-08-14
**Revision Date:**
**Columns**

| Column | Meaning |
|---|---|
| STD-MARKER | Rule id from the standards file. |
| Rule Summary | One-line paraphrase of the rule. |
| Category | Procedural / Infrastructure / Development. |
| Confidence | Binary (deterministic pass/fail) / Heuristic (risk-flag, needs human confirmation) / Manual-only (no automation path). |
| Severity | Hard-stop / Warning / Manual-only-comment. |
| Frequency | Every-Commit (change-set scoped) / Every-PR / Periodic (recurring cadence). |
| Technique | Short detection approach. |
| Status | Missing / Existing / Acknowledged-future-work. |

**Applicability note:** This file governs the Azure DevOps repository/branch/PR process itself, not application source code. Detection here is dominated by **Azure DevOps REST API / `az repos` CLI queries** (branch policy configuration, PR metadata, PR description content) rather than Roslyn or file-tree scanning. Several rules are inherently "approved repository-specific deviation" escape-valved — meaning a hard-stop rule is not violated if a documented local addendum exists, so any automated check must also check for the deviation record before flagging a failure.

---

### Section 2 — Branch Model

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| repository.2.1 | `main` and `dev` long-lived branches must exist, created at repo setup before development begins | Infrastructure | Binary | Hard-stop | Periodic (repo-setup / onboarding check) | `az repos ref list` or ADO REST API: verify both `refs/heads/main` and `refs/heads/dev` exist | Missing |
| repository.2.2 | Delivery branches follow `feature/<descriptive-name>` or `hotfix/<descriptive-name>` pattern, branched from refreshed local `dev`; branch name must not be a bare ticket number or generic label (`changes`, `updates`) | Procedural | Heuristic | Warning | Every-PR | Regex on branch name: enforce `feature/` or `hotfix/` prefix; flag names matching a ticket-number-only pattern (e.g., `^\d+$` or `^[A-Z]+-\d+$`) or a generic-label denylist (`changes`, `updates`, `fix`, `wip`); "branched from refreshed local dev" (freshness) is not verifiable after the fact from the remote alone | Missing |
| repository.2.3 | `main` and `dev` are protected — no direct commits under any circumstance; all changes via PR; local pre-commit hook must block commits on `dev`/`main` with a specific message | Infrastructure | Binary | Hard-stop | Every-Commit | Two-part detection: (1) **Local pre-commit hook** — read the current branch name (`git rev-parse --abbrev-ref HEAD`) and block the commit with the message "Feature branch required, cannot check directly into Dev or Main" if it equals `dev` or `main` exactly; no ADO API call needed for this half. (2) **Server-side** — there is no dedicated "block direct commit" toggle in ADO; direct pushes are blocked as an implicit side effect of any required branch policy (e.g., minimum reviewers, build validation) existing on the branch. Verify at least one required policy is configured on `main`/`dev` AND that the "Bypass policies when pushing"/"Exempt from policy enforcement" permission is denied for standard contributor identities (including admins, in practice) — both conditions must hold for direct-push protection to actually be in effect | Missing |

---

### Section 3 — Check-In Rules

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| repository.3 | No direct commits to `dev`/`main`; all changes via `feature/* → dev → main` PR flow; feature/hotfix branches created from refreshed local `dev`; local validation script run and passed before PR creation; commit+push+PR-creation treated as a single atomic operation (no pushed branch left without an open PR); repos producing deployable artifacts must produce traceable output via the `feature/* → dev` flow | Procedural | Heuristic | Hard-stop | Every-PR / Every-Commit | ADO REST API: enumerate remote branches with no associated open PR and flag as non-conforming ("pushed branch, no PR"); direct-commit prevention duplicates repository.2.3; "local validation script was run" is not verifiable from ADO metadata alone unless the pipeline independently re-runs the same checks (which it does per Section 5) — so this sub-rule is effectively covered by the Section 5 pipeline gate rather than directly | Missing |

---

### Section 4 — Pull Request Requirements

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| repository.4.1 | Required PR flow is `feature/* → dev → main` / `hotfix/* → dev → main`; direct PRs to `main` not permitted; promotion to `main` only via a `dev → main` PR | Procedural | Binary | Hard-stop | Every-PR | ADO REST API: query PR list, flag any PR whose target branch is `main` and whose source branch is not `dev` | Missing |
| repository.4.2 | PR creation checklist: source is `feature/*`/`hotfix/*`; target is `dev`; title descriptive (not branch name/ticket/generic label); description follows the format in 4.3; auto-complete enabled at creation; required reviewer set by branch policy; local validation script run successfully | Procedural | Heuristic | Hard-stop | Every-PR | ADO REST API: verify PR source/target branch pattern, title-format heuristic (denylist generic labels/ticket-only titles), auto-complete flag set at creation timestamp (not added later — requires comparing PR creation event history), and reviewer-policy presence; description format is checked by repository.4.3; "local validation script run" is not independently verifiable, covered by the dev-pipeline re-validation instead | Missing |
| repository.4.3 | PR description must contain: title line matching PR title exactly, a summary paragraph (not restating the title), and a `Changes:` bulleted list of discrete changes | Procedural | Heuristic | Warning | Every-PR | Text-pattern check on PR description: verify a `Changes:` section header exists followed by bullet items; verify a non-empty paragraph precedes it; "does not restate the title" and "explains intent" require semantic judgment beyond pattern matching, so this rule is Heuristic even though the structural skeleton is Binary-checkable | Missing |
| repository.4.4 | Auto-complete enabled on every PR at creation time (not added after the fact) unless an approved deviation defines different merge behavior | Procedural | Binary | Hard-stop | Every-PR | ADO REST API: compare PR creation payload/audit history for the auto-complete flag state at the initial creation event versus a later update — detects "enabled after the fact" as a violation | Missing |
| repository.4.5 | No specific merge strategy mandated by default for `dev` or `main`; any merge type may be allowed unless an approved deviation restricts them; source branch deleted automatically on merge | Infrastructure | Binary | Hard-stop | Every-PR / Periodic (policy audit) | ADO branch-policy query: verify "delete source branch" setting on both `dev` and `main`; no merge-strategy restriction check required | Missing |
| repository.4.6 | Troy Crowe is default required reviewer for PRs targeting approval-gated branches (`dev`); enforced via ADO branch policy, not hardcoded in pipeline YAML; `main` not approval-gated by default | Infrastructure | Binary | Hard-stop | Periodic (policy audit) | ADO branch-policy query: verify required-reviewer policy on `dev` lists the correct reviewer and self-approval is blocked; verify no reviewer-approval policy exists on `main` unless a deviation adds one | Missing |
| repository.4.7 | Approval belongs to the PR/branch-policy process; delivery execution must not introduce a second approval gate after `dev → main` merges, unless an approved deviation requires it | Infrastructure | Manual-only | Manual-only-comment | Periodic (pipeline-design review) | Requires inspecting pipeline YAML for a manual-approval gate/environment-approval-check step after merge — a Heuristic detection is feasible (scan pipeline YAML for `approvals:`/environment checks post-merge) but classified Manual-only here pending confirmation this is worth automating given its rarity | Missing |

---

### Section 5 — Validation Pipeline Requirement

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| repository.5 | Every `feature/* → dev` PR must pass a validation pipeline before merge (unless deviation removes it); every repo must expose a documented local validation script mirroring the dev-pipeline gates; the pipeline auto-triggers on PR, validates build, runs all tests, and notifies the developer with a PR comment identifying the failing step/run link on failure; repos producing deployable artifacts must produce output traceable to commit SHA/PR/build run; production pipeline must not perform build/test discovery, deploy, connect to remote environments, or perform DB operations | Infrastructure | Heuristic | Hard-stop | Every-PR | ADO branch-policy query: verify "Build validation" policy is required on `dev` and references an actual pipeline; pipeline-YAML scan: verify PR-trigger configuration, verify no deploy/DB-operation tasks exist in the dev pipeline definition (duplicates `GlobalAzureDevOpsPipelineStandards.md` coverage) — "documented local validation script exists" is a file-presence check (e.g., `Tools/*.ps1` referenced from a README), separate from the pipeline check itself | Missing |

---

### Section 6 — Dev-to-Main Promotion PR (Manual)

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| repository.6 | Automated carry-forward PR chain is deprecated and must not be implemented in new repositories; the `dev → main` PR must be created manually by the repository/release owner once the change set is ready for promotion; existing open `dev → main` PR must be reused rather than duplicated | Infrastructure | Heuristic | Warning | Periodic (repository audit) | Pipeline-file scan: flag any repository whose pipeline templates still auto-create the `dev → main` PR on `feature/* → dev` merge (e.g. hard-coded `mergeStrategy`/PR-completion automation in a carry-forward template) as stale and due for correction; ADO REST API: verify no duplicate open `dev → main` PRs exist at any time | Missing |

---

### Section 7 — ADO Branch Policy Configuration

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| repository.7.1 | `dev` policies: min 1 reviewer (Troy Crowe, block self-approval) unless deviation; no mandated merge strategy; build validation required + auto-triggered; delete source branch enabled | Infrastructure | Binary | Hard-stop | Periodic (policy audit) | ADO branch-policy query against `dev`: verify each of the listed policy settings matches, excluding any merge-strategy restriction | Missing |
| repository.7.2 | `main` policies: no mandated merge strategy, delete source branch enabled; no reviewer-approval policy required (promotion gated by delivery time-window check in production pipeline instead) | Infrastructure | Binary | Hard-stop | Periodic (policy audit) | ADO branch-policy query against `main`: verify delete-source-branch setting; verify absence of a reviewer-approval policy (or presence, if intentionally added — flag as deviation-needs-documentation); no merge-strategy restriction check required | Missing |
| repository.7.3 | Enforcement-status checklist restating 7.1/7.2 as applied-state confirmation for the GlobalStandards repository specifically | Infrastructure | Binary | Hard-stop | Periodic (policy audit) | Same detection as repository.7.1/7.2 — this section is a point-in-time confirmation snapshot rather than an independent rule; duplicate coverage, no separate technique needed | Missing |

---

### Section 8 — Working and Temporary Files

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| repository.8.1 | Definition of a working/temporary file: drafts/staging content, session/handoff/debug support files now concluded, names indicating temporary nature (`_scratch_`, `_draft_`, `_temp_` prefixes, or under `Docs/Working/`), no ongoing reference from any committed standard/pipeline/doc; must never be committed to source control | Procedural | Heuristic | — (definitional, no independent severity) | — | Regex on filenames for temp-name prefixes and `Docs/Working/` path; "no ongoing reference from any committed file" requires a repo-wide reference/link scan — feasible but requires building a reference graph, more involved than a simple filename regex | Missing |
| repository.8.2 | Working/temp files never committed to source control; their folder(s) (e.g. `Docs/Working/`) must be listed in root `.gitignore`; every such file declares an active `Status:` metadata line (e.g. `Active`, `In Review`, `Stale — pending deletion`); files marked `Stale` or whose work has concluded must be deleted immediately, no accumulation | Procedural | Heuristic | Warning | Periodic (session start/end check) | `.gitignore` content check: verify `Docs/Working/` (or equivalent) is listed; file scan of the working folder (untracked, so not visible via PR diff) for a `Status:` line near the top of each file; flag files with no `Status:` line, a `Stale` status, or a status unchanged across a defined number of sessions as deletion candidates | Missing |
| repository.8.3 | Working/temp files are untracked and gitignored, so they are never present in a PR diff and cannot be enforced via code review; enforcement instead relies on session-start/session-end inspection of the `Status:` metadata line, deleting any file that is `Stale` or whose supporting work has concluded | Procedural | Manual-only | Manual-only-comment | Periodic (session start/end) | Not PR-diff-detectable by definition (untracked files); this is a session-hygiene check performed by whoever (human or AI model) is working in the repository, not an automatable PR-review gate | Missing |

---

### Section 9 — Azure Rollout Artifact Placement

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| repository.9.1 | Azure rollout artifacts (when present) stored under `Docs/Templates/Azure/<scope>/<name>/source/` (raw exports/captures) and `Docs/Templates/Azure/<scope>/<name>/packages/` (rollout-unit manifests/grouped templates); full resource-group exports preserved as source even after packages are added; package artifacts grouped by deployment unit | Structure | Heuristic | Warning | Every-Commit (when Azure template files change) | File-path scan: verify files under a repo's `Docs/Templates/Azure/` tree are correctly split between `source/` and `packages/` subfolders per scope/name; "grouped by deployment unit" is a content-judgment check beyond simple path matching | Missing |
| repository.9.2 | Raw source captures and rollout packages must not be mixed in the same folder without a structure distinguishing purpose; documentation referencing rollout automation must point to packages layer for grouped intent and source layer for raw captures | Procedural | Heuristic | Warning | Periodic (documentation review) | Same file-path scan as 9.1 for the folder-mixing check; the "documentation points to the correct layer" check requires link-target inspection in markdown docs, a lighter-weight but separate check | Missing |

---

### Section 10 — Compliance Verification (Excluded as an independent detection target)

The `repository.10.1`–`repository.10.x` checklist items restate Sections 2–9 verbatim as a pre-merge checklist. No independent detection techniques are introduced; each maps 1:1 to a rule already captured above.

### Section 11 — Governance (Excluded)

`repository.11` (ownership/exception-process boilerplate) is excluded — not code-checkable, procedural ownership statement only.

---

**Notes:**
1. This file is almost entirely **ADO REST API / `az repos` / `az devops` CLI-detectable** rather than source-code detectable — a notable third category alongside file-tree/naming scans (Solution Structure) and Roslyn semantic analysis (Coding/Security/Performance/Testing). A dedicated "ADO policy auditor" tool querying branch policies, PR metadata, and PR history would cover the majority of Sections 2, 4, 6, and 7.
2. Many rules here carry an explicit escape valve: "unless an approved repository-specific deviation..." — any automated enforcement must check for a documented deviation record (in the repo's local `Standards/` addendum per `GlobalSolutionStructureStandards.md` solution-structure.2.1) before flagging a hard-stop violation. This is a cross-cutting design requirement for the eventual validation engine, not a per-rule note.
3. repository.5 (validation pipeline requirement — build/test/notify) heavily duplicates `GlobalAzureDevOpsPipelineStandards.md`'s dev-pipeline behavior rules already captured in the accepted pipeline matrix; this file's version is the "process ownership" statement (who is responsible), while the pipeline standard is the "implementation" statement (how it's built).
4. repository.8 (working/temp file rules) parallels `GlobalSolutionStructureStandards.md` solution-structure.2.2 (`Working/` folder retention) almost exactly — both require the folder be gitignored/untracked, both require a `Status:` metadata line to make retention state inspectable, and both flag `Docs/Working/`-style folders as scratch-only. These should likely share a single detection rule referenced by both marker IDs rather than being implemented twice.
5. The "auto-complete enabled at creation time, not added after the fact" pattern (repository.4.2, 4.4) requires inspecting PR *event history*, not just current state — a materially different (and more expensive) query than a simple current-state check, worth flagging for the validation engine's design.

