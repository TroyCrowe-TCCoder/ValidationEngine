# GlobalStandards Repository Addendum

- **Global baseline:** [`Docs/Standards/GlobalGovernanceStandards.md`](../Docs/Standards/GlobalGovernanceStandards.md)
  - Ensure the root/index includes a dedicated section for each standards file: list each standard domain, link to the file that contains the detailed rules, and ensure every standards file is referenced and chained from the root governance document.
  - Include all actual standards files in the root governance chain even if they are not fully worked on yet (for example: GlobalAgentStandards.md, GlobalAgentTeamStandards.md). Add placeholders where content is pending to preserve the canonical chain.
  - Begin each section in Docs/Standards/GlobalGovernanceStandards.md with a limited, direct governance summary as the first statement; the summary must state the root governance rule for that domain and point to the child standard for implementation details.
  - Do not encode conditional or variance language (for example, "when required for the repository type") inside root governance rules; state the global rule directly and handle exceptions only through repository-level deviation documents placed in the target repository's `Standards/` folder.
  - Keep the governance root file on a conforming standards filename and identify content changes through version updates.

- **Standards location policy:** Keep global standards files in their canonical location; use the root-level `Standards/` folder only for repository-local deviation files. Treat templates as global starting-point artifacts: do not include repository-specific deviations in templates.
  - GlobalStandards is the sole source of global standards and reusable templates; sibling repositories must reference GlobalGovernanceStandards.md (for example, from their .github/copilot-instructions.md) and keep only approved local deviation files under their own root-level `Standards/` folder. Do not maintain parallel copies of global standards or shared runtime template assets in sibling repositories.
  - Keep only actual standards files in Docs/Standards/ and in any repository-level `Standards/` folder; move non-standards files (drafts, notes, process documents) to `Working/` or remove them when no longer useful. Clean up working/history artifacts when they are no longer needed. For this rollout, prefer deferring major repository-structure cleanup or refinement until after completing repository and pipeline rollout work end-to-end; focus first on rollout completion, then schedule cleanup as a follow-up to avoid disrupting deployment progress.
  - Include a remediation area inside standards files for task statements and future-state or next-phase notes. Move future-state or next-phase statements into that remediation area and also record them in `Working/ProjectBacklog.md` instead of placing them in the authoritative rule body.
  - Naming convention: Use PascalCase for all standards filenames. Global standards files use the `Global` prefix and the `Standards` suffix (for example, `GlobalEngineeringStandards.md`). Do not use non-conforming names (for example, avoid lowercase-hyphenated names like `global-engineering-governance` or `global-engineering-standards.md`); rename non-conforming files to the PascalCase pattern. Local deviation files must use the matching base name without the `Global` prefix, include the `Standards` suffix, use PascalCase, and reside in the target repository's root-level `Standards/` folder (for example, `GlobalEngineeringStandards.md` -> `Standards/EngineeringStandards.md`). For a global standard use `GlobalFileSpecificationStandards.md`; for a repository-local deviation use `FileSpecificationStandards.md` placed in the repository's `Standards/` folder.
  - Uppercase acronyms in solution identifiers and filenames; treat repository root folder names as allowed to lag behind this naming standard and handle them separately (do not rely on root folder naming to enforce the rule). For example, in the CaptiveExpenses repository the solution and project names have been updated to uppercase acronyms per standard while the root folder name was not changed; treat the root folder name as the current physical path only and not as the naming-standard source of truth.
  - Reference global standards from repositories; do not copy global standard files into local `Standards/` folders.
  - Template structure and placement:
    - Separate template domains: treat repository and pipeline templates as a distinct template domain from Azure resource/rollout templates; maintain separate top-level folders (for example: Docs/Templates/Pipelines/{RepositoryType}/{Environment} and Docs/Templates/Azure/) and do not mix repository/pipeline templates with Azure resource rollout templates.
    - Keep template assets self-contained under a clear templates hierarchy (for example: Docs/Templates/Pipelines/{RepositoryType}/{Environment}).
    - Package Azure rollout artifacts by deployment unit: resources that roll out together for a new application (for example: App Service, Application Insights, related monitoring, slots, alerts) should be stored together as one package rather than only as isolated per-resource files. Store these packages under a clear deployment-unit path within the templates hierarchy to maintain cohesion and simplify deployment.
    - PR automation (AutoPR carry-forward) is discontinued; the former Docs/Templates/PRs/ folder has been removed. Do not recreate it unless PR automation is reinstated (see GlobalAzureDevOpsPipelineStandards.md Section 3.3-3.4).
    - Avoid separate shared or variables template folders when possible; prefer self-contained templates per repository-type/environment to reduce cross-repo coupling.
    - Place scripts that belong to repository or pipeline templates under the matching templates structure (for example: Docs/Templates/Pipelines/{RepositoryType}/{Environment}/Scripts/).
    - Treat live repository-local assets separately from reusable templates. Files such as this repository's active `.azure-pipelines/workflows/*.yml` files or `Scripts/validate.ps1` may remain in their operational locations when they are part of the current repository implementation rather than reusable template content.
    - Copy templates into new repositories and update pointers there; do not embed repository-specific deviations inside the global templates.
    - Implement delivery-window checks, database destructive-change/pre-deploy mitigation enforcement, /healthcheck behavior, PR helper logic, and similar app-type behaviors via app-type templates stored in GlobalStandards. Use configurable delivery-window start and end values (template parameters or repository-level configuration) rather than hardcoded time bounds; document the configuration parameters and require repository initialization to replace template placeholders with repository-specific values. Copy these templates into target repositories and replace placeholders as part of repository initialization. Do not implement these behaviors as shared runtime assets inside sibling repositories.
    - Remove unused template or working files when they are no longer needed to prevent clutter and stale artifacts.
    - Azure rollout template formats: Use ARM as the canonical required export/source format for Azure rollout templates in this repository; do not require Terraform unless explicitly enabled later. When using authoring formats such as Bicep or JSON, include an ARM export or ensure tooling produces ARM artifacts and document any enabled alternative formats in the repository's standards.

