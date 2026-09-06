# WebApiWebApp Pipeline Templates

## Purpose

This folder mirrors the `.azure-pipelines/` folder structure of `CaptiveExpensesAPI`, which is the current reference-standard pipeline build for ASP.NET Core Web API / Web App repositories deployed to an Azure App Service via a staging-slot swap model. These files are templates only. They do not become active pipeline files until they are copied into a target repository, placed in the correct `.azure-pipelines` folders, and configured through the shared variables file.

## Required Outcome

Use these templates to produce a working Dev → Main pipeline chain for a deployable API/Web App repository that:

- Detects code-only vs. documentation-only changes and skips downstream stages when nothing code-relevant changed.
- Validates GlobalStandards compliance on `dev` PRs.
- Builds, tests, and publishes an artifact to a Dev deployment slot.
- On `main`, swaps a staging slot into production behind a health check and configurable delivery window, validates the live site after swap, and automatically rolls back on validation failure.

## Folder Structure

- `Dev/` — Dev environment stage templates (`Build.yml`, `Test.yml`, `Publish.yml`) and the pipeline entry point (`Dev.yml`).
- `Main/` — Main/production promotion stage templates (`Swap.yml`, `Validate.yml`, `Rollback.yml`) and the pipeline entry point (`Main.yml`).
- `QA/` — Reserved placeholder for a future QA environment stage. Not yet implemented.
- `Shared/` — Cross-environment reusable templates (`ChangeDetection.yml`, `StandardsValidation.yml`).
- `Variables/` — Single shared `Variables.yml` file containing all placeholder tokens used across the Dev and Main templates.

## Direction For The AI Model

- Treat every YAML file in this folder as reusable source material for rollout.
- Copy `Dev/`, `Main/`, `QA/`, `Shared/`, and `Variables/` into the target repository under `.azure-pipelines/` with the same relative structure.
- Replace every `<placeholder>` token across all copied files with the target repository's actual values before considering the rollout complete. Do not leave unresolved placeholder tokens in active repository files.
- Keep the YAML files variable-driven. Do not replace shared `$(...)` runtime variable references with repository-specific literals when a shared variable already exists in `Variables.yml`.
- Keep `dev` and `main` fixed as the branch names referenced by the Dev and Main pipeline entry points. Do not introduce placeholders for these branch names.
- Preserve the distributed folder structure and keep orchestration files (`Dev.yml`, `Main.yml`) separate from stage-action files (`Build.yml`, `Test.yml`, `Publish.yml`, `Swap.yml`, `Validate.yml`, `Rollback.yml`).
- Do not copy a `Scripts/` folder — standards validation invokes `Scripts/RunValidationEngine.ps1` from the `GlobalStandards` repository checkout, not a local script.
- Validate the copied paths, service connection name, SDK version, artifact settings, App Service names, resource group, slot name, health-check URLs, and delivery-window hours before rollout is considered complete.

## Direction For The Human Developer

- Copy `Dev/`, `Main/`, `QA/`, `Shared/`, and `Variables/` into the target repository at `.azure-pipelines/`.
- Replace every `<placeholder>` token in `Variables/Variables.yml` and any files that reference App Service/environment names directly (deployment `environment:` values cannot expand runtime variables) before enabling or validating the pipeline.
- Leave the `$(...)` references in the Dev/Main YAML files intact unless you are intentionally changing the shared template design.
- Add the `GlobalStandards` repository resource declaration to your `Dev.yml` (see the template) so the standards-validation stage can check out `GlobalStandards` alongside your repository.
- Every ASP.NET Core Web API/Web App repository provisioned from this template family is required to have a staging deployment slot on its App Service. The staging-slot swap pattern (`Swap.yml`/`Validate.yml`/`Rollback.yml`) is not optional and is not conditioned on App Service tier — provision the App Service with a plan/tier that supports at least one deployment slot as part of standing up the repository's infrastructure.
