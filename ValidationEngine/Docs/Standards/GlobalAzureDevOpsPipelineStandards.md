# Azure DevOps Pipeline Standards

**Version:** 1.5.0
**Status:** Active
**Applies To:** All repositories under [https://dev.azure.com/tcrowe0170](https://dev.azure.com/tcrowe0170)
**Audience:** AI models and human developers
**Last Modified:** 2026-07-22
**Owner:** Troy Crowe

**Dependencies:**
- Root governance standard — branch, PR, and approval rules
- [`GlobalSolutionStructureStandards.md`](GlobalSolutionStructureStandards.md) — repository layout and project naming conventions
- [`GlobalDatabaseStandards.md`](GlobalDatabaseStandards.md) — DACPAC deployment and migration rules

---
<!-- STD-MARKER: azure-dev-ops-pipeline.file -->

<!-- STD-MARKER: azure-devops-pipeline.file -->


## 1. Purpose
<!-- STD-MARKER: azure-devops-pipeline.1 -->

This document defines the authoritative workflow split for pipeline behavior across all governed repositories. `feature/* → dev` owns validation and creation of traceable promoted output. `dev → main` owns promotion of the already-validated change set through pull-request policy. Production delivery owns deploy, deployment validation, and rollback. Follow every step in order. Steps are tagged with the application types they apply to. Skip steps that do not apply to your application type.

---

## 2. Pipeline Behavior by Environment
<!-- STD-MARKER: azure-devops-pipeline.2 -->

This section defines what each pipeline environment must do for each application type. These rules are the authoritative source of truth. All pipeline templates and individual repository pipeline files must conform to this specification. Application-type standards files (e.g., `GlobalDatabaseStandards.md`) reference this section for pipeline behavior rules.

### 2.1 Dev Environment
<!-- STD-MARKER: azure-devops-pipeline.2.1 -->

The dev pipeline runs on every pull request targeting the `dev` branch. Its purpose is to confirm the change is safe before it merges. This behavior is permanent — it does not change when a QA environment is introduced.

**All application types — dev pipeline must follow this exact sequence. Each step must complete before the next begins. Any failure stops the pipeline immediately.**

Every repository must provide a documented local validation script or equivalent command entry point that mirrors this dev-pipeline sequence as closely as the local development environment allows. Developers must run that local validation entry point before creating or updating a pull request.

The `feature/* → dev` path is the authoritative validation workflow. Build validation, automated tests, security and package checks, and repository-type validation must be completed here before merge. For repository types that produce a deployable artifact, package, or release bundle, this path must also create the traceable promoted output used later in promotion and delivery.

**Step 1 — Check the PR for code changes.**

The first stage must detect whether the PR contains any files that affect compiled code, database schema, or pipeline configuration. Documentation-only changes (`.md` files, `docs/` folders, etc.) do not require a build or test run.

- **No code changes detected** — the pipeline must complete green immediately. All subsequent stages must be skipped. The build validation policy is satisfied and the PR merges via auto-complete.
- **Code changes detected** — proceed to Step 2.

**Step 2 — Validate the build.**

Compile the project. If the build fails, the pipeline must stop immediately, post a failure comment to the PR identifying the build failure and including a direct link to the failed run, and mark the pipeline as failed. Do not proceed to Step 3.

**Step 3 — Run tests.**

- For application and library repos: run the unit test suite.
- For database repos: run SQL validation and test script inspection.

- **Tests pass** — the pipeline must complete green. The build validation policy is satisfied and the PR merges via auto-complete.
- **Tests fail** — the pipeline must stop immediately, post a failure comment to the PR identifying which tests failed and including a direct link to the failed run, and mark the pipeline as failed.

**Step 4 — Create the promoted output.**

For repositories that produce a deployable artifact, package, or release bundle, the dev pipeline must create that output before completion and record its traceability metadata.

- **API or Web App** — produce the packaged deployable artifact that will be promoted later.
- **Database** — produce the release bundle containing the DACPAC and associated reviewed deployment artifacts.
- **Library** — produce the NuGet package and any required symbols package. Package artifact creation may precede Step 3 (tests) for build efficiency, but publication of the package to the organization's Azure Artifacts feed must never occur until Step 3 has completed successfully. Feed publication is delivery, not artifact creation, and must remain gated behind a passing test run.
- **Documentation-only repositories** — no promoted artifact is required.

The promoted output must be traceable to the originating commit SHA, source PR, and validation build run.

The dev pipeline must not deploy anything. It must not connect to any non-local environment. It must not generate diff scripts or perform any database operations.

### 2.1.1 Local validation entry point requirement
<!-- STD-MARKER: azure-devops-pipeline.2.1.1 -->

Each governed repository must expose a repeatable local validation script or equivalent command entry point that developers can run without reconstructing the gate sequence manually.

The local validation entry point must align to the dev pipeline for the repository type and must include, as applicable:

- restore or dependency acquisition
- build or compilation
- automated tests or repository-type validation steps
- required local security or package checks that are part of the repository's dev validation gate

If a pipeline gate cannot run locally in identical form, the repository must provide the closest practical local equivalent and document the difference.

The local validation entry point must not deploy, must not require a non-local target environment, and must not bypass branch policy or PR validation requirements.

### 2.1.2 Promoted output traceability requirement
<!-- STD-MARKER: azure-devops-pipeline.2.1.2 -->

When a repository type produces a deployable artifact, package, or release bundle, the dev pipeline must publish metadata that associates the promoted output with:

- the commit SHA
- the originating pull request
- the validation build number or run identifier

The promoted output must be the source referenced by later promotion and delivery behavior where the repository type supports immutable promotion.

---

### 2.2 QA Environment
<!-- STD-MARKER: azure-devops-pipeline.2.2 -->

> **Status: Not yet active.** No QA environment exists. QA pipeline templates are maintained as documented placeholders. The rules below describe the intended behavior when a QA environment is provisioned.

The QA pipeline runs on every pull request targeting the `qa` branch. Its purpose is to validate the build and deploy to the QA environment for human and automated testing.

**All application types — QA pipeline must:**

1. **Validate the build.** Compile the project. Fail immediately if the build does not succeed.
2. **If build passes — merge dev into QA and deploy to the QA environment.** No pipeline-level automated tests run before the deploy. Testing is performed by humans and automated test suites operating against the live QA environment after deployment.
3. **If build fails — block the PR.** Post a failure comment to the PR. Do not deploy.

The QA pipeline must not run unit tests in the pipeline before deploying. All testing activity happens in the QA environment after the deploy completes.

---

### 2.3 Production Environment
<!-- STD-MARKER: azure-devops-pipeline.2.3 -->

The production pipeline is a delivery workflow that runs after a PR merges to `main`. The `dev → main` path is not a second validation path and does not require a human approval gate as part of the delivery standard. Its responsibility is delivery-window enforcement, delivery of the already-validated change, live deployment validation, and rollback when needed.

Production delivery must consume the already-validated change set and, where the repository type supports it, the traceable promoted output created during the `feature/* → dev` validation path. Rebuilding in delivery may be used for integrity verification when explicitly required by repository type, but it must not replace the earlier authoritative validation result as the primary source of trust.

**All application types — production pipeline must follow this exact sequence. Each step must complete before the next begins. Any failure stops the pipeline immediately.**

**Step 1 — Check for delivery-worthy changes or promoted output.**

- **No delivery-worthy changes or promoted output detected** — the pipeline must complete green immediately. All subsequent stages must be skipped.
- **Code changes detected** — proceed to Step 2.

**Step 2 — Check the CST deployment window.**

This must be the first check after code changes are confirmed. No build, no compilation, no resources are consumed if the window check fails.

- **Outside the configured CST window (automated run)** — stop the delivery workflow cleanly with no error. The pipeline run must complete green. Do not use `SucceededWithIssues`, `throw`, or `exit 1`. All downstream steps must be condition-guarded so they skip silently. Do not wait, sleep, or hold an agent open for the window to arrive. Delivery must resume either through a manual rerun, which overrides the time gate, or through the next scheduled delivery-pickup run when the allowed window opens.
- **Manual run** — bypass the window check entirely and proceed regardless of time.
- **Inside window (automated run)** — proceed to Step 3.

Each deliverable repository must define how a paused delivery is picked up when the allowed window opens. This must be implemented as a scheduled delivery-pickup job or equivalent automation that runs near or within the configured delivery window, checks whether a delivery-worthy promoted output is available, and automatically proceeds with delivery when one exists. Holding a pipeline agent open waiting for the window is not permitted.

See Section 2.3.1 for the required window check implementation pattern.

**Step 3 — Validate release integrity.**

Confirm that the promoted output or release content to be deployed is present and valid for the repository type. If an integrity check fails, the pipeline must stop immediately, post a failure notification, and mark the pipeline as failed. Do not proceed.

- **API or Web App** — verify the packaged deployable artifact to be released.
- **Database** — verify the DACPAC and associated reviewed deployment artifacts.
- **Library** — no production delivery pipeline is required. Governed shared libraries are packaged as NuGet packages for consumption by other applications rather than rolled out to a live production environment.
- **Documentation-only repositories** — no production delivery pipeline is required.

**Step 4 — [Database only, else skip] Analyze schema changes.**

Prefer to generate and review the DACPAC diff before the delivery pipeline begins. When that is not possible, the delivery workflow must generate the diff before any deploy step runs. Evaluate what the diff contains:

- **Destructive changes detected (DROP COLUMN, DROP TABLE, column rename pattern) and no pre-deploy script is present** — the pipeline must stop immediately, post a notification identifying the destructive operations and the missing pre-deploy script, and mark the pipeline as failed. Do not deploy.
- **Destructive changes detected and a pre-deploy script is present** — proceed to Step 5.
- **Additive changes only (ADD COLUMN, new table, new index, new object) or no schema changes** — proceed to Step 5. A pre-deploy script is not required for additive-only changes.

The pre-deploy script check must verify that a `.sql` file exists in the project's pre-deploy script location. Its presence confirms the developer has addressed the destructive operation. The pipeline does not execute or validate the script contents — that responsibility belongs to the PR code review.

**Step 5 — Deploy.**

- **Database** — apply the DACPAC with `BlockOnPossibleDataLoss=True`. A pre-deploy backup must be created before the apply. See `GlobalDatabaseStandards.md` Section 10 for full rules.
- **API or Web App** — deploy to the staging slot. Direct production-slot deployment is not permitted as the standard release path.

If the deploy fails, proceed to the rollback and notification path in Step 6.

**Step 6 — Validate deployment.**

- **Database** — run post-deploy smoke validation against the deployed schema.
- **API or Web App** — verify the staging slot health endpoint returns the expected response.

For API and Web App repositories, deployment validation must follow a documented smoke-validation contract that defines the validation endpoint or route, expected status code, any required response assertion, and the validation timeout.

- **Validation passes** — for API/Web App: swap staging to production, then validate the production slot. For database: proceed to Step 7.
- **Validation fails** — rollback is required. Proceed to rollback and notification.

**Step 6a — [API/Web App only] Validate production after swap.**

Verify the production slot health endpoint returns the expected response after the swap.

- **Passes** — proceed to Step 7.
- **Fails** — swap the production slot back to the previous version. Post a rollback notification. If the rollback swap itself fails, post a second notification identifying the rollback failure. Mark the pipeline as failed.

This staging-validate-swap-validate pattern is the required no-downtime release path for API and Web App repositories unless an approved repository-specific deviation replaces it.

**Step 7 — Complete.**

The pipeline must complete green. No further action required.

---

**Failure notification requirements — all environments**

Every pipeline failure notification posted to a PR or sent as an alert must include:
- The step that failed (build, test, schema check, deploy, validation, rollback)
- A direct URL to the failed pipeline run
- The build number
- The repository and branch name

Notifications must be posted as a PR comment when the pipeline is running in PR context. For CI-triggered production runs, notifications must be posted as a pipeline run comment and surfaced in the run summary.

---

#### 2.3.1 Window check implementation — required pattern
<!-- STD-MARKER: azure-devops-pipeline.2.3.1 -->

The following PowerShell pattern must be used verbatim in all production pipelines. Deviations are not permitted. The production variable set must define `deliveryWindowStartHourCst` and `deliveryWindowEndHourCst` as integer hour values from `0` to `23`. The current default approved values remain `20` and `5`.

```powershell
if ('$(Build.Reason)' -eq 'Manual')
{
  Write-Host 'Manual run — deployment window check bypassed.'
  Write-Host '##vso[task.setvariable variable=skipDeployment]false'
  return
}

$centralNow = [TimeZoneInfo]::ConvertTimeBySystemTimeZoneId([DateTimeOffset]::UtcNow, 'Central Standard Time')
$hour = $centralNow.Hour
Write-Host "Central time now: $($centralNow.ToString('yyyy-MM-dd HH:mm:ss zzz'))"

$windowStartHour = [int]'$(deliveryWindowStartHourCst)'
$windowEndHour = [int]'$(deliveryWindowEndHourCst)'
Write-Host "Configured CST deployment window: start=$windowStartHour end=$windowEndHour"

if ($windowStartHour -eq $windowEndHour)
{
  $inWindow = $true
}
elseif ($windowStartHour -lt $windowEndHour)
{
  $inWindow = $hour -ge $windowStartHour -and $hour -lt $windowEndHour
}
else
{
  $inWindow = $hour -ge $windowStartHour -or $hour -lt $windowEndHour
}

if (-not $inWindow)
{
Write-Host "Outside configured CST auto-deploy window (start=$windowStartHour end=$windowEndHour hour=$hour). Delivery paused."
Write-Host "To deploy now, trigger this pipeline manually. Otherwise resume through the next repository-defined automated pickup when the window opens."
  Write-Host '##vso[task.setvariable variable=skipDeployment]true'
  return
}
Write-Host 'Within configured CST deployment window. Continuing.'
Write-Host '##vso[task.setvariable variable=skipDeployment]false'
```

Every step that follows the window check must carry this condition:

```yaml
condition: and(succeeded(), ne(variables['skipDeployment'], 'true'))
```

| Scenario | Pipeline result | Deployment |
|---|---|---|
| Automated run, inside configured CST window | ✅ Green | Deploys |
| Automated run, outside configured window | ✅ Green | Pauses cleanly — waits for manual rerun or next scheduled delivery-pickup run |
| Manual run, any time | ✅ Green | Deploys |
| `throw` / `exit 1` used (wrong) | ❌ Red (Failed) | Blocked — do not implement this way |
| `SucceededWithIssues` used (wrong) | 🟡 Yellow | Blocked — do not implement this way |

---

#### 2.3.2 Scheduled delivery pickup requirement
<!-- STD-MARKER: azure-devops-pipeline.2.3.2 -->

Every deliverable production pipeline must have an automated pickup mechanism aligned to the configured CST delivery window.

- The pickup must run on a repository-defined schedule or equivalent automation cadence.
- The pickup must check whether a delivery-worthy promoted output is available for release.
- If no promoted output is available, the pickup run must complete green and exit cleanly.
- If a promoted output is available and the current time is inside the configured delivery window, the pickup must continue into the normal production delivery sequence.
- If a promoted output is available and the current time is outside the configured delivery window, the pickup run must complete green and exit cleanly. It must not wait or hold the agent open.
- Manual production runs must bypass the time-window restriction and proceed immediately.

This pickup mechanism is how repositories automatically deliver already-promoted changes when the allowed window arrives. It does not replace the `feature/* → dev` validation path and it does not add build or test validation to production delivery.

---

#### 2.3.3 Azure DevOps scheduled trigger configuration for WebApp/WebAPI repositories
<!-- STD-MARKER: azure-devops-pipeline.2.3.3 -->

For API and Web App repositories, the scheduled delivery-pickup mechanism required by [Section 2.3.2](#232-scheduled-delivery-pickup-requirement) must be implemented as an Azure DevOps scheduled trigger on the production pipeline (for example, `.azure-pipelines/Main/schedule.yml` or an equivalent scheduled-entrypoint file), configured directly in Azure DevOps rather than solely in pipeline YAML `schedules:` blocks, so the trigger can be independently enabled or disabled without a code change.

The scheduled trigger must be configured to run at or near the start of the configured CST delivery window (see [Section 2.3.1](#231-window-check-implementation--required-pattern)) so a paused delivery is picked up promptly once the window opens.

During initial pipeline rollout for a repository, the scheduled trigger must be created but left **disabled** until the repository reaches go-live. This allows the schedule to be registered and reviewed ahead of time without triggering unintended production runs. Enabling the schedule is a deliberate go-live step, not part of initial pipeline registration.

---

## 3. Pull Request Standards
<!-- STD-MARKER: azure-devops-pipeline.3 -->

This section defines the required PR creation process, auto-complete policy, carry-forward behavior, and the automated PR chain for all repositories. Every rule in this section applies to all application types unless explicitly tagged otherwise.

### 3.1 PR Creation Requirements
<!-- STD-MARKER: azure-devops-pipeline.3.1 -->

Every pull request must be created with the following:

- **A descriptive title.** The title must clearly identify what changed. It must not be a branch name, a ticket number alone, or a generic label like "updates" or "changes".
- **A structured description.** The description must contain:
  - A link to the current PR (self-referencing, so carry-forward PRs always trace back to their origin)
  - A list of the changes included in this PR
  - A plain-language summary of what the changes do and why
- **Auto-complete enabled at creation time.** Auto-complete must be set when the PR is created — not added after the fact. The PR merges automatically once all required branch policies are satisfied. Auto-complete does not bypass policies; it waits for them.

### 3.2 Required Approver
<!-- STD-MARKER: azure-devops-pipeline.3.2 -->

PR approval requirements are environment-path specific and are configured as ADO branch policies on the target branch — not hardcoded in pipeline YAML.

- PRs targeting `dev` must retain the required reviewer gate because `feature/* → dev` is the authoritative validation and merge-control path.
- PRs targeting future pre-production environment branches such as `qa` must also use the required reviewer gate when those branches become active.
- PRs targeting `main` in the current `feature/* → dev → main` model must not require a human approval gate as part of the delivery standard. The `dev → main` PR exists to promote the already-validated change set into the production delivery path.
- The current required reviewer for approval-gated branches is **Troy Crowe**.
- The required reviewer can be changed at any time by updating the branch policy in ADO. No pipeline or standards file change is required when the reviewer changes.
- Auto-complete must remain enabled on created PRs. For approval-gated branches it waits for reviewer approval and required branch policies. For `main`, it waits only for the non-approval policies that remain configured for that branch.

### 3.3 Automated PR Chain (Deprecated — Not Currently Required)
<!-- STD-MARKER: azure-devops-pipeline.3.3 -->

> **Deprecated:** The AutoPR carry-forward automation described in this section is discontinued. `dev → main` PRs are currently created manually. The reusable template (`Docs/Templates/PRs/AutoPRChain.yml`) and all repository-local copies (`.azure-pipelines/templates/auto-pr-chain.yml`) and dangling workflow files (`.azure-pipelines/workflows/dev-main-promotion.yml`) have been removed. No repository pipeline currently references this automation (confirmed against registered Azure DevOps pipeline definitions). This section is retained for reference only in case the automation is reinstated in the future; it is not an active requirement.

<!--
When a PR merges to any environment branch, a pipeline must automatically create the next PR in the delivery chain with the title and description carried forward from the merged PR. This automation applies to all repositories.

For the current `feature/* → dev → main` model, the `dev → main` PR is the required promotion artifact for the change set moving toward production. The automation is responsible for creating that artifact promptly after the `feature/* → dev` merge completes.

**Current chain (no QA environment):**

```
feature/* → dev → main
```

**Future chain (when QA is introduced):**

```
feature/* → dev → qa → main
```

**Behavior on merge:**

- When a **feature→dev PR merges** — a pipeline stage triggers on the `dev` branch CI, looks up the PR that caused the merge, and creates a new `dev→main` PR using the same title and description. Auto-complete is enabled. No human approval gate is required on `main` in the current model. Where the repository type produces a promoted output, the promotion PR must reference or be traceable to that output.
- When a **dev→QA PR merges** *(future)* — the same pattern fires and creates a `QA→main` PR with the carried-forward title and description.
- The carry-forward pipeline must not create a duplicate PR if one already exists between the same branches. It must check for an existing open PR first and skip creation if one is found.
- If an open `dev→main` PR already exists, the automation must log that PR as the active promotion artifact and must not create a competing PR.
- Auto-complete on the created `dev→main` PR must wait only for the configured non-approval branch policies on `main`. The automation must not treat the absence of a human approval as a creation failure because approval is not required on this path.
- If PR creation fails, the pipeline must fail clearly and identify that the promotion PR was not created. Silent success is not permitted when the required `dev→main` promotion artifact is missing.

**What is carried forward:**

| Field | Behavior |
|---|---|
| Title | Carried forward verbatim from the merged PR |
| Description | Carried forward verbatim from the merged PR, including a generated change list and origin PR link |
| Work items | Carried forward — all work items linked to the merged PR must be linked to the new PR |
| Auto-complete | Always enabled on the created PR |
| Required reviewer | Enforced by the target branch policy for approval-gated branches; not required for the current `dev → main` promotion path |
| Promoted output traceability | The created PR must remain traceable to the promoted output created during the `feature/* → dev` validation run when the repository type produces one |

**Implementation:**

The carry-forward PR automation is a reusable template maintained in `GlobalStandards/Docs/Templates/PRs/AutoPRChain.yml`. Each repository calls this template from a pipeline that triggers on CI push to `dev` (and later `qa`). The template must:

1. Look up the most recently merged PR to the current branch using the ADO REST API (`GET /pullRequests?status=completed&$top=1`)
2. Extract the title and description from that PR
3. Check whether an open PR already exists from the current branch to the next branch (`GET /pullRequests?sourceRefName=...&targetRefName=...&status=active`)
4. If no open PR exists — create the PR with the extracted title, description, and auto-complete enabled
5. If an open PR already exists — skip creation and log the existing PR ID
6. Retrieve work items linked to the merged PR (`GET /pullRequests/{id}/workitems`) and associate them with the newly created PR so traceability is maintained through the full delivery chain
7. Treat successful PR creation as the existence of an active `dev→main` PR with auto-complete enabled and waiting on the configured `main` branch-policy satisfaction, not as immediate completion of the promotion
8. Preserve or emit the metadata required to associate the promotion PR with the promoted output created during the originating validation run when the repository type produces one

> **Backlog:** Step 6 (work item carry-forward) is tracked as **STD-019** in `Working/ProjectBacklog.md` and is not yet implemented in the template.

The template must use `System.AccessToken` for authentication.

If repository automation is temporarily unable to create the `dev→main` PR, the repository owner or release owner must create the PR manually using the same carried-forward title and description pattern. This is a recovery action for automation failure and does not remove the requirement for the promotion PR to exist.
-->

### 3.4 PR Chain Configuration Per Repository (Deprecated — Not Currently Required)
<!-- STD-MARKER: azure-devops-pipeline.3.4 -->

> **Deprecated:** See the note in Section 3.3. This section is retained for reference only in case AutoPR automation is reinstated in the future.

<!--
Each repository must declare its branch chain in its pipeline variable file so the auto-PR template knows the next target branch without hardcoding it.

```yaml
# .azure-pipelines/variables/common.yml
variables:
  # Branch chain — defines the next branch when creating carry-forward PRs
  # Update this when new environment branches are added
  devNextBranch: 'main'          # next branch after dev (change to 'qa' when QA is introduced)
  # qaNextBranch: 'main'         # uncomment when QA branch exists
```

When QA is introduced, the only required change per repository is updating `devNextBranch` from `main` to `qa` and uncommenting `qaNextBranch`.
-->

### 3.5 Canonical template layout
<!-- STD-MARKER: azure-devops-pipeline.3.5 -->

The canonical template store in GlobalStandards must keep reusable assets grouped so a developer or AI model can collect all required files for a repository type from one place.

- Pipeline templates belong under `Docs/Templates/Pipelines/{RepositoryType}/{Environment}/`.
- PR automation is currently discontinued (see Section 3.3); no `Docs/Templates/PRs/` folder exists.
- Avoid separate canonical
- If a script belongs to a reusable template, place it with that template rather than in a separate top-level scripts area.
- The reusable local validation entry-point script for each repository type belongs under `Docs/Templates/Pipelines/{RepositoryType}/Scripts/Validate.ps1`, with a companion `Readme.md` describing rollout. See Section 3.5.2.
- Repository-local live assets in an individual repository (for example `.azure-pipelines/workflows/*.yml` or `Scripts/validate.ps1`) are not template clutter and may remain in their operational locations.
- Delete unused working files, obsolete drafts, and stale template artifacts when they are no longer needed.

### 3.5.1 Numbered pipeline template file prefixes
<!-- STD-MARKER: azure-devops-pipeline.3.5.1 -->

Pipeline template files within an environment folder (for example `Docs/Templates/Pipelines/{RepositoryType}/{Environment}/`) that represent an ordered sequence of stages must use a two-digit, zero-padded numeric prefix followed directly by a PascalCase stage name — for example `01Trigger.yml`, `02Build.yml`, `03Deploy.yml`.

- The numeric prefix indicates execution order only. It does not replace the `stage`/`dependsOn` ordering declared inside the YAML — both must agree.
- Prefixes start at `01` per environment folder and increment by `01` for each subsequent stage in that folder. Do not skip numbers or reuse a number within the same folder.
- Use two digits (`01`–`09`, then `10`+) so filenames sort correctly regardless of file system or tooling.
- Files that are not part of an ordered stage sequence (for example shared includes, variable files, or `README.md` placeholders) must not carry a numeric prefix.
- If a stage is removed, renumber the remaining files so the sequence stays contiguous — do not leave gaps.

### 3.5.2 Local validation script templates
<!-- STD-MARKER: azure-devops-pipeline.3.5.2 -->

Each repository type must have a reusable local validation entry-point script template maintained under `Docs/Templates/Pipelines/{RepositoryType}/Scripts/Validate.ps1`, with a companion `Readme.md` describing its purpose and rollout procedure. This gives each repository type a consistent local command that mirrors the Dev pipeline's build/test gates before a pull request is created or updated.

- `Docs/Templates/Pipelines/Library/Scripts/Validate.ps1` — restores, builds, tests (when a test project exists), and packs (`-Pack`) a class-library repository when the `-Pack` switch is supplied.
- `Docs/Templates/Pipelines/WebApiWebApp/Scripts/Validate.ps1` — restores, builds, tests (when a test project exists), and publishes (`-Publish`) a deployable API/Web App repository when the `-Publish` switch is supplied.
- `Docs/Templates/Pipelines/Database/Scripts/Validate.ps1` — validates the `.sqlproj` file and prepares a release-bundle folder (`-Bundle`) for a database repository when the `-Bundle` switch is supplied.

When a repository is initialized or rolled over to the current pipeline standard, copy the matching `Validate.ps1` template into the repository's local tooling folder (for example `Scripts/`, `scripts/`, or `Tools/`, matching the repository's existing convention) and reference it from the repository README as the local validation entry point. The copied script becomes a repository-local live asset per Section 3.5 and is not template clutter once copied.

---



> **This guide is the authoritative source for pipeline implementation. It is written to be executed by an AI model with zero ambiguity. The following rules govern all execution.**

1. **Follow every step exactly as written.** Do not infer, improvise, or apply patterns from outside this document. If this guide specifies a file name, variable name, task name, or script pattern — use it verbatim.
2. **Do not skip mandatory steps.** Steps marked `[All]` apply to every repo. Steps marked `[Deployable]` apply to APIs and Web Apps. Steps marked `[Library]` apply to class libraries. Steps tagged `[API only]` or `[Web App only]` apply only to that sub-type. All other steps are skipped silently.
3. **Replace tokens precisely.** Every `<placeholder>` in this guide is a required substitution. Do not leave any placeholder literal in a committed file. The token replacement table is in each step.
4. **Verify after each step.** Each step ends with a verification action. Complete it before proceeding to the next step. If verification fails, fix the issue in the current step — do not continue.
5. **Produce files, not descriptions.** Write every file to disk using the exact content shown. Do not summarize or paraphrase — create the file.
6. **One branch per repo, one PR per branch.** All pipeline work for a repo goes on a single feature branch (e.g., `feature/pipeline-setup`). One PR to `dev` when complete.
7. **Commit message format.** Use: `feat: add standard Azure DevOps pipeline setup` for the initial pipeline commit.
8. **Do not modify existing application code** unless Step 8 (health check) explicitly requires it and the endpoint does not already exist.
9. **Do not add packages, libraries, or dependencies** beyond what Step 8 specifies.
10. **If a step says "must" — it is a hard requirement.** If a step says "do not" — it is prohibited. There are no exceptions.

---

## Step 0 — Determine Your Application Type

Before starting, identify which category your repository falls into. Every subsequent step is tagged accordingly.

| Tag | Application Type | Examples | Has Deployment Pipelines? | Health Check Required? |
|---|---|---|---|---|
| **[Deployable]** | ASP.NET Core Web API | CaptiveMessagingAPI, DocumentManagerAPI, CaptiveExpensesAPI | Yes | Yes — `/health` endpoint (HTTP 200, body: `Healthy`) |
| **[Deployable]** | ASP.NET Core Web App (with React or other JS frontend) | CaptiveExpensesWeb, CaptiveBillingWeb, CaptiveInventoryWeb | Yes | Yes — app root `/` or known stable page (2xx required) |
| **[Database]** | SQL Database Project (DACPAC) | CaptiveExpensesDB | Yes | No — post-deploy validation runs SQL smoke queries instead |
| **[Library]** | Class Library / NuGet Package | HttpClientManager, DataImportExportManager | No | Not applicable |

> **[Deployable]** steps apply to both APIs and Web Apps unless further tagged **[API only]** or **[Web App only]**.
> **Both APIs and Web Apps require a health or readiness check URL.** The implementation differs — see Step 8.
> **[Database]** repositories deploy a DACPAC to Azure SQL. They do not use App Service slots, staging environments, or health check URLs. Skip Steps 1, 2, 3, 8, 11, and 12 — these are not applicable. Use the database-specific instructions in Steps 5, 6, 7, 9, 10, and 13.
**[Library]** repositories use a single pipeline (build + test + merge) and skip all deployment infrastructure steps.

---

## Prerequisites

### All application types

- [ ] Azure DevOps organization and project created for the repository.
- [ ] Git repository exists in the Azure DevOps project.
- [ ] Repository has been cloned locally and has an initial commit on `main` or `master`.

### Deployable applications only (API and Web App)

- [ ] Azure subscription with an App Service already provisioned (production slot).
- [ ] App Service has a `staging` deployment slot created (see Step 1).
- [ ] Service connection exists or will be created in the Azure DevOps project (see Step 2).

> **Mandatory:** Every deployable API or Web App repository must provision its App Service on a plan/tier that supports at least one deployment slot. The staging-slot swap pattern is a required part of this domain's pipeline standard, not an optional or tier-conditional feature — do not provision a slot-incapable tier (e.g., Free/Shared) for a repository governed by this standard.

---

## Step 1 — Create the Staging Deployment Slot `[Deployable]`

The staging slot is the intermediary deployment target. Code deploys to `staging` first, is verified, then swapped to production.

**Azure Portal:**

1. Open the Azure Portal -> **App Services** -> select your App Service.
2. Under **Deployment** -> **Deployment slots** -> click **Add Slot**.
3. Name: `staging`.
4. Clone settings from: **production** (recommended -- copies app settings and connection strings).
5. Click **Add**.

**Azure CLI equivalent:**

```powershell
az webapp deployment slot create ``
  --name <appServiceName> ``
  --resource-group <resourceGroupName> ``
  --slot staging ``
  --configuration-source <appServiceName>
```

> Record the staging slot hostname: `https://<appServiceName>-staging.azurewebsites.net`

---

## Step 2 — Create the Azure DevOps Service Connection `[Deployable]`

The service connection authorizes pipeline tasks (`AzureWebApp@1`, `AzureAppServiceManage@0`) to interact with your Azure subscription.

**Azure DevOps Portal:**

1. Open your Azure DevOps **Project Settings** -> **Service connections**.
2. Click **New service connection** -> select **Azure Resource Manager**.
3. Authentication method: **Workload Identity Federation**.
4. Scope level: **Subscription**.
5. Select subscription **`SingleSourceManagement`** and set the target resource group for the repository when the connection is scoped to a resource group.
6. For the current rollout, use the shared User Managed Identity **`CI-DevOps-UMI`**.
7. Service connection name: **`CI-DevOps-UMI-Connection`**.
8. Check **Grant access permission to all pipelines**.
9. Click **Save**.

If an existing UMI-backed service connection cannot be edited to match this configuration, delete it and recreate it with the approved shared identity and name. Do not leave superseded UMI-backed service connections in place.

### Current rollout baseline

Use this sequence for the current shared rollout configuration:

1. Authenticate to Azure interactively with the device login flow.
   - Run `az login --use-device-code`.
   - Complete the browser/device-code prompt with the intended developer identity.
2. Set the active subscription to `SingleSourceManagement`.
   - Run `az account set --subscription SingleSourceManagement`.
3. Verify the shared User Managed Identity `CI-DevOps-UMI` exists in the target tenant and has the permissions required for the target resource group or subscription scope.
4. In Azure DevOps, open **Project Settings** -> **Service connections** and create or recreate the Azure Resource Manager service connection using **Workload Identity Federation**.
5. Bind the service connection to subscription `SingleSourceManagement`, use the shared UMI `CI-DevOps-UMI`, and name the connection `CI-DevOps-UMI-Connection`.
6. Grant access to all pipelines only after confirming the connection targets the intended subscription and identity.
7. Update each repository pipeline variable file to reference `CI-DevOps-UMI-Connection` exactly.

If a prior UMI-backed connection used a different name or identity, remove it after the replacement connection is confirmed working so no orphaned service-connection configuration remains.

> Record the exact service connection name -- it must match the `azureServiceConnection` variable in `common.yml` exactly.

---

## Step 3 — Create the Azure DevOps Environment `[Deployable]`

The pipeline `environment` resource provides deployment tracking and, when needed, approval gates.

**Azure DevOps Portal:**

1. Open your Azure DevOps project -> **Pipelines** -> **Environments**.
2. Click **New environment**.
3. Name: use the pattern `<reponame-lowercase>-prod`.
   - Example: `captivemessagingapi-prod`
4. Description: optional.
5. Resource: **None** (no Kubernetes or VM resource needed for App Service deployments).
6. Click **Create**.

> **First-run authorization:** The first time a pipeline references a new environment, Azure DevOps will pause and ask you to authorize the pipeline to use it. This is a one-time action per pipeline per environment. Approve it in the pipeline run UI.

---

## Step 4 — Create the Branch Structure `[All]`

All feature work branches from `dev`. `dev` merges to `master`/`main` via PR only.

```powershell
git checkout main          # or master -- use whichever is the root branch
git pull origin main
git checkout -b dev
git push origin dev
```

> If `dev` already exists remotely, skip creation and just pull.

---

## Step 5 — Create the Pipeline Variable Files `[Deployable]` `[Database]`

Variable files centralize configuration and keep pipelines readable. Create the folder structure:

**Deployable (API and Web App)**
```
.azure-pipelines/
  variables/
    common.yml
    prod.yml
  workflows/
    <reponame>-dev.yml          # PR trigger entrypoint
    <reponame>-validation.yml   # Build + test template (no trigger)
    <reponame>-master-deploy.yml
```

**Database (DACPAC)**
```
.azure-pipelines/
  workflows/
    <reponame>-dev.yml           # PR trigger entrypoint — feature → dev gate
    <reponame>-master.yml        # Master entrypoint — extends the deploy template
    <reponame>-master-deploy.yml # Deploy template — build, backup, DACPAC, recovery
```

**Class Library**
```
.azure-pipelines/
  variables/
    common.yml
  workflows/
    <reponame>-dev.yml   # PR trigger entrypoint
    <reponame>-ci.yml    # Build + test template (no trigger)
```

### Token Reference

Every `<placeholder>` used in this guide maps to a real value from the repository and Azure environment. Resolve all tokens **before writing any file**. Do not write a file that contains a literal `<placeholder>` string.

| Token | Required in | How to resolve |
|---|---|---|
| `<reponame>` | All file names and YAML | Lowercase repo name, e.g., `captivemessagingapi`, `captiveexpensesdb` |
| `<SolutionName>.sln` | `common.yml` | The `.sln` file at the repository root |
| `<ProjectFolder>/<ProjectName>.csproj` | `common.yml` (`apiProjectPath`) | Path from repo root to the deployable `.csproj` |
| `<ServiceConnectionName>` | `common.yml` | Exact service connection name from Step 2 |
| `<reponame-lowercase>-prod` | `prod.yml`, `common.yml` | Lowercase repo name + `-prod`, must match Step 3 exactly |
| `<AzureAppServiceName>` | `prod.yml` | Azure App Service resource name |
| `<ResourceGroupName>` | `prod.yml` | Azure resource group containing the App Service |
| `<SqlProjectFolder>/<SqlProjectName>.sqlproj` | Database YAML | Path from repo root to the `.sqlproj` file, e.g., `CaptiveExpensesDB/CaptiveExpensesDB.sqlproj` |
| `<SqlProjectName>` | Database YAML | The SQL project name, e.g., `CaptiveExpensesDB` |
| `<SqlServerName>` | Database YAML | Azure SQL Server resource name without domain, e.g., `Captive-sql-server` |
| `<DatabaseName>` | Database YAML | Target Azure SQL database name, e.g., `CaptiveExpenses` |
| `<AzureServiceConnectionName>` | Database YAML | Exact service connection name authorized for Azure SQL and Azure CLI operations |

> **Verification:** After resolving tokens, confirm no `<` or `>` characters remain in any file you write. If any do, the file is incomplete — fix it before proceeding.

### 5a -- common.yml
<!-- STD-MARKER: azure-devops-pipeline.5a -->

Contains build settings and the service connection name.

> **Template:** [`Docs/Templates/Pipelines/WebApiWebApp/Variables/Variables.yml`](../Templates/Pipelines/WebApiWebApp/Variables/Variables.yml)
>
> Copy the file and replace:
> - `<solutionFileName>` — the `.sln`/`.slnx` file at repo root (e.g., `CaptiveMessaging.sln`)
> - `<appProjectRelativePath>` — path to the deployable project file
> - `<azureServiceConnectionName>` — exact name from Step 2

### 5b -- prod.yml
<!-- STD-MARKER: azure-devops-pipeline.5b -->

Contains all production and staging environment values.

> **Template:** [`Docs/Templates/Pipelines/WebApiWebApp/Variables/Variables.yml`](../Templates/Pipelines/WebApiWebApp/Variables/Variables.yml)
>
> Replace all remaining tokens for the production/main environment:
> - `<mainEnvironmentName>` — must match the environment name from Step 3
> - `<appServiceName>` — the App Service name in Azure
> - `<resourceGroupName>` — the resource group containing the App Service

> **[Web App]:** The staging slot URL uses the `-staging` suffix by default. If yours differs, update `stagingHealthCheckUrl` accordingly. If the app does not have a keyword-identifiable response, leave `healthCheckExpectedKeyword` empty. See Step 8 for details.

### 5c — Database variables `[Database]`
<!-- STD-MARKER: azure-devops-pipeline.5c -->

Database pipelines declare variables inline

The following variables must be declared in both `<reponame>-dev.yml` and `<reponame>-master-deploy.yml`:

```yaml
variables:
  buildPlatform: 'Any CPU'
  buildConfiguration: 'Release'
  sqlProjectName: '<SqlProjectName>'
  sqlProjectPath: '<SqlProjectFolder>/<SqlProjectName>.sqlproj'
  sqlServerName: '<SqlServerName>'
  databaseName: '<DatabaseName>'
  enableAutoCorruptionRecovery: 'true'
```

Replace:
- `<SqlProjectName>` — the SQL project name, e.g., `CaptiveExpensesDB`
- `<SqlProjectFolder>/<SqlProjectName>.sqlproj` — path from repo root to the project file, e.g., `CaptiveExpensesDB/CaptiveExpensesDB.sqlproj`
- `<SqlServerName>` — Azure SQL Server name without domain, e.g., `Captive-sql-server`
- `<DatabaseName>` — target database name, e.g., `CaptiveExpenses`

The `azureSubscription` (service connection name) is declared inline on each `AzureCLI@2` and `SqlAzureDacpacDeployment@1` task rather than as a shared variable.

> **Verification:** Confirm `enableAutoCorruptionRecovery` is set to `'true'` in every database deploy pipeline. Setting it to `'false'` disables the pre-deploy backup and auto-recovery flow and must never be committed to a production branch without explicit approval.

---

## Step 6 — Create the Validation Pipeline YAML `[All]`

This pipeline handles the `feature/* -> dev` PR gate. It **only** builds and tests -- it never publishes or deploys.

All app types use a **two-file pattern**: a trigger entrypoint (`<reponame>-dev.yml`) that owns the PR trigger and a template file that owns the steps. This keeps the trigger declaration separate from the implementation and makes the template reusable.

### Build-once artifact hand-off `[Deployable]` `[Database]`
<!-- STD-MARKER: azure-devops-pipeline.6.build-once -->

Both human developers and AI models must verify that a compiled project is never rebuilt by a downstream stage in the same pipeline run.

- The Build stage compiles the project exactly once and publishes two outputs as pipeline artifacts: the app deploy artifact (the packaged output that will be deployed or released) and the build output used for testing (the compiled test binaries and their dependencies).
- The Test stage must not run `dotnet build` or `dotnet restore` again. It downloads the published build output and runs tests with `dotnet test --no-build --no-restore`.
- The Publish/Deploy stage must not recompile. It downloads the app deploy artifact produced by the Build stage and deploys it as-is.
- No stage after Build may reference source code directly for compilation purposes; every downstream stage consumes a published artifact from the Build stage.

This pattern guarantees that the exact binary validated by the test run is the same binary that gets deployed, eliminating drift between what was tested and what is released.

### Deployable applications (API and Web App)

Create `.azure-pipelines/workflows/<reponame>-dev.yml` (trigger entrypoint) using the template:

> **Template:** [`Docs/Templates/Pipelines/WebApiWebApp/Dev/Dev.yml`](../Templates/Pipelines/WebApiWebApp/Dev/Dev.yml)
>
> Copy the file as-is. It already references `../Shared/ChangeDetection.yml`, `../Shared/StandardsValidation.yml`, `Build.yml`, `Test.yml`, and `Publish.yml` by relative path — no filename substitution is required.

Create `.azure-pipelines/workflows/<reponame>-build.yml` and `<reponame>-test.yml` (build and test templates) using the templates:

> **Templates:** [`Docs/Templates/Pipelines/WebApiWebApp/Dev/Build.yml`](../Templates/Pipelines/WebApiWebApp/Dev/Build.yml), [`Docs/Templates/Pipelines/WebApiWebApp/Dev/Test.yml`](../Templates/Pipelines/WebApiWebApp/Dev/Test.yml)
>
> Copy the files as-is. Adjust `dotNetSdkVersion` in `Variables/Variables.yml` to match the target .NET SDK. Use `includePreviewVersions: true` only when the target SDK is in preview. If the repository has no test project yet, omit the `dotnet test` step.

### Class Library `[Library]`

Class library repositories publish a NuGet package to an Azure Artifacts feed. There is **no App Service, slot, or SQL deployment target** — the terminal action of the `main` pipeline is `NuGetAuthenticate@1` + `dotnet nuget push`. The `dev` pipeline builds, packs, tests, and (on success on the `dev` branch) opens a carry-forward PR to `main`; the `main` pipeline builds, packs, and publishes.

Create `.azure-pipelines/Variables/variables.yml` using the template:

> **Template:** [`Docs/Templates/Pipelines/Library/Variables/variables.yml`](../Templates/Pipelines/Library/Variables/variables.yml)
>
> Copy the file and replace:
> - `<projectRelativePath>` — path from repo root to the library `.csproj`
> - `<testProjectRelativePath>` — path from repo root to the test project `.csproj`
> - `<artifactsFeedName>` — the Azure Artifacts feed name
> - `<artifactsFeedUrl>` — the full NuGet v3 feed URL for the Azure Artifacts feed

Create `.azure-pipelines/Dev/dev.yml` (PR-gate entrypoint) using the templates:

> **Templates:** [`Docs/Templates/Pipelines/Library/Dev/dev.yml`](../Templates/Pipelines/Library/Dev/dev.yml), [`Docs/Templates/Pipelines/Library/Dev/change-detection.yml`](../Templates/Pipelines/Library/Dev/change-detection.yml), [`Docs/Templates/Pipelines/Library/Dev/build.yml`](../Templates/Pipelines/Library/Dev/build.yml), [`Docs/Templates/Pipelines/Library/Dev/test.yml`](../Templates/Pipelines/Library/Dev/test.yml), [`Docs/Templates/Pipelines/Library/Dev/carry-forward.yml`](../Templates/Pipelines/Library/Dev/carry-forward.yml)
>
> Copy the files as-is. Adjust `<dotNetSdkVersion>` and `<includePreviewVersions>` in `build.yml` and `test.yml` to match the target .NET SDK. If the repository has no test project yet, omit the `dotnet test` step — add it when tests are introduced.

Create `.azure-pipelines/Main/main.yml` (production publish entrypoint) using the templates:

> **Templates:** [`Docs/Templates/Pipelines/Library/Main/main.yml`](../Templates/Pipelines/Library/Main/main.yml), [`Docs/Templates/Pipelines/Library/Main/change-detection.yml`](../Templates/Pipelines/Library/Main/change-detection.yml), [`Docs/Templates/Pipelines/Library/Main/publish.yml`](../Templates/Pipelines/Library/Main/publish.yml)
>
> Copy the files as-is. Adjust `<dotNetSdkVersion>` and `<includePreviewVersions>` in `publish.yml` to match the target .NET SDK. Confirm the pipeline's build service has **Feed Contributor** permission on the target Azure Artifacts feed — `NuGetAuthenticate@1` does not use a separate service connection.

> Adjust `version: 10.0.x` to match the target .NET SDK. Use `includePreviewVersions: true` only when the target SDK is in preview.

> **Verification:** Run `dotnet build <SolutionName>.sln --configuration Release` from the repo root. It must succeed with zero errors before committing these files.

### Database `[Database]`

Database repositories use a **two-file pattern**: a trigger entrypoint (`<reponame>-dev.yml`) that owns the PR trigger and a template file (`<reponame>-master-deploy.yml`) that owns the steps. The PR validation pipeline builds and validates only — it never deploys.

Create `.azure-pipelines/workflows/<reponame>-dev.yml` (PR trigger entrypoint) using the template:

> **Template:** [`Docs/Templates/Pipelines/Database/workflows/Dev.yml`](../Templates/Pipelines/Database/workflows/Dev.yml)
>
> Copy the file and replace all tokens: `<sqlProjectName>`, `<sqlProjectRelativePath>`. Replace the `<sqlProjectName>` reference in the stored procedures path inside the `SQL Source Validation` script (see also `Docs/Templates/Pipelines/Database/workflows/MainDeploy.yml` for the production deploy chain and `Docs/Templates/Pipelines/Database/README.md` for known deferred/unfinished items in this template).

> **Verification:** The PR validation pipeline must build the `.sqlproj` using `VSBuild@1` and produce a `.dacpac` artifact. It must not contain any deploy, publish, or Azure connection tasks.

---

## Step 7 — Create the Release Pipeline YAML `[Deployable]`

This pipeline handles both the `dev -> master` PR build gate **and** the CI post-merge deploy. It enforces the CST auto-deploy window and includes staging deploy, health verification, slot swap, production verify, and rollback.

> **[Library]** -- skip this step entirely. Class libraries have no deployment infrastructure. The `dev.yml` + `ci.yml` pattern created in Step 6 is the complete pipeline setup for a class library.

Create `.azure-pipelines/Main/Main.yml` using the templates:

> **Templates:** [`Docs/Templates/Pipelines/WebApiWebApp/Main/Main.yml`](../Templates/Pipelines/WebApiWebApp/Main/Main.yml), [`Docs/Templates/Pipelines/WebApiWebApp/Main/Swap.yml`](../Templates/Pipelines/WebApiWebApp/Main/Swap.yml), [`Docs/Templates/Pipelines/WebApiWebApp/Main/Validate.yml`](../Templates/Pipelines/WebApiWebApp/Main/Validate.yml), [`Docs/Templates/Pipelines/WebApiWebApp/Main/Rollback.yml`](../Templates/Pipelines/WebApiWebApp/Main/Rollback.yml)
>
> Copy the files as-is. No token replacements are needed in these files — all values are resolved at runtime via the shared variable template (`Variables/Variables.yml`).

> If the root branch is `main` instead of `master`, replace every `refs/heads/master` with `refs/heads/main`.

> **Verification:** Confirm the file contains exactly three `azureSubscription: $(azureServiceConnection)` references (staging deploy, slot swap, rollback) and zero literal service connection name strings. Confirm `condition: and(succeeded(), ne(variables['skipDeployment'], 'true'))` is present on every deploy step that follows the `Evaluate CST deployment window` task.

### Database deploy pipeline `[Database]`

Database repositories require two pipeline files for the master branch: an entrypoint that owns the trigger and a deploy template that owns all steps. The deploy template must never be triggered directly — it is only invoked via `extends`.

Create `.azure-pipelines/workflows/<reponame>-master.yml` (master entrypoint) using the template:

> **Template:** [`Docs/Templates/Pipelines/database-master.yml`](../Templates/Pipelines/database-master.yml)
>
> Copy the file and replace all tokens: `<RepoName>` (comment header), `<SqlProjectName>`, `<reponame>-master-deploy.yml` in the `extends` block.

Create `.azure-pipelines/workflows/<reponame>-master-deploy.yml` (deploy template) using the template:

> **Template:** [`Docs/Templates/Pipelines/database-master-deploy.yml`](../Templates/Pipelines/database-master-deploy.yml)
>
> Copy the file and replace all tokens: `<RepoName>` (comment header), `<SqlProjectName>`, `<SqlProjectFolder>/<SqlProjectName>.sqlproj`, `<SqlServerName>`, `<DatabaseName>`, `<AzureServiceConnectionName>` (appears on every `SqlAzureDacpacDeployment@1` and `AzureCLI@2` task).

The remainder of this step — the hardening requirements table, the CST window implementation guidance, and the correct script and condition patterns — apply unchanged regardless of whether you are reading the template or writing from scratch.

                  Write-Host "Staging health check attempt $attempt of $maxAttempts -- $verifyUrl"
                  try
                  {
                    $response = Invoke-WebRequest -Uri $verifyUrl -Method Get -TimeoutSec $timeoutSeconds -SkipHttpErrorCheck
                    if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 300)
                    {
                      if (-not [string]::IsNullOrWhiteSpace($expectedKw) -and $response.Content -notmatch [regex]::Escape($expectedKw))
                      {
                        throw "Health response did not contain expected keyword '$expectedKw'."
                      }
                      Write-Host "Staging health check passed (HTTP $($response.StatusCode))."
                      return
                    }
                    Write-Host "Staging health check returned HTTP $($response.StatusCode)."
                  }
                  catch
                  {
                    Write-Host "Staging health check error: $($_.Exception.Message)"
                  }
                  if ($attempt -lt $maxAttempts) { Start-Sleep -Seconds $delaySeconds }
                }
                throw "Staging health verification failed after $maxAttempts attempts."

          - task: AzureAppServiceManage@0
            displayName: Swap staging slot to production
            inputs:
              azureSubscription: $(azureServiceConnection)
              Action: Swap Slots
              WebAppName: $(appServiceName)
              ResourceGroupName: $(resourceGroupName)
              SourceSlot: $(stagingSlotName)
              SwapWithProduction: true

- stage: Verify_Prod
  displayName: Verify Production
  dependsOn: Deploy_Prod
  condition: and(succeeded('Deploy_Prod'), ne(variables['Build.Reason'], 'PullRequest'), eq(variables['Build.SourceBranch'], 'refs/heads/master'))
  variables:
  - template: ../variables/prod.yml
  jobs:
  - job: VerifyProduction
    displayName: Verify production health
    pool:
      vmImage: windows-latest
    steps:
    - task: PowerShell@2
      displayName: Verify production health endpoint
      inputs:
        pwsh: true
        targetType: inline
        script: |
          $healthUrl      = '$(healthCheckUrl)'
          $expectedKw     = '$(healthCheckExpectedKeyword)'
          $maxAttempts    = [int]'$(verificationRetryCount)'
          $delaySeconds   = [int]'$(verificationRetryDelaySeconds)'
          $timeoutSeconds = [int]'$(verificationRequestTimeoutSeconds)'

          for ($attempt = 1; $attempt -le $maxAttempts; $attempt++)
          {
            Write-Host "Production health check attempt $attempt of $maxAttempts -- $healthUrl"
            try
            {
              $response = Invoke-WebRequest -Uri $healthUrl -Method Get -TimeoutSec $timeoutSeconds -SkipHttpErrorCheck
              if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 300)
              {
                if (-not [string]::IsNullOrWhiteSpace($expectedKw) -and $response.Content -notmatch [regex]::Escape($expectedKw))
                {
                  throw "Health response did not contain expected keyword '$expectedKw'."
                }
                Write-Host "Production health check passed (HTTP $($response.StatusCode))."
                return
              }
              Write-Host "Production health check returned HTTP $($response.StatusCode)."
            }
            catch
            {
              Write-Host "Production health check error: $($_.Exception.Message)"
            }
            if ($attempt -lt $maxAttempts) { Start-Sleep -Seconds $delaySeconds }
          }
          throw "Production health verification failed after $maxAttempts attempts."