- **Local deviation:** [`Standards/GovernanceStandards.md`](../Standards/GovernanceStandards.md)
- **Local deviation:** [`Standards/RepositoryStandards.md`](../Standards/RepositoryStandards.md)

## Standards Tone and Authority
- Maintain the directive model during standards review and restructuring; do not frame resulting standards content as suggestions or recommendations.
- Present standards as authoritative requirements, not optional guidance.
- Use a Purpose section to describe scope and applicability where appropriate. The Purpose may describe what the rules qualify or apply to; outside the Purpose section, write only descriptive, directive rules that must be followed. Avoid conditional or variance language in rule bodies; handle exceptions through repository-level deviation documents.
- When summarizing governance at the top of a section, keep the statement limited and direct; use it to convey the authoritative root rule and link to the child standard for implementation details.
- When a standards document is identified as a baseline, treat the entire document as an initial baseline draft: convert nonconforming or conditional language into direct, prescriptive rules; fill reasonable gaps with clear directive statements or explicit placeholders for later authoring; and defer final activation, publishing, or enforcement until explicit user review and approval.
- Avoid repetition in standards documents: consolidate overlapping onboarding and setup requirements instead of restating them across multiple sections. Centralize authoritative onboarding content in GlobalRepositoryOnboardingStandards.md and reference it from other standards; record repository-specific deviations in the repository `standards/` folder.

## Implementation Priorities
- Prioritize completing repository and pipeline rollout end-to-end. Do not begin delivery testing until the rollout changes have been merged and delivered to the branches and pipeline definitions that Azure DevOps actually uses; complete repository and pipeline rollout first, then run delivery tests. Defer non-essential structural reorganizations and large cleanup tasks until after rollout completion to avoid disrupting deployment progress and validation gating.
- Treat cleanup and refinement as scheduled follow-up work once rollout objectives and validation gates are stable.

## Onboarding and Repository Initialization
- Clone the GlobalStandards repository first when joining the project or starting work on standards.
- Treat GlobalGovernance/GlobalStandards as informational only for this rollout; continue to reference GlobalGovernanceStandards.md for authoritative rules and templates, and record any repository-local deviations in the repository `standards/` folder.
- Consolidate onboarding requirements: centralize common onboarding steps and validation commands in `GlobalRepositoryOnboardingStandards.md` to avoid duplicate content across standards files. Reference the centralized onboarding standard from other documents and place repository-specific deviations in the repository's `standards/` folder.

### Developer onboarding focus
- Focus developer onboarding on:
  - System setup (tools, accounts, environment configuration).
  - Learning the standards (where to find global and repository standards, required compliance steps).
  - Learning the code (build, tests, repository layout, local validator entry point).
  - Walking through the functional application (run the app locally, exercise key flows, smoke tests).
