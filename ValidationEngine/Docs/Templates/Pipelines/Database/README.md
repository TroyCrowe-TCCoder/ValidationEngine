# Database Pipeline Templates

## Purpose

This folder mirrors the `.azure-pipelines/` folder structure of `CaptiveExpensesDB`, which is the current reference-standard pipeline build for SQL Database (`.sqlproj`/DACPAC) repositories deployed to Azure SQL. These files are templates only. They do not become active pipeline files until copied into a target repository, placed in the correct `.azure-pipelines` folders, and configured through the variable files.

## Known Deferred/Unfinished Work In The Source Repository

The source (`CaptiveExpensesDB`) pipeline is functional but has open items that were deliberately deferred rather than solved. Carry these forward as known gaps when using this template — do not silently "complete" them without the user's direction, and flag them again when a target repository's rollout reaches the affected step:

- **Post-deploy smoke validation is a stub.** `workflows/MainDeploy.yml` Step 5 contains a placeholder step (`Post-Deploy Smoke Validation`) that only logs a message. No live schema/query-based validation has been implemented yet.
- **Pre-deploy backup/rollback uses `az sql db copy`**, which only works against Azure SQL Database (not Managed Instance or SQL on VM). If a target repository's SQL target differs, this rollback mechanism must be redesigned before use.
- **Destructive-change detection is pattern-based** (regex over the generated diff script) and only covers a fixed list of SQL operations (`DROP TABLE`, `DROP COLUMN`, `DROP INDEX`, `ALTER COLUMN ... NOT NULL`, `sp_rename`). It is not a complete static analysis and may miss other destructive patterns.
- **The dev PR pipeline's "test" stage does not execute SQL tests** — it only inspects that `.sql` test script files exist under `Tests/StoredProcedures`. No actual test execution or tSQLt integration exists yet.
- **`SqlResourceGroup` and `deliveryWindowStartHourCst`/`deliveryWindowEndHourCst`** are currently hardcoded per-repo in `variables/Prod.yml` rather than resolved dynamically; this mirrors the same CST-delivery-window pattern used by `WebApiWebApp`.

## Folder Structure

- `workflows/` — Pipeline entry-point/orchestration files: `Dev.yml` (PR gate against `dev`), `Main.yml` (thin `main` trigger entry point that extends `MainDeploy.yml`), and `MainDeploy.yml` (the full window-check/build/analyze-schema/deploy/rollback stage chain).
- `variables/` — `Common.yml` (shared build/DB settings), `Dev.yml` (dev-environment overrides), and `Prod.yml` (production-environment overrides: service connection, resource group, delivery window).

## Direction For The AI Model

- Treat every YAML file in this folder as reusable source material for rollout, consistent with the deferred items noted above.
- Copy `workflows/` and `variables/` into the target repository under `.azure-pipelines/` with the same relative structure.
- Replace every `<placeholder>` token across all copied files with the target repository's actual values before considering the rollout complete. Do not leave unresolved placeholder tokens in active repository files.
- Keep the YAML files variable-driven. Do not replace shared `$(...)` runtime variable references with repository-specific literals when a shared variable already exists in `variables/Common.yml`, `variables/Dev.yml`, or `variables/Prod.yml`.
- Keep `dev` and `main` fixed as the branch names referenced by the pipeline entry points. Do not introduce placeholders for these branch names.
- Do not silently implement the deferred items listed above (post-deploy smoke validation, SQL test execution, expanded destructive-change detection) as part of a template rollout — surface them to the user as known follow-up work instead.

## Direction For The Human Developer

- Copy `workflows/` and `variables/` into the target repository at `.azure-pipelines/`.
- Replace every `<placeholder>` token in `variables/Common.yml` and `variables/Prod.yml` before enabling or validating the pipeline.
- Ensure the target SQL database has a pre-deploy script location (`Deployment/Deploy`) if destructive schema changes are ever introduced; the pipeline blocks destructive changes without one.
- Confirm `enableAutoCorruptionRecovery` behavior is appropriate for the target Azure SQL Database (requires `az sql db copy`, which is Azure SQL Database-only).
- Be aware of the deferred/unfinished items listed above before relying on this template as a complete solution.