- stage: Rollback_Prod
  displayName: Rollback Production
  dependsOn: Verify_Prod
  condition: and(failed('Verify_Prod'), ne(variables['Build.Reason'], 'PullRequest'), eq(variables['Build.SourceBranch'], 'refs/heads/master'))
  variables:
  - template: ../variables/prod.yml
  jobs:
  - deployment: RollbackProd
    displayName: Rollback slot swap
    environment: $(environmentName)
    pool:
      vmImage: windows-latest
    strategy:
      runOnce:
        deploy:
          steps:
          - task: AzureAppServiceManage@0
            displayName: Swap production back to previous version
            inputs:
              azureSubscription: $(azureServiceConnection)
              Action: Swap Slots
              WebAppName: $(appServiceName)
              ResourceGroupName: $(resourceGroupName)
              SourceSlot: $(stagingSlotName)
              SwapWithProduction: true
```

> If the root branch is `main` instead of `master`, replace every `refs/heads/master` with `refs/heads/main`.

> **Verification:** Confirm the file contains exactly three `azureSubscription: $(azureServiceConnection)` references (staging deploy, slot swap, rollback) and zero literal service connection name strings. Confirm `condition: and(succeeded(), ne(variables['skipDeployment'], 'true'))` is present on every deploy step that follows the `Evaluate CST deployment window` task.

### Database deploy pipeline `[Database]`

Database repositories require two pipeline files for the master branch: an entrypoint that owns the trigger and a deploy template that owns all steps. The deploy template must never be triggered directly — it is only invoked via `extends`.

Create `.azure-pipelines/workflows/<reponame>-master.yml` (master entrypoint):

```yaml
# Azure DevOps Pipeline — <RepoName> Master Entrypoint

