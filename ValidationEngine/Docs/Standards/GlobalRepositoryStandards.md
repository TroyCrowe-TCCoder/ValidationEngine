# Repository Standards

**Version:** 1.7.0
**Status:** Active
**Applies To:** All repositories under [https://dev.azure.com/tcrowe0170](https://dev.azure.com/tcrowe0170)
**Audience:** AI models and human developers
**Last Modified:** 2026-07-23
**Owner:** Troy Crowe

**Dependencies:**
- Root governance standard — governance framework, rule precedence, and exception process
- [`GlobalAzureDevOpsPipelineStandards.md`](GlobalAzureDevOpsPipelineStandards.md) — pipeline behavior for each application type and environment

---
<!-- STD-MARKER: repository.file -->


## 1. Purpose
<!-- STD-MARKER: repository.1 -->

This document is the authoritative standard for repository structure, branch management, check-in rules, and pull request requirements. It applies to every repository under the GlobalStandards governance model. Both human developers and AI agents must follow every rule in this file. Approved repository-specific deviations are the standard override mechanism for all repositories governed by GlobalStandards. Any repository-specific deviation from this standard must be documented and approved using the root governance exception process. When an approved deviation is documented in the repository-local addendum, that repository-specific rule takes precedence for that repository only.

---

## 2. Branch Model
<!-- STD-MARKER: repository.2 -->

### 2.1 Required Branches
<!-- STD-MARKER: repository.2.1 -->

Every repository must have the following long-lived branches created at setup before any development work begins:

| Branch | Purpose | Created by |
|---|---|---|
| `main` | Production root branch | Azure DevOps at repository creation |
| `dev` | Integration branch | Manually from `main` immediately after repository creation |

### 2.2 Delivery Branch Types
<!-- STD-MARKER: repository.2.2 -->

| Type | Pattern | Branched from |
|---|---|---|
| Feature work | `feature/<descriptive-name>` | Refreshed local `dev` |
| Hotfix work | `hotfix/<descriptive-name>` | Refreshed local `dev` |

Branch names must be descriptive. A branch name must not be a ticket number alone or a generic label such as `changes` or `updates`.

### 2.3 Protected Branches
<!-- STD-MARKER: repository.2.3 -->

`main` and `dev` are protected branches. Neither may be committed to directly under any circumstance. All changes must arrive via pull request.

Local standards-validation enforcement, as required by [`GlobalDeveloperOnboardingStandards.md` Section 4](GlobalDeveloperOnboardingStandards.md#4-step-2--enable-local-standards-validation-enforcement), must block a local commit whose current branch is `dev` or `main` before the commit completes, using the message: "Feature branch required, cannot check directly into Dev or Main". This local check is a stop-gap in addition to, not a replacement for, the branch-policy enforcement in Azure DevOps.

---

## 3. Check-In Rules
<!-- STD-MARKER: repository.3 -->

1. No direct commits to `dev` are permitted.
2. No direct commits to `main` are permitted.
3. All changes must flow through pull requests using the `feature/* → dev → main` flow.
4. Every `feature/*` or `hotfix/*` branch must be created from a refreshed local `dev`:
   - `git checkout dev`
   - `git pull origin dev`
   - `git checkout -b feature/<descriptive-name>`
5. Work is completed, committed, and pushed to the remote feature branch.
6. A pull request is opened from the feature branch to `dev`.

Before Step 6, the developer must run the repository's documented local validation script or equivalent command entry point and resolve any failures. The pipeline is not the first validation pass.

Commit, push, and pull request creation are a single atomic operation. After a branch is pushed to the remote, a pull request targeting `dev` must be created immediately in the same sitting. A pushed branch with no open pull request is a non-conforming state. Work in progress must not be pushed without a corresponding open pull request.

For repositories that produce a deployable artifact, package, or release bundle, the `feature/* → dev` flow must also produce the traceable output that will later be promoted. Delivery must not rely on an untracked rebuild of different content as the authoritative release source.

---

## 4. Pull Request Requirements
<!-- STD-MARKER: repository.4 -->

### 4.1 Required PR Flow
<!-- STD-MARKER: repository.4.1 -->

```
feature/* → dev → main
hotfix/*  → dev → main
```

Direct PRs from any branch to `main` are not permitted. All changes must pass through `dev` first.

Promotion to `main` must occur through a `dev → main` pull request. A merge to `main` without an active or newly created `dev → main` PR is not a conforming delivery action.

### 4.2 PR Creation Checklist
<!-- STD-MARKER: repository.4.2 -->

Every pull request must satisfy all of the following at creation time unless an approved repository-specific deviation replaces the default requirement:

- [ ] Source is a `feature/*` or `hotfix/*` branch
- [ ] Target is `dev`
- [ ] Title is descriptive — clearly identifies what changed; not a branch name, ticket number alone, or generic label
- [ ] Description follows the format in [Section 4.3](#43-pr-description-format)
- [ ] Auto-complete is enabled at creation time — not added after the fact
- [ ] The required reviewer is set by the target branch policy in ADO when reviewer approval is required for that repository
- [ ] The repository's documented local validation script or equivalent command entry point has been run successfully against the pending change set

### 4.3 PR Description Format
<!-- STD-MARKER: repository.4.3 -->

Every pull request must include a structured description in the following format:

```
<Title line>

<Summary paragraph — one or more sentences describing what the change does and why.>

Changes:
- <Bullet item describing one discrete change>
- <Bullet item describing one discrete change>
```

Rules:
- The title line must match the PR title exactly.
- The summary paragraph must explain the intent of the change, not restate the title.
- The `Changes:` section must list each discrete change as a separate bullet item.
- This format is required on all `feature/*` → `dev` and `hotfix/*` → `dev` PRs.

### 4.4 Auto-Complete Behavior
<!-- STD-MARKER: repository.4.4 -->

Auto-complete must be enabled on every pull request at creation time unless an approved repository-specific deviation defines different merge behavior. Auto-complete merges the PR automatically once all branch policies are satisfied:

- Build validation pipeline passes when the target branch requires validation (see [Section 5](#5-validation-pipeline-requirement))
- Required reviewer approval has been recorded when reviewer approval is required for the target branch
- All other ADO branch policies on the target branch are satisfied

Auto-complete does not bypass policies. It waits for them. It must not be added after the fact.

For `dev → main` PRs in the current delivery model, auto-complete must wait only for the non-approval branch policies configured on `main`. The `dev → main` path promotes the already-validated change set and must not require a second human approval gate as part of the default standard.

### 4.5 Merge Strategy
<!-- STD-MARKER: repository.4.5 -->

No specific merge strategy (squash, rebase, or basic merge) is mandated by default for `dev` or `main`. A repository may allow any combination of merge types unless an approved repository-specific deviation restricts them. The source (feature) branch must still be deleted automatically on merge.

The automated carry-forward PR chain described in [Section 6](#6-dev-to-main-promotion-pr-manual) does not depend on a specific merge strategy being enforced.

### 4.6 Required Reviewer
<!-- STD-MARKER: repository.4.6 -->

Troy Crowe is the default required reviewer for pull requests targeting approval-gated branches such as `dev`. This is enforced as an ADO branch policy — not hardcoded in pipeline YAML. In the current `feature/* → dev → main` model, `main` is not approval-gated by default because `dev → main` is a time-gated promotion path for an already-validated change set. An approved repository-specific deviation may add, remove, or replace the reviewer requirement for a repository or branch. The reviewer may be changed at any time by updating the branch policy in ADO. No standards file change is required when the reviewer changes.

### 4.7 Approval Ownership
<!-- STD-MARKER: repository.4.7 -->

Approval belongs to the pull-request process through Azure DevOps branch policy. Delivery execution must not introduce a second approval gate after the `dev → main` PR has merged unless an approved repository-specific deviation explicitly requires it.

---

## 5. Validation Pipeline Requirement
<!-- STD-MARKER: repository.5 -->

Every `feature/*` → `dev` pull request must pass a validation pipeline before it can merge unless an approved repository-specific deviation removes that requirement. The pipeline is defined in [`GlobalAzureDevOpsPipelineStandards.md`](GlobalAzureDevOpsPipelineStandards.md) and varies by application type.

Every repository must also provide a documented local validation script or equivalent command entry point that developers run before PR creation or update. The local validation entry point must mirror the dev-pipeline gates as closely as the local development environment allows.

The `feature/* → dev` validation path is the authoritative source of code-level validation. Build validation, automated test execution, security/package checks, and repository-type validation must be owned here. Production delivery may perform integrity checks needed for safe deployment, but it must not become the primary place where unit-test or code-correctness failures are first discovered.

For repositories that produce a deployable artifact, package, or release bundle, the validation pipeline must create a traceable output that can be promoted later. At minimum, the promoted output must be traceable to the originating commit SHA, source PR, and build run.

All application types share this behavior for the dev pipeline:

1. **Auto-triggered** on pull requests targeting `dev`
2. **Validates the build** — the project must compile without errors
3. **All tests pass** — unit tests and any applicable validation scripts must pass
4. **Any failure notifies the developer** — a comment is posted to the PR identifying the failing step, with a direct link to the failed run

The pipeline must not deploy anything, connect to any remote environment, or perform any database operations.

See [`GlobalAzureDevOpsPipelineStandards.md` Section 2.1](GlobalAzureDevOpsPipelineStandards.md#21-dev-environment) for the full per-application-type pipeline behavior specification.

---

## 6. Dev-to-Main Promotion PR (Manual)
<!-- STD-MARKER: repository.6 -->

Any existing pipeline template or repository that still creates the `dev → main` PR automatically is stale and must be corrected to the manual model below the next time that repository or template is updated.

When a `feature/*` → `dev` PR merges, the `dev → main` PR must be created manually by the repository owner or release owner once the change set is ready for promotion. It is not auto-created by pipeline automation.

The `dev → main` PR is the required promotion record for the change set moving toward production. It must exist for each conforming promotion to `main`.

The `dev → main` PR promotes the already-validated change set. For repositories that produce a deployable artifact, package, or release bundle, the promotion record must map to the traceable output created during the `feature/* → dev` validation path.

**Current chain:**

```
feature/* → dev  →  (manual)  →  dev → main
```

If a `dev → main` PR already exists, the repository must use that active PR as the promotion record rather than creating a second competing PR. The existing PR must be reviewed to confirm it still accurately represents the current `dev` branch content before merge.

---

## 7. ADO Branch Policy Configuration
<!-- STD-MARKER: repository.7 -->

The following policies must be configured in Azure DevOps for every repository unless an approved repository-specific deviation replaces a default policy. These are set manually in the ADO portal at **Repos → Branches → (branch) → Branch Policies**.

### 7.1 Policies Required on `dev`
<!-- STD-MARKER: repository.7.1 -->

| Policy | Setting |
|---|---|
| Require a minimum number of reviewers | Minimum reviewers: 1; Required reviewer: Troy Crowe; Block self-approval: yes, unless an approved repository-specific deviation removes or replaces the reviewer requirement |
| Require a merge strategy | Not mandated — any merge type may be allowed unless an approved repository-specific deviation restricts them |
| Build validation | Validation pipeline registered and set as required; trigger: automatic |
| Delete source branch | Enabled — delete feature branch automatically on merge |

### 7.2 Policies Required on `main`
<!-- STD-MARKER: repository.7.2 -->

| Policy | Setting |
|---|---|
| Require a merge strategy | Not mandated — any merge type may be allowed unless an approved repository-specific deviation restricts them |
| Delete source branch | Enabled — delete `dev`-equivalent source on merge |

No reviewer approval policy is required on `main`. Promotion from `dev → main` is gated by the delivery time-window check enforced in the production pipeline. See [`GlobalAzureDevOpsPipelineStandards.md` Section 2.3](GlobalAzureDevOpsPipelineStandards.md#23-production-environment) for the required window check implementation.

### 7.3 Enforcement Status
<!-- STD-MARKER: repository.7.3 -->

All required policies listed in Sections 7.1 and 7.2 are enforced in Azure DevOps. The following confirms the current applied state for the GlobalStandards repository:

- [x] `dev` — Require a minimum number of reviewers (Troy Crowe, required; block self-approval: yes)
- [x] `dev` — Build validation policy (validation pipeline linked and set as required)
- [x] `main` — No reviewer approval policy; delivery gated by CST time-window check in production pipeline

---

## 8. Working and Temporary Files
<!-- STD-MARKER: repository.8 -->

Working files, scratch files, and any other file created for temporary use must never be committed to the repository. They must remain untracked for the lifetime of the work they support, and must be deleted as soon as they are no longer needed. A file is no longer needed when the work it supported has been completed or abandoned.

### 8.1 Definition
<!-- STD-MARKER: repository.8.1 -->

A working or temporary file is any file that:

- Was created to draft, iterate on, or stage content that belongs elsewhere once finalized
- Was created to support a session, context-handoff, or debugging task that has since concluded
- Has a name that indicates its temporary nature (e.g., prefixed with `_scratch_`, `_draft_`, `_temp_`, or placed under `Working/`)
- Has no ongoing reference from any committed standard, pipeline, or documentation file

### 8.2 Rule
<!-- STD-MARKER: repository.8.2 -->

| Rule | Requirement |
|---|---|
| Never committed | Working or temporary files must never be added to source control. The folder(s) that hold them (for example `Working/`) must be listed in the repository's root `.gitignore`. |
| Declared active status | Every working or temporary file must declare its own retention state in a `Status:` metadata line near the top of the file (for example `Status: Active`, `Status: In Review`, `Status: Stale — pending deletion`). A file's mere presence is never itself a reason to keep it — it is retained only while its `Status:` line shows it is actively carrying work forward. |
| No accumulation | Working files must not accumulate. A file marked `Stale` must be deleted immediately rather than left in place, and any file whose supporting work has concluded must be deleted even if its status line was never updated. |

### 8.3 Enforcement
<!-- STD-MARKER: repository.8.3 -->

Because working and temporary files are untracked and gitignored, they are never present in a pull request diff and cannot be enforced through code review. Enforcement instead relies on the `Status:` metadata line: any human developer or AI model working in the repository must check the retention state of files in `Working/` (or an equivalent scratch folder) at the start and end of a session, and must delete any file that is `Stale` or whose supporting work has concluded, regardless of who created it.

---

## 9. Azure Rollout Artifact Placement
<!-- STD-MARKER: repository.9 -->

Repositories that capture or package Azure rollout infrastructure must store those artifacts in a structure that preserves raw source captures separately from rollout packages. The repository structure must make it clear which artifacts are descriptive source records and which artifacts are grouped deployment units intended for rollout automation.

### 9.1 Required Structure
<!-- STD-MARKER: repository.9.1 -->

When a repository contains Azure rollout artifacts, it must use the following placement model unless an approved repository-specific deviation defines a more specific equivalent:

- Raw suite-level exports and per-resource captures must be stored under `Docs/Templates/Azure/<scope>/<name>/source/`.
- Rollout-unit manifests, grouped templates, and deployment-oriented packaging artifacts must be stored under `Docs/Templates/Azure/<scope>/<name>/packages/`.
- Full resource-group exports must remain preserved as source artifacts even when more granular package artifacts are added.
- Package artifacts must group resources by deployment unit so that resources that roll out together are represented together.

### 9.2 Repository Rule
<!-- STD-MARKER: repository.9.2 -->

Azure rollout artifacts must not mix raw source captures and rollout packages in the same folder without a structure that distinguishes their purpose. Documentation that references rollout automation must point to the package layer for grouped deployment intent and to the source layer for authoritative raw captures.

---

## 10. Compliance Verification
<!-- STD-MARKER: repository.10 -->

Both human developers and AI models must verify the following items before marking repository-governance work complete. A pull request that changes repository process or policy must not be merged until every applicable item has been checked.

- [ ] `main` and `dev` are protected branches. <!-- STD-MARKER: repository.10.1 -->
- [ ] No direct commits to `main` or `dev` are permitted. <!-- STD-MARKER: repository.10.2 -->
- [ ] All changes flow through the `feature/* → dev → main` or `hotfix/* → dev → main` branch flow. <!-- STD-MARKER: repository.10.3 -->
- [ ] Every feature or hotfix branch is created from a refreshed local `dev` branch. <!-- STD-MARKER: repository.10.4 -->
- [ ] Every `feature/*` or `hotfix/*` pull request to `dev` uses the required title and description format. <!-- STD-MARKER: repository.10.5 -->
- [ ] Auto-complete is enabled at pull request creation time unless an approved repository-specific deviation defines different merge behavior. <!-- STD-MARKER: repository.10.6 -->
- [ ] Required reviewer policy is enforced on the target branch unless an approved repository-specific deviation removes or replaces that requirement. <!-- STD-MARKER: repository.10.7 -->
- [ ] Approval is enforced in the pull-request branch-policy process and is not reintroduced as a second gate in delivery execution unless an approved repository-specific deviation requires it. <!-- STD-MARKER: repository.10.8 -->
- [ ] The validation pipeline requirement is enforced unless an approved repository-specific deviation removes that requirement. <!-- STD-MARKER: repository.10.9 -->
- [ ] The repository exposes a documented local validation script or equivalent command entry point that is required before PR creation or update. <!-- STD-MARKER: repository.10.10 -->
- [ ] The `feature/* → dev` path owns build, test, security/package, and repository-type validation before merge. <!-- STD-MARKER: repository.10.11 -->
- [ ] Repositories that produce deployable artifacts, packages, or release bundles create a traceable promoted output during `feature/* → dev` validation. <!-- STD-MARKER: repository.10.12 -->
- [ ] `dev → main` behavior follows either the default automated carry-forward model or an approved repository-specific deviation documented through the root governance exception process. <!-- STD-MARKER: repository.10.13 -->
- [ ] Only one active `dev → main` promotion PR exists per repository at a time. <!-- STD-MARKER: repository.10.14 -->
- [ ] Azure DevOps branch policies match the required repository configuration unless an approved repository-specific deviation replaces a default policy. <!-- STD-MARKER: repository.10.15 -->
- [ ] Every approved repository-specific deviation is documented in the repository-local addendum. <!-- STD-MARKER: repository.10.16 -->
- [ ] Azure rollout artifacts, when present, separate raw source captures from rollout packages under the documented template structure. <!-- STD-MARKER: repository.10.17 -->
- [ ] Local standards-validation enforcement blocks a local commit made directly to `dev` or `main`. <!-- STD-MARKER: repository.10.18 -->

---

## 11. Governance
<!-- STD-MARKER: repository.11 -->

This standard is owned by Troy Crowe. No changes to this file may be merged without Troy Crowe's explicit approval. Changes must be submitted as a pull request with a rationale comment explaining the reason for the update. Direct commits to `dev` or `main` are not permitted.