- Use `GlobalDeveloperOnboardingStandards.md` as the prescriptive basis for a later, narrative onboarding walkthrough document. Keep the prescriptive standard separate from repository setup guidance and the repository compliance onboarding standard.

### Authentication
- Use the device login flow with the provided device code to authenticate to Azure and Azure DevOps for interactive developer access; document the process and expected prompts in repository onboarding materials.
- Record any non-interactive or service principal authentication methods required by automation in the repository standards and onboarding documents.

- When creating a new repository:
  - Add `.github/copilot-instructions.md` first and include a link to `Docs/Standards/GlobalGovernanceStandards.md`.
  - Create a root-level `Standards/` folder for approved local overrides after adding the copilot instructions file.
  - Reference global standards from the canonical locations; do not duplicate global files into the new repository.
  - Add a repository-local validation entry point: include a top-level script or defined command (for example, a `scripts/validate` executable, a Makefile `validate` target, or a `package.json` script named `validate`) that runs build checks, gating rules, and related validation used for PR gating. Ensure CI/CD templates invoke this entry point and require it for pull-request validation before work proceeds. Make this a standing requirement: document the entry point and track any planned expansion of its capabilities as backlog items rather than implementing unapproved changes immediately.
  - Document the validation entry point usage in the repository README and in `GlobalRepositoryOnboardingStandards.md`.
  - Treat `GlobalRepositoryOnboardingStandards.md` as the repository compliance and setup document: use it to bring a repository into standards compliance and initial configuration; do not use it for new-developer onboarding.
  - Author `GlobalDeveloperOnboardingStandards.md` as a focused, specific onboarding standard (tools, access, first-day tasks, role-specific orientation). List the exact steps required to get a developer onboarded and up to speed. Make the document prescriptive and include explicit checklists, verification steps, required accounts/permissions, environment setup commands, common troubleshooting tips, and links to repository and organization resources. Use this document as the authoritative source for producing a separate onboarding walkthrough. Keep it separate from repository setup guidance and author it when planned.

### Pre-PR Local Validation
- Require a single, explicit local command entry point that runs all pre-PR validation (build, lint, tests, security scans, and gates). Provide pre-checkin (pre-commit/pre-push) hooks that invoke this entry point and block local check-in until validation passes; document hook installation, expected behavior, and any approved bypass or emergency procedures.
- Place the entry point in a conventional location (repository root, scripts/, or package manager scripts) and keep its invocation simple and stable for CI templates to call.
- Ensure CI and pipeline templates call the local entry point instead of embedding duplicate commands; keep pipeline templates as references to the repository-local validator.
- Document expected outputs and non-zero exit behavior so contributors and automation understand failure modes.
- Treat future expansions to the validator as backlog items to be planned, reviewed, and approved; do not implement unapproved capability additions immediately.
- Allow proactive additions to related standards files to describe recommended content for the validator; treat such additions as proposals subject to later review and approval.

## Pipeline and Deployment Standards
### Environment model for rollout/testing
- Use only two environments for this rollout: Dev and Production.
- Dev is local developer machines only; do not create an Azure QA environment for this rollout.
- Production is Azure PaaS hosted in the organization's Azure subscription / CaptiveInnovationsRG and includes:
  - App Services: CaptiveExpenses, CaptiveExpensesAPI, CaptiveMessagingAPI, CaptiveDocumentManagerAPI
  - The CaptiveExpenses production database
- Treat DocumentImportExportManager and HttpClientManager as NuGet class libraries for this rollout; build and publish them as versioned NuGet packages.
- Use isolated delivery test paths that do not target active live environments during rollout preparation; enable live production deployments only after rollout changes are merged and validated.
- Continue using isolated delivery test paths that do not target active live environments during rollout preparation; enable live production deployments only after rollout changes are merged and validated.