trigger:
  branches:
    include:
      - master
  paths:
    include:
      - <SqlProjectName>/**
      - .azure-pipelines/**
    exclude:
      - "**/*.md"

pr:
  branches:
    include:
      - master
  paths:
    include:
      - <SqlProjectName>/**
      - .azure-pipelines/**
    exclude:
      - "**/*.md"

extends:
  template: <reponame>-master-deploy.yml
```

Create `.azure-pipelines/workflows/<reponame>-master-deploy.yml` (deploy template):

```yaml
# Azure DevOps Pipeline — <RepoName> Master Deploy Template
# Triggered via <reponame>-master.yml entrypoint; not triggered directly.

variables:
  buildPlatform: 'Any CPU'
  buildConfiguration: 'Release'
  sqlProjectName: '<SqlProjectName>'
  sqlProjectPath: '<SqlProjectFolder>/<SqlProjectName>.sqlproj'
  sqlServerName: '<SqlServerName>'
  #### Hardening requirements for database deploy pipelines

Every database deploy pipeline must satisfy all of the following:

| Requirement | Where enforced |
|---|---|
| `BlockOnPossibleDataLoss=True` | `SqlAzureDacpacDeployment@1` — all three actions: `DeployReport`, `Script`, `Publish` |
| `DropObjectsNotInSource=False` | `SqlAzureDacpacDeployment@1` — all three actions |
| `GenerateSmartDefaults=True` | `SqlAzureDacpacDeployment@1` — all three actions |
| DACPAC diff report generated before deploy | `SqlAzureDacpacDeployment@1` `DeploymentAction: DeployReport` |
| DACPAC deploy SQL script generated and published as artifact | `SqlAzureDacpacDeployment@1` `DeploymentAction: Script` + `PublishBuildArtifacts@1` |
| Destructive-schema-change detection enforced before deploy | Generated DACPAC script is scanned for destructive operations and the pipeline fails if a required pre-deploy script is missing |
| Pre-deploy backup copy created before DACPAC is applied | `AzureCLI@2` — `Create Pre-Deploy Backup Copy` |
| Auto-recovery restores from backup on deploy failure | `AzureCLI@2` — `Auto-Recovery` with `condition: failed()` |
| Pre-deploy backup deleted after success or failure | `AzureCLI@2` — `Delete Pre-Deploy Backup Copy` with `condition: always()` |
| `enableAutoCorruptionRecovery` must be `'true'` | Declared in pipeline variables |
| CST deployment window check with `skipDeployment` guard | `PowerShell@2` — `Evaluate CST deployment window` |
| Every deploy step guarded by `ne(variables['skipDeployment'], 'true')` | All tasks after the window check |
| Deploy runs only on `master` branch, not on PRs | `DeployAzure` stage `condition` |

> **`BlockOnPossibleDataLoss=True` is a hard requirement.** Any pipeline that omits this argument or overrides it to `False` must not be merged to the `master` branch. This setting causes the DACPAC deployment to fail rather than silently drop columns, tables, or constraints that would result in data loss.

---

> ⚠️ **This step is mandatory. Do not skip it.** The CST window check in the release pipeline controls whether an automated deployment proceeds. See **Section 2.3 and Section 2.3.1** for the authoritative behavior rules and required implementation pattern. The summary below is a reference — Section 2.3.1 is the source of truth.

### Required Implementation

The `Evaluate CST deployment window` PowerShell task **must** implement the following pattern exactly. This is duplicated from Section 2.3.1 for in-context reference — do not alter it here independently of Section 2.3.1.

1. **Outside window → skip cleanly, no error.** Set `skipDeployment=true` via `##vso[task.setvariable]` and use `return` to exit cleanly. The pipeline run must complete green. Never use `throw`, `exit 1`, or `##vso[task.complete result=SucceededWithIssues]`.
2. **Guard every downstream deploy step.** Add `condition: and(succeeded(), ne(variables['skipDeployment'], 'true'))` to every task that follows the window check.
3. **Manual runs always bypass the window.** Check `Build.Reason -eq 'Manual'` at the top of the script, set `skipDeployment=false`, and return immediately. A manual run deploys at any time regardless of CST window.