- Use isolated delivery test paths that do not target active live environments; enable live environments only after rollout and development are complete.
- Use configurable delivery-window start and end values instead of hardcoded time bounds in rollback pipeline logic; store values as template parameters or repository-level configuration and document them in repository standards.
- Distinguish repository types and apply pipeline rules accordingly:

  - Delivery model and branch-promotion flow
    - Treat the dev -> main promotion as a delivery-only channel. Do not require a human approval process for the dev -> main (master) promotion; feature -> dev is the authoritative validation gate for code, tests, and gating checks. Allow deployments from main only within an approved time window; outside the window require the pipeline to pause in a green state and defer the deployment. Resume deferred deployments via a later automation or a scheduled job that advances pending deployments. Manual runs of the dev -> main promotion pipeline may bypass the configured delivery window.
    - Do not use SucceededWithIssues to represent or implement the time-window skip behavior; use an explicit paused/deferred state and automation to resume.
    - Use configurable delivery-window start and end values (template parameters or repository-level configuration) rather than hardcoded bounds; validate and document the configured values in the repository's standards.
    - For the dev -> main (master) promotion pipeline, do not run build or test validation. Run build/test validation earlier in the flow (for example, feature -> dev or dev pipelines). The dev -> main promotion pipeline must perform only running-service and running-database post-deployment validation and roll back if the service or database is unhealthy (use /healthcheck as the canonical running-service health endpoint when aligned with the actual repositories).
    - Permit temporary adjustment of delivery-window start and end values for delivery testing to validate that deferred deployments are automatically picked up and resumed when the configured window opens; document and revert test-specific adjustments in the repository standards.

  - Documentation-only repositories
    - Require a documented local validation script or equivalent that contributors run prior to PR work.
    - Require a validation pipeline on `dev` and a carry-forward `dev -> main` promotion pipeline. Treat the `dev -> main` promotion as delivery-only and subject to the approved time-window behavior: pause and defer deployments outside the window and resume them via automation. Do not require a human approval process for the `dev -> main` promotion; feature -> dev remains the authoritative validation gate. Manual runs of the `dev -> main` promotion pipeline may bypass the window.
    - For the `dev -> main` promotion pipeline, do not run build/test validation; perform post-deployment running-service checks and rollback on failure.
    - Do not require a production deployment pipeline unless an approved repository-specific deviation adds one.

  - Database repositories
    - Require a documented local validation script or equivalent that contributors run prior to PR work.
    - Do not introduce a manual human review/approval gate as a fail-safe. Use automated destructive-schema-change detection and require a pre-deploy mitigation script when destructive changes are detected. Enforce BlockOnPossibleDataLoss=True for DACPAC (or equivalent) so that if DACPAC still detects possible data-loss issues after a pre-deploy script runs, the deployment stops. Do not reintroduce a human gate as the primary fail-safe. This change does not alter the green outside-window delivery behavior.
    - For the `dev -> main` promotion pipeline, do not run build/test validation; perform post-deployment running-database checks and rollback on failure. Ensure pre-deploy mitigation scripts run as needed when destructive changes are detected. Do not require a human approval process for the `dev -> main` promotion; feature -> dev is the authoritative validation gate. Manual runs of the `dev -> main` promotion pipeline may bypass the configured delivery window.
    - Require automated destructive-schema-change detection that:
      - Detects destructive operations and identifies the change type.
      - Define "destructive schema change" as any schema modification that may cause data loss or irreversible structural change. Examples include, but are not limited to:
        - DROP TABLE, DROP COLUMN
        - TRUNCATE TABLE
        - ALTER COLUMN that narrows type or reduces length (for example, VARCHAR(255) -> VARCHAR(50)) or changes nullability in a way that can cause data loss
        - Column type conversions that are not guaranteed to be lossless (for example, numeric -> integer)
        - Removal of primary keys or unique constraints that will break referential integrity or cause data de-duplication loss
        - Removal of foreign-key constraints that will affect referential integrity and downstream data assumptions
        - Any operation that deletes, irreversibly transforms, or discards existing data
    - Require an accompanying pre-deploy mitigation script when destructive changes are detected. The pre-deploy script must:
      - Describe the detected destructive change and expected impact
      - Include mitigation or migration steps to preserve or transform data safely (for example: backfills, data-copy, staged rollouts, or validation queries)
      - Provide verification steps and rollback guidance
    - Fail validation and notify the author and approvers if destructive changes are found without an accompanying, valid pre-deploy script.
    - Allow the pipeline to continue when destructive changes include a valid pre-deploy script and automated checks pass; however, if DACPAC (or equivalent) still reports possible data loss, BlockOnPossibleDataLoss=True must block the deployment.
    - Keep deeper validator expansions and additional automated gating for databases as backlog items until reviewed and approved.

  - API / WebApp repositories
    - Implement a gated CI/CD flow that includes:
      - PR approval gating
      - Code-change detection to trigger the pipeline
      - Optional maintenance/time-window check before deploy
      - Build validation (tests, lint, security scans)
      - Deploy to target environment
      - Use a staging slot release path for deliverable WebApp and WebAPI repositories: deploy to a staging slot, validate staging is healthy (smoke tests, health checks — use /healthcheck as the canonical health endpoint when aligned with the actual repositories), perform a slot swap to production, then validate production post-swap to achieve near-zero downtime. Document slot-specific configuration and ensure environment-specific settings are handled correctly.
      - Deployment validation (smoke tests, health checks)
      - Rollback and notification on deployment failure
    - Ensure the pipeline calls the repository-local validator and documents expected outputs, failure modes, and notification targets.
    - For the `dev -> main` promotion pipeline, do not run build/test validation as part of the promotion; rely on earlier promotion stages to have run build/test validation. The `dev -> main` promotion must perform only running-service post-deployment validation and rollback on failure. Do not require a human approval process for the `dev -> main` promotion; feature -> dev is the authoritative validation gate. Manual runs of the `dev -> main` promotion pipeline may bypass the configured delivery window.

  - Library and package repositories
    - Produce reproducible, versioned build artifacts and publish them through the branch-promotion flow (feature -> dev -> main).
    - Package class libraries as NuGet even if consumed by only one repository; cross-repository reuse is not required to justify NuGet packaging.
    - For named libraries in this rollout, ensure HttpClientManager and DocumentImportExportManager produce NuGet packages as part of the promoted build pipeline while still following the feature/dev/main promotion flow.
    - Document package versioning, feeding, and publishing steps in the repository standards and ensure CI templates call the repository-local validation and packaging entry points.
    - Apply the dev -> main promotion behavior described above (no human approval required; feature -> dev is the authoritative validation gate; promotion enforces the configurable delivery window with green pause and scheduled pickup; manual runs may bypass the window).