### Correct Script Pattern

```powershell
if ('$(Build.Reason)' -eq 'Manual')
{
  Write-Host 'Manual run — deployment window check bypassed.'
  Write-Host '##vso[task.setvariable variable=skipDeployment]false'
  return
}

$centralNow = [TimeZoneInfo]::ConvertTimeBySystemTimeZoneId([DateTimeOffset]::UtcNow, 'Central Standard Time')
$hour = $centralNow.Hour
Write-Host "Central time now: $($centralNow.ToString('yyyy-MM-dd HH:mm:ss zzz'))"

$inWindow = $hour -ge 20 -or $hour -lt 5
if (-not $inWindow)
{
  Write-Host "Outside 8 PM - 5 AM CST auto-deploy window (hour=$hour). Deployment skipped."
  Write-Host "To deploy now, trigger this pipeline manually."
  Write-Host '##vso[task.setvariable variable=skipDeployment]true'
  return
}
Write-Host 'Within 8 PM - 5 AM CST deployment window. Continuing.'
Write-Host '##vso[task.setvariable variable=skipDeployment]false'
```

### Correct Step Condition Pattern

Every deployment step after the window check must carry this condition:

```yaml
condition: and(succeeded(), ne(variables['skipDeployment'], 'true'))
```

### Expected Outcomes