### Monitoring and Observability
- Use Azure Monitor as the umbrella monitoring platform for this rollout; configure Application Insights as an Azure Monitor service and include a (single shared) Log Analytics workspace as part of the standard monitoring setup to serve as the central, unified telemetry store.
- Use a single shared Azure Monitor Log Analytics workspace as the central, unified telemetry store so all application monitoring, analytics, and topology flow into one consolidated monitoring infrastructure view.
- Treat Application Insights as an Azure Monitor service and configure application telemetry through Application Insights within Azure Monitor.
- Log destination: The repository must configure the destination for ILogger<T> output in Program.cs. Application Insights is preferred. Any approved alternative must be documented in the repository addendum. Scaffold logging-provider integrations behind clear interfaces and adapter classes so onboarding a provider account or configuration requires only registration/configuration changes (for example DI registration or configuration values) without modifying application business code.
- Log message content: Use the method signature (including class and method name) as the basis for describing what happened in log entries; accompany it with a concise action/result description and only necessary context. Avoid logging sensitive data.
- Prefer Azure Monitor services to provide capabilities currently delivered via separate workspace-based setups; consolidate telemetry, alerts, and diagnostics in Azure Monitor when feasible.
- When Log Analytics workspaces are required, link Application Insights and other telemetry sources to the designated single shared workspace and document the linkage in repository standards.
- Configure monitoring resource creation and linkage in deployment templates; require repository onboarding and standards documents to record monitoring configuration and any approved deviations.
- Reference global monitoring standards from GlobalStandards; do not duplicate monitoring configuration across repositories. Record repository-local deviations in the repository's standards/ folder.
- Use /healthcheck as the canonical health endpoint value in standards when it aligns with the actual repositories; document any deviations in the repository standards.
- Define required rules for configurable SaaS service-provider integrations as provider-agnostic requirements (for example: authentication configuration, telemetry destination selection, sampling and retention defaults, alerting contract, and configuration override points). Do not embed provider-specific product choices into the authoritative standard body.
- Track provider-specific implementations and prospective vendors (for example: Serilog, Dynatrace) as remediation/backlog items or task statements in the standards remediation area and in `Working/ProjectBacklog.md`; treat these entries as implementation options to be evaluated rather than hard requirements. Record any approved repository-level provider deviations in the repository's standards/ folder.
- When Azure monitoring changes require application code updates in another repository, add an urgently important backlog item in the target repository to update the Application Insights registration/configuration; link the backlog item to the monitoring change, record it in the standards remediation area and `Working/ProjectBacklog.md`, and track completion there.
- Alerting and notification:
  - Use explicit email or webhook receivers for production alerting in Azure Monitor; do not use ARM role receivers.
  - Document alert routing, receiver endpoints, and ownership in the repository's standards and onboarding documentation; record any approved deviations there.

- Logging and telemetry specifics:
  - Do not require audit timestamps to be UTC. Document the chosen timestamp format and timezone handling in the repository's standards to ensure consistent interpretation.
  - Log errors and critical exceptions at the global error handler location; avoid duplicative error logging across layers.
  - Do not require a /ready endpoint; do not add /ready unless it is explicitly implemented and documented as a deviation from the canonical /healthcheck endpoint.
  - For large data imports performed in 100-row batches, emit at most one summary log record per 100 records rather than logging each row individually.
  - Retain analytics telemetry used for product-improvement usage for at least 6 months. If product-improvement analytics are not yet implemented, record the implementation work as a remediation/backlog item in the standards remediation area and `Working/ProjectBacklog.md`.

#### Dashboards
- Use one suite-level executive summary dashboard that drills into individual application dashboards.
- Create separate, detailed dashboards for each application for granular investigation and troubleshooting.
- Link the executive dashboard to per-application dashboards and surface shared telemetry from the single Log Analytics workspace.
- Treat existing dashboards in CaptiveInnovationsRG as likely auto-generated defaults; do not consider them authoritative. Review and, where appropriate, replace or redesign them as deployable artifacts under templates/monitoring/dashboards. Record review findings, replacements, and deprecation steps in the standards remediation area and `Working/ProjectBacklog.md`.
- Store dashboard definitions as deployable artifacts under templates/monitoring/dashboards; require an ARM artifact as the canonical export/source format. Allow Bicep or JSON sources only if an ARM export is included and the alternative formats are documented.
- Store dashboard definitions as deployable artifacts (ARM/Bicep/JSON) under templates/monitoring/dashboards and document deployment and update processes in repository standards.
- Record dashboard ownership, update cadence, and any approved deviations in the repository's standards remediation area.

### Azure DevOps Service Connections
- Use a new User-Managed Identity (UMI) named CI-DevOps-UMI placed in the same resource group as the target applications; use this UMI for deployment service connections.
- For this rollout, if an existing UMI-backed Azure DevOps service connection cannot be edited, delete and recreate it to avoid leaving unused resources.
- When replacing Azure resources whose names differ only by casing, remove the obsolete resource first to avoid case-insensitive naming collisions.
- Recreate service connections to conform to naming, permission, and documentation standards; reference the recreated connection from pipeline templates and record the change in the repository's onboarding or standards files.
- Remove any orphaned or unused service connection artifacts from the project to prevent stale credentials or resource sprawl.
- Document and support interactive developer authentication using the device login flow (device code) when granting or verifying access; include steps in onboarding and service-connection documentation.

## Copilot Instructions

## General Guidelines
- For multi-step tasks, complete the work and provide a concise summary of what was finished at the end; do not report each intermediate step.
- After a plan is approved by the user — the user has explicitly authorized autonomous continuation until the current standards task is complete and will review the results afterward — proceed end-to-end without pausing for additional approvals or seeking confirmation for each execution step; use user-authorized bulk PR approvals to merge required PRs as needed. Avoid repeated approval prompts during rollout execution; proceed without waiting unless there is a genuine, open decision that requires explicit approval. Set up the expected environment before running delivery tests; when directed, promote CaptiveMessagingAPI from master to main immediately as part of environment setup rather than deferring.
- Track internal plan progress privately (do not include intermediate planning details); ensure the final summary reflects completed outcomes.
- When follow-up backlog items require changes across sibling repositories within the same workspace, update those sibling repositories in the background without switching the active repository context; record and link the changes in the originating repository's remediation/backlog area.

## Validation Engine
- Before implementing the ValidationEngine operational plan, first discuss and agree on the operating model (how it's kicked off — pre-commit/CLI/CI, where the tool and config/standards files need to be located relative to a consuming repo, full-run vs changeset-run triggers) — this discussion should happen before/alongside plan execution, not just be assumed.

## Standards Brush-Up
- Resume interrupted work by promoting standards files to Active status, focusing on `GlobalDeveloperOnboardingStandards.md` as the most recent priority.

## Standards Library README Structure
- Structure the README like a catalog outline rather than a narrative overview.
- The standards-library README should be structured like a book catalog: grouped by subject headings, with each standards file listed as a linked chapter under the appropriate group.