| Scenario | Pipeline result | Deployment |
|---|---|---|
| Automated run, inside 8 PM – 5 AM CST | ✅ Green | Deploys |
| Automated run, outside window | ✅ Green | Skipped cleanly — no error |
| Manual run, any time | ✅ Green | Deploys |
| `throw` / `exit 1` used (wrong) | ❌ Red (Failed) | Blocked — do not implement this way |
| `SucceededWithIssues` used (wrong) | 🟡 Yellow | Blocked — do not implement this way |

---

## Step 8 — Configure the Health or Readiness Check URL `[Deployable]`

Both APIs and Web Apps require a health or readiness check URL. The staging and production verify steps issue an HTTP GET and expect a 2xx response before the slot swap is considered successful. The implementation differs by application type.

### API applications `[API only]`

APIs must expose a `/healthcheck` endpoint using the ASP.NET Core built-in health checks service.

Add to `Program.cs`:

```csharp
builder.Services.AddHealthChecks();
app.MapHealthChecks("/healthcheck");
```

Verify locally:

```powershell
dotnet run
Invoke-WebRequest http://localhost:<port>/healthcheck
# Expected: StatusCode 200, Content: "Healthy"
```

Set in `prod.yml`:

```yaml
healthCheckUrl: https://<appServiceName>.azurewebsites.net/health
healthCheckUrl: https://<appServiceName>.azurewebsites.net/healthcheck
healthCheckExpectedKeyword: Healthy
```

### Web App applications `[Web App only]`

Web Apps must also have a health check URL. Because ASP.NET Core Web App pages do not expose a `/healthcheck` endpoint by default, use a known stable page that reliably returns HTTP 200 or add the shared `/healthcheck` endpoint explicitly.

Options (choose one):
- App root (`/`) -- simple, always available, confirms the server is responding.
- A lightweight dedicated status page (e.g., `/status`) that returns a known string.
- Add `builder.Services.AddHealthChecks()` + `app.MapHealthChecks("/healthcheck")` to the Web App if you want the same endpoint pattern as an API.

Set in `prod.yml`:

```yaml
healthCheckUrl: https://<appServiceName>.azurewebsites.net/
healthCheckExpectedKeyword: ""    # Leave empty to accept any 2xx, or set a known string from the page
```

> The keyword match is optional for Web Apps. Leave `healthCheckExpectedKeyword` empty to pass on any 2xx response, or set it to a string reliably present in the page body.

---

## Step 9 — Register Pipelines in Azure DevOps `[All]`

Pipelines must be registered in Azure DevOps before they will run.

### Deployable applications -- register two pipelines

**Validation pipeline:**

1. Azure DevOps -> **Pipelines** -> **New pipeline**.
2. Source: **Azure Repos Git** -> select your repository.
3. Configure: **Existing Azure Pipelines YAML file**.
4. Branch: `dev` (or `master`/`main` if `dev` does not yet exist). Path: `.azure-pipelines/workflows/<reponame>-dev.yml`.
5. Click **Continue** -> **Save** (do not run yet).
6. Rename: **Rename/Move** -> `<RepoName>-Dev`.

**Release pipeline:**

1. Repeat the same steps.
2. Path: `.azure-pipelines/workflows/<reponame>-master-deploy.yml`.
3. Rename to `<RepoName>-Main`.
4. After saving, open pipeline settings -> **Default branch for manual and scheduled builds** -> set to `master` (or `main`). This prevents "YAML not found" errors on manual runs.

### Class Library -- register one pipeline `[Library]`

1. Azure DevOps -> **Pipelines** -> **New pipeline**.
2. Source: **Azure Repos Git** -> select your repository.
3. Configure: **Existing Azure Pipelines YAML file**.
4. Branch: `dev`. Path: `.azure-pipelines/workflows/<reponame>-dev.yml`.
5. Click **Continue** -> **Save**.
6. Rename to `<RepoName>-CI`.

### Database -- register two pipelines `[Database]`

**PR validation pipeline:**

1. Azure DevOps -> **Pipelines** -> **New pipeline**.
2. Source: **Azure Repos Git** -> select your repository.
3. Configure: **Existing Azure Pipelines YAML file**.
4. Branch: `dev`. Path: `.azure-pipelines/workflows/<reponame>-dev.yml`.
5. Click **Continue** -> **Save**.
6. Rename: **Rename/Move** -> `<RepoName>-Dev`.

**Master deploy pipeline:**

1. Repeat the same steps.
2. Path: `.azure-pipelines/workflows/<reponame>-master.yml` (the entrypoint, not the template).
3. Rename to `<RepoName>-Main`.
4. After saving, open pipeline settings -> **Default branch for manual and scheduled builds** -> set to `master`. This prevents "YAML not found" errors on manual runs.

> **Do not register `<reponame>-master-deploy.yml` directly.** It is a deploy template and must only be invoked via `extends` from `<reponame>-master.yml`. Registering it directly will cause authorization failures because it has no trigger and no source branch context.

> **Verification:** Run the following CLI command. Confirm the pipeline names appear and their `defaultBranch` matches `master` (deployable, database) or `dev` (library):
> ```powershell
> az pipelines list --org https://dev.azure.com/<OrgName> --project <ProjectName> --query "[].{name:name,id:id,branch:process.yamlFilename}" -o table
> ```

---

## Step 10 — Configure Branch Policies `[All]`

### Protect `dev`

1. Azure DevOps -> **Project Settings** -> **Repositories** -> select your repository -> **Policies** -> **Branch Policies** -> select `dev`.
2. **Require a minimum number of reviewers:** ON.
   - Minimum reviewers: `1`
   - **Allow requestors to approve their own changes:** ON (`creatorVoteCounts: true` -- sole-developer flow)
3. **Build validation:** Add:
   - Deployable: `<RepoName>-Dev`
   - Database: `<RepoName>-Dev`
   - Library: `<RepoName>-CI`
   - Trigger: Automatic | **Required** | Expires after push
   - > ⚠️ **The policy MUST be set to Required (blocking).** A PR cannot be completed unless the validation pipeline passes. Optional policies do not block merge.
4. **Limit merge types** to match team practice.
5. Click **Save changes**.

**Preferred: CLI automation (repeatable, scriptable)**

Run the following once per repo after registering the pipeline in Azure DevOps. Replace placeholder values with the actual pipeline definition ID and repository ID:

```bash
# Get the repository ID
az repos show --org https://dev.azure.com/<OrgName> --project <ProjectName> --repository <RepoName> --query id -o tsv

# Get the build definition ID
az pipelines list --org https://dev.azure.com/<OrgName> --project <ProjectName> --query "[].{name:name,id:id}" -o table

# Create the required build validation policy on dev
az repos policy build create \
  --org https://dev.azure.com/<OrgName> \
  --project <ProjectName> \
  --branch dev \
  --build-definition-id <BuildDefinitionId> \
  --repository-id <RepositoryId> \
  --blocking true \
  --enabled true \
  --manual-queue-only false \
  --queue-on-source-update-only true \
  --display-name "Require PR Validation to pass before merge" \
  --valid-duration 0
```

> `--valid-duration 0` means the policy approval never expires. `--queue-on-source-update-only true` re-queues automatically on every new push to the PR branch.

**CLI automation for `main` (non-blocking):**

```bash
# Create the non-blocking build validation policy on main/master
az repos policy build create \
  --org https://dev.azure.com/<OrgName> \
  --project <ProjectName> \
  --branch main \
  --build-definition-id <MainBuildDefinitionId> \
  --repository-id <RepositoryId> \
  --blocking false \
  --enabled true \
  --manual-queue-only false \
  --queue-on-source-update-only true \
  --display-name "Run Main delivery pipeline on promotion PR (non-blocking)" \
  --valid-duration 0
```

> `--blocking false` is required here. The pipeline still runs and is still visible on the PR, but its outcome does not gate PR completion — the pipeline's own coded gates and completion automation own that responsibility.

### Protect `master`/`main`

1. Repeat on the `master` (or `main`) branch.
2. Same reviewer policy settings as `dev`, except do not add a minimum-approver policy on `main`. `main` auto-completes with no human approval, driven only by the coded delivery gates (see [Section 3.2](#32-required-approver)).
3. **Build validation:** Add:
   - Deployable: `<RepoName>-Main` (build PR gate, deploy skipped on PR by condition)
   - Database: `<RepoName>-Main` (build PR gate, deploy skipped on PR by `DeployAzure` stage condition)
   - Library: `<RepoName>-CI` (Validate stage runs; Deliver stage skipped on PR by condition)
   - > ⚠️ **The policy on `main` MUST be set to Optional (non-blocking).** It must remain **enabled** so it still triggers the pipeline on every promotion PR, but it must not block PR completion. Delivery gating for the `main` pipeline lives entirely inside the pipeline's own stages (code-change detection and CST window check), and the pipeline's own automation completes the PR programmatically once those gates pass. If the branch policy on `main` is blocking, the pipeline's own completion request is rejected because the completing run is the in-progress validation build itself, which forces a manual override or manual completion and defeats the automated `dev → main` promotion path.
4. Save changes.

> **Verification:** Run the following CLI command and confirm `dev` shows a `True` blocking policy and `master`/`main` shows a `False` (non-blocking) policy:
> ```powershell
> az repos policy list --org https://dev.azure.com/<OrgName> --project <ProjectName> --query "[?settings.scope[0].refName != null].{branch:settings.scope[0].refName, blocking:isBlocking, enabled:isEnabled, pipeline:settings.buildDefinitionId}" -o table
> ```

---

## Step 11 — Authorize the Service Connection `[Deployable]`

On the first pipeline run, Azure DevOps may display:

> *"This pipeline needs permission to access a resource before this run can continue."*

1. Open the pipeline run -> click **View** next to the warning.
2. Click **Permit** -> **Permit** to confirm.

One-time per pipeline per service connection.

---

## Step 12 — Authorize the Environment `[Deployable]`

On the first run that uses the `<reponame>-prod` environment:

1. Open the pipeline run -> click **View** next to the environment warning.
2. Click **Permit** -> **Permit**.

One-time per pipeline per environment.

---

## Step 13 — Verify the Full Delivery Flow `[All]`

This step confirms every pipeline behaves correctly end-to-end. Each item below is a **required pass criterion**. Setup is not complete until every applicable item is confirmed.

### Deployable applications

Execute each check in order. Record the actual result. All must pass.

| # | Action | Expected result | Pass? |
|---|---|---|---|
| 1 | Create `feature/verify-setup` from `dev`, push a trivial change, open a PR to `dev` | `<RepoName>-Dev` pipeline triggers automatically | |
| 2 | Wait for the pipeline to complete | Pipeline result is ✅ green. Build and test pass. No deploy steps run | |
| 3 | Merge the PR to `dev` | Merge succeeds. No manual override needed | |
| 4 | Open a PR from `dev` to `master`/`main` | `<RepoName>-Main` pipeline triggers automatically | |
| 5 | Wait for the pipeline to complete | Pipeline result is ✅ green. Build step runs. `dotnet publish` and all deploy steps are **skipped** (condition-guarded) | |
| 6 | Merge the PR to `master`/`main` | Merge succeeds | |
| 7 | If current time is inside 8 PM–5 AM CST: wait for the triggered CI run to complete | Pipeline result is ✅ green. All stages run: Build_Publish → Deploy_Prod → Verify_Prod. No rollback | |
| 8 | If current time is outside 8 PM–5 AM CST: wait for the triggered CI run to complete | Pipeline result is ✅ green. `Evaluate CST deployment window` task sets `skipDeployment=true` and exits cleanly. All deploy steps skip. No error, no yellow status | |
| 9 | Trigger a manual run of `<RepoName>-Main` at any time | Pipeline deploys regardless of CST window. Result is ✅ green | |
| 10 | Confirm production health check URL responds | `Invoke-WebRequest https://<appServiceName>.azurewebsites.net/health` returns HTTP 200 with body containing `Healthy` | |

> **If any check fails:** stop, fix the issue in the step that owns that concern (branch policy → Step 10, YAML logic → Step 7/7a, health check → Step 8), then rerun from that check.

### Database

| # | Action | Expected result | Pass? |
|---|---|---|---|
| 1 | Create `feature/verify-setup` from `dev`, push a trivial change to the database project, open a PR to `dev` | `<RepoName>-Dev` pipeline triggers automatically | |
| 2 | Wait for the pipeline to complete | Pipeline result is ✅ green. `Build` stage runs: `VSBuild@1`, SQL source validation, DACPAC staged and published as artifact. No deploy steps run | |
| 3 | Merge the PR to `dev` | Merge succeeds. No manual override needed | |
| 4 | Open a PR from `dev` to `master` | `<RepoName>-Main` pipeline triggers automatically via `<reponame>-master.yml` entrypoint | |
| 5 | Wait for the pipeline to complete | Pipeline result is ✅ green. `Build` stage runs. `DeployAzure` stage is skipped — `ne(variables['Build.Reason'], 'PullRequest')` condition prevents deploy on PR | |
| 6 | Merge the PR to `master` | Merge succeeds | |
| 7 | If current time is inside 8 PM–5 AM CST: wait for the triggered CI run to complete schema analysis | Pipeline result remains green through `Analyze Schema Changes`. The generated deploy script and diff artifact are published. If destructive changes are present, the pipeline verifies that a pre-deploy script exists before continuing | |
| 8 | If destructive changes are present with no pre-deploy script | Pipeline fails before deploy. The log identifies the destructive operations and the missing pre-deploy script. No backup or DACPAC publish step runs | |
| 9 | If destructive changes are present and a pre-deploy script exists, or if no destructive changes are present | Pipeline result is ✅ green. Full sequence runs: diff report → deploy script → destructive-change check → pre-deploy backup → DACPAC deploy → backup cleanup → smoke validation | |
| 10 | If current time is outside 8 PM–5 AM CST: wait for the triggered CI run to complete | Pipeline result is ✅ green. `Evaluate CST deployment window` task sets `skipDeployment=true` and exits cleanly. No deploy steps run and no agent waits | |
| 11 | Trigger a manual run of `<RepoName>-Main` at any time | Pipeline deploys regardless of CST window. The same destructive-change and pre-deploy-script checks still run before deploy proceeds | |
| 12 | Verify the pre-deploy backup was created and then deleted | Pipeline log for `Create Pre-Deploy Backup Copy` shows backup database name. Log for `Delete Pre-Deploy Backup Copy` confirms deletion | |
| 13 | Verify no `BlockOnPossibleDataLoss=False` appears anywhere in either pipeline file | `Select-String -Path '.azure-pipelines/**/*.yml' -Pattern 'BlockOnPossibleDataLoss=False'` returns no results | |

> **If any check fails:** stop, fix the issue in the step that owns that concern (branch policy → Step 10, YAML logic → Step 7, pipeline registration → Step 9), then rerun from that check.

### Class Library

| # | Action | Expected result | Pass? |
|---|---|---|---|
| 1 | Create `feature/verify-setup` from `dev`, push a trivial change, open a PR to `dev` | `<RepoName>-CI` pipeline triggers automatically | |
| 2 | Wait for the pipeline to complete | Pipeline result is ✅ green. `Build_Test` stage runs: restore, build, test. No deploy | |
| 3 | Merge the PR to `dev` | Merge succeeds | |
| 4 | Open a PR from `dev` to `master`/`main` | No pipeline triggers (the `dev.yml` entrypoint only targets `dev`) | |
| 5 | Merge the PR to `master`/`main` | Merge succeeds. No pipeline gate required on `master`/`main` for libraries | |

---

## Quick Reference: Pipeline Layout by Application Type

### Deployable (API and Web App)

| File | Purpose |
|---|---|
| `.azure-pipelines/variables/common.yml` | Build settings and service connection name (top-level -- required for service connection resolution) |
| `.azure-pipelines/variables/prod.yml` | Environment values, health check URL, retry settings |
| `.azure-pipelines/workflows/<name>-validation.yml` | Validation pipeline -- build + test, PR gate on `dev`, never deploys |
| `.azure-pipelines/workflows/<name>-master-deploy.yml` | Release pipeline -- build PR gate + CI deploy, CST window, slot swap, rollback |

### Database

| File | Purpose |
|---|---|
| `.azure-pipelines/workflows/<name>-dev.yml` | PR trigger entrypoint — triggers on PR to `dev`; build, SQL validation, DACPAC artifact |
| `.azure-pipelines/workflows/<name>-master.yml` | Master entrypoint — owns CI trigger on `master`; extends deploy template via `extends:` |
| `.azure-pipelines/workflows/<name>-master-deploy.yml` | Deploy template — CST window gate, schema diff/script generation, destructive-change and pre-deploy-script enforcement, backup, DACPAC deploy, recovery |

### Class Library

| File | Purpose |
|---|---|
| `.azure-pipelines/variables/common.yml` | Build settings (no service connection) |
| `.azure-pipelines/workflows/<name>-dev.yml` | PR trigger entrypoint — triggers on PR to `dev`, extends `<name>-ci.yml` |
| `.azure-pipelines/workflows/<name>-ci.yml` | Build and test template — `Build_Test` stage only, no publish, no deploy |

> **Each class library must have its own separate pipeline registration.** Library pipelines must not be merged into a shared multi-library pipeline. Known active examples: `HttpClientManager`, `DataImportExportManager`. These are distinct pipelines and must remain distinct.

---

## Azure Rollout Artifact Consumption

Pipelines that deploy or validate Azure infrastructure must consume rollout artifacts according to the repository artifact model. Raw source captures and rollout packages do not serve the same purpose and must not be treated interchangeably.

### Source captures

- Raw exports under `Docs/Templates/Azure/<scope>/<name>/source/` must be treated as authoritative reference artifacts.
- Source captures may be used for comparison, regeneration, auditing, or package authoring, but they must not be assumed to be directly deployable without refinement.

### Rollout packages

- Deployment pipelines must target the grouped artifacts under `Docs/Templates/Azure/<scope>/<name>/packages/` when rollout automation is implemented.
- Package artifacts must represent resources that deploy together and must preserve declared dependencies between shared infrastructure and application rollout units.
- Where package manifests are used, pipeline logic must read deployment order and dependencies from the package layer rather than inferring them from unordered raw exports.

### Validation expectation

- Validation of Azure rollout automation must confirm that the package layer references the expected source artifacts, preserves deployment-unit grouping, and keeps the full resource-group export intact in the source layer.

---

## Quick Reference: Governance Rules

| Rule | Detail |
|---|---|
| Feature branches | Always cut from `dev`. Never from `master`/`main`. |
| PR flow | `feature/*` -> `dev` -> `master`/`main` |
| Direct commits | Blocked on both `dev` and `master`/`main` by branch policy |
| Validation pipeline | Build + test only. No publish, no deploy. |
| Release pipeline (PR mode) | Build only. Publish and deploy steps condition-guarded and skipped. |
| Release pipeline (CI, in window) | Full deploy: staging -> verify -> swap -> verify prod -> rollback if failed |
| Release pipeline (CI, outside window) | Deploy stage exits cleanly — green, no error. Sets `skipDeployment=true`. All deploy steps skipped via `condition: ne(variables['skipDeployment'], 'true')`. Pipeline must **not** `throw`, `exit 1`, or emit `SucceededWithIssues` — those are wrong. To deploy after a window-skipped run, trigger the pipeline manually. |
| Release pipeline (manual run) | Deploys immediately regardless of time window. |
| Service connection variable | Must live in `common.yml` at top level, not in stage-level variables. |
| API health check | Required. Add `/healthcheck` endpoint. Must return HTTP 200 with body containing `Healthy`. |
| Web App health check | Required. Use app root, a stable page, or add `/healthcheck` endpoint. Keyword match is optional. |
| Class library | No deployment infrastructure. Two-file CI pattern: `<name>-dev.yml` (trigger) + `<name>-ci.yml` (build/test template). Testing runs on `feature -> dev` PR only. Known active examples: `HttpClientManager`, `DataImportExportManager`. |
| Rollback trigger | Automatic if production health verification fails after slot swap. |
| Azure rollout artifacts | Pipelines use `source/` for reference and `packages/` for grouped deployment units. Raw exports are not the default deployment target. |
| `dev` branch validation policy | **Blocking (required).** PR cannot complete unless the validation pipeline passes. |
| `main`/`master` branch validation policy | **Non-blocking (optional), enabled.** Pipeline still triggers on every promotion PR, but delivery gating lives in the pipeline's own stages, not the branch policy. |
| Scheduled trigger on production pipeline | Created but left **disabled** until go-live; runs at the start of the configured CST delivery window once enabled. |
| Library feed publication | Package artifact creation may precede the test run; publication of the package to the Azure Artifacts feed must never occur until tests pass. |
| Build-once artifact hand-off | Build stage compiles once and publishes both the deploy artifact and the test/build output; Test stage reuses the build output (`--no-build --no-restore`); Publish/Deploy stage reuses the deploy artifact. No downstream stage recompiles. |

| Database `BlockOnPossibleDataLoss` | Must be `True` on all three `SqlAzureDacpacDeployment@1` actions (DeployReport, Script, Publish). Must never be set to `False` in any pipeline file. If the flag blocks a deploy, the correct response is a pre-deploy script — not overriding the flag. |
| Database backup | Pre-deploy backup copy created via `az sql db copy` before every DACPAC apply. Restored automatically on deploy failure. Deleted on pipeline completion regardless of outcome. |

---

## 4. Compliance Verification
<!-- STD-MARKER: azure-devops-pipeline.4 -->

**All application types**

- [ ] Feature branches cut from `dev` — never from `master`/`main`.
- [ ] Direct commits to `dev` and `master`/`main` are blocked by branch policy.
- [ ] PR validation pipeline is registered, renamed, and set as a required (blocking) build policy on `dev`.
- [ ] Release pipeline (deployable/database) is registered, renamed, and set as a non-blocking, enabled build policy on `master`/`main` (see [Section 3.2](#32-required-approver) and Step 10 — `main` must not be blocking).
- [ ] No minimum-approver policy is configured on `main`/`master`.
- [ ] Scheduled trigger on the production pipeline exists and is left disabled until go-live.
- [ ] No `<placeholder>` literals remain in any committed pipeline YAML file.
- [ ] Step 13 end-to-end verification table has been executed and all applicable checks pass.
- [ ] Azure rollout automation, when present, targets the package layer for deployment intent and preserves the source layer for raw exports.
- [ ] Build stage publishes both the deploy artifact and the build/test output; Test stage uses `--no-build --no-restore`; no downstream stage recompiles.

**Class Library**

- [ ] Package artifact is created by the Build stage; publication to the Azure Artifacts feed only occurs after the test run passes.

**Deployable applications (API and Web App)**

- [ ] Staging slot exists and is reachable at `https://<appServiceName>-staging.azurewebsites.net`.
- [ ] Service connection exists with the exact name declared in `common.yml`.
- [ ] Azure DevOps environment `<reponame-lowercase>-prod` exists.
- [ ] `azureServiceConnection` variable is declared in `common.yml` at top level — not in stage-level variables.
- [ ] `Evaluate CST deployment window` task exits cleanly (green) outside the window — `SucceededWithIssues`, `throw`, and `exit 1` must not be used. See Section 2.3.1 for the required pattern.
- [ ] Every deploy step after the window check carries `condition: and(succeeded(), ne(variables['skipDeployment'], 'true'))`.
- [ ] Health check URL returns HTTP 200 with expected keyword from both staging and production slots.
- [ ] Rollback stage is present and conditioned on `failed('Verify_Prod')`.

**Database (DACPAC)**

- [ ] `BlockOnPossibleDataLoss=True` is present in all three `SqlAzureDacpacDeployment@1` tasks (DeployReport, Script, Publish).
- [ ] `BlockOnPossibleDataLoss=False` does not appear anywhere in any pipeline file (`Select-String -Path '.azure-pipelines/**/*.yml' -Pattern 'BlockOnPossibleDataLoss=False'` returns no results).
- [ ] `Generate DACPAC Diff Report` (`DeploymentAction: DeployReport`) runs before the deploy step.
- [ ] `Generate DACPAC Deploy Script` (`DeploymentAction: Script`) runs before the deploy step.
- [ ] The generated deploy script artifact is published before the destructive-change check runs.
- [ ] The pipeline scans the generated deploy script for destructive changes before deploy.
- [ ] When destructive changes are detected, the pipeline requires a pre-deploy script in the repository's pre-deploy script location and fails if that script is missing.
- [ ] Destructive changes are explicitly treated as including `DROP TABLE`, `DROP COLUMN`, `DROP INDEX`, `ALTER COLUMN ... NOT NULL`, and rename patterns such as `sp_rename`.
- [ ] `enableAutoCorruptionRecovery` is set to `'true'` in pipeline variables.
- [ ] Pre-deploy backup task runs before `SqlAzureDacpacDeployment@1` Publish action.
- [ ] Auto-recovery task has `condition: and(failed(), ...)`.
- [ ] Backup cleanup task has `condition: and(always(), ...)`.
- [ ] `<reponame>-master-deploy.yml` is not registered as a pipeline directly — only `<reponame>-master.yml` is registered.
- [ ] Deploy template is invoked via `extends:` in `<reponame>-master.yml` — not via `template:` reference.

---

## 5. Governance
<!-- STD-MARKER: azure-devops-pipeline.5 -->

This standard is owned by the GlobalStandards repository maintainer. Changes require a pull request that includes a rationale comment explaining the reason for the update or deviation. No changes may be merged without maintainer approval. Direct commits to `dev` or `main` are not permitted.

This standard is owned by the GlobalStandards repository maintainer. Changes require a pull request that includes a rationale comment explaining the reason for the update or deviation. No changes may be merged without maintainer approval. Direct commits to `dev` or `main` are not permitted.
