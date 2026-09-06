# Repository Onboarding Standards

**Version:** 2.0.0
**Status:** Active
**Applies To:** All repositories under the GlobalStandards governance model
**Audience:** AI models and human developers
**Created:** 2026-07-12
**Last Modified:** 2026-08-08

---
<!-- STD-MARKER: repository-onboarding.file -->


## 1. Purpose
<!-- STD-MARKER: repository-onboarding.1 -->

This document defines the required state of a repository at the time it is created and brought under the GlobalStandards governance model. It is a creation-time baseline checklist, not a remediation, verification, or ongoing-compliance process. A governed repository must satisfy every applicable item in this file before it is used for production delivery, package publication, or environment-bound deployment.

Onboarding is a single, one-time event completed in full when a repository is created or first brought under governance. It is not a recurring or resumable process, and it is not a mechanism for tracking ongoing compliance -- ongoing compliance is the subject of [GlobalRepositoryStandards.md](../standards/GlobalRepositoryStandards.md) and the other active standards files, not this one.

---

## 2. Repository Governance Entry Points
<!-- STD-MARKER: repository-onboarding.2 -->

Every repository must contain the following entry points before AI-assisted or governed development begins:

| Path | Requirement |
|---|---|
| `.github/copilot-instructions.md` | Must exist and must link first to the designated global root governance standard. Any repository-local deviation documents must be listed after that global baseline link. |
| `Standards/` | Must exist at the repository root and must contain repository-local deviation files only. |
| `README.md` | Must exist at the repository root and must describe the repository's purpose and how to get started locally. |
| `.gitignore` | Must exist at the repository root and must exclude build output, IDE artifacts, and any local secrets/settings files appropriate to the repository's technology stack. |

The repository entry point structure must follow [GlobalSolutionStructureStandards.md Section 2.1](../standards/GlobalSolutionStructureStandards.md#21-repository-standards-folder).

---

## 3. Branch Model
<!-- STD-MARKER: repository-onboarding.3 -->

Every repository must use `main` as the root branch and `dev` as the integration branch as defined in [GlobalRepositoryStandards.md Section 2](../standards/GlobalRepositoryStandards.md#2-branch-model).

At repository creation:

1. Create the repository with `main` as the root branch.
2. Create `dev` from `main` immediately after repository creation.

---

## 4. Branch Policies
<!-- STD-MARKER: repository-onboarding.4 -->

Branch protection must be configured in Azure DevOps according to [GlobalRepositoryStandards.md Section 7](../standards/GlobalRepositoryStandards.md#7-ado-branch-policy-configuration) at the time the repository is created.

At minimum, the following must be configured before the repository is used for governed development:

- `dev` blocks direct commits.
- `dev` requires the governed validation pipeline.
- `dev` enforces the required reviewer policy unless an approved deviation replaces it.
- `main` blocks direct commits.
- `main` allows only the approved non-approval promotion policies for the `dev -> main` path unless an approved deviation adds an approval gate.
- No specific merge strategy is mandated for `dev` or `main` unless an approved repository-specific deviation restricts merge types.

---

## 5. Solution and Test Baseline
<!-- STD-MARKER: repository-onboarding.5 -->

The repository structure must conform to [GlobalSolutionStructureStandards.md](../standards/GlobalSolutionStructureStandards.md) from creation.

Before new feature work begins, the repository must have:

- A solution file at the repository root.
- Each project in its own top-level directory.
- The required test project for the solution, containing at least one or two representative starter tests.

---

## 6. Approved Template Assets
<!-- STD-MARKER: repository-onboarding.6 -->

Repository creation must use the approved reusable template assets rather than recreating baseline files ad hoc.

The following template rules apply:

- Pipeline templates copied or adapted at creation must come from `Docs/Templates/Pipelines/`.
- PR automation is currently discontinued; there are no PR-related templates to copy at creation. If reinstated in the future, they would come from `Docs/Templates/PRs/` (see GlobalAzureDevOpsPipelineStandards.md Section 3.3-3.4).
- Azure rollout artifacts copied or adapted at creation must preserve the two-layer model defined in [GlobalRepositoryStandards.md Section 9](../standards/GlobalRepositoryStandards.md#9-azure-rollout-artifact-placement).
- Scripts that belong to a template must remain with that template asset rather than being placed in an unrelated shared script area.

The current approved Azure rollout structure is demonstrated under `Docs/Templates/Azure/suites/CaptiveInnovations/`, where raw exports live under `source/` and grouped rollout units live under `packages/`.

---

## 7. Local Validation Entry Point
<!-- STD-MARKER: repository-onboarding.7 -->

Every repository must expose a documented local validation script or equivalent command entry point before pull requests are opened.

The local validation entry point must follow [GlobalAzureDevOpsPipelineStandards.md Section 2.1.1](../standards/GlobalAzureDevOpsPipelineStandards.md#211-local-validation-entry-point-requirement) and must mirror the repository's dev-pipeline gates as closely as the local development environment allows.

---

## 8. Validation Pipeline
<!-- STD-MARKER: repository-onboarding.8 -->

Every repository must implement the repository-type validation pipeline required by [GlobalAzureDevOpsPipelineStandards.md Section 2.1](../standards/GlobalAzureDevOpsPipelineStandards.md#21-dev-environment) and the repository-type file layout defined in the pipeline quick reference.

The following must be true before the repository is used for governed development:

- The validation pipeline triggers on pull requests targeting `dev`.
- The validation pipeline builds the repository.
- The validation pipeline runs the required automated tests or repository-type validation checks.
- The validation pipeline produces the traceable promoted output when the repository type requires one.
- The validation pipeline is registered as the required branch policy on `dev`.

---

## 9. Promoted Output and Traceability
<!-- STD-MARKER: repository-onboarding.9 -->

Any repository that produces a deployable artifact, package, or release bundle must document the promoted output created during `feature/* -> dev` validation.

At minimum, the promoted output definition must map the released content to:

- the originating commit SHA
- the source pull request
- the validation build or run identifier

---

## 10. Release or Publication Pipeline
<!-- STD-MARKER: repository-onboarding.10 -->

Any repository that produces a deployable artifact, package, or release bundle must implement the governed release or publication path defined in [GlobalAzureDevOpsPipelineStandards.md Section 2.3](../standards/GlobalAzureDevOpsPipelineStandards.md#23-production-environment).

Repository-type-specific rules apply:

- Web API and Web App repositories must deploy through the required staging-validate-swap-validate sequence and must expose `/healthcheck` as required by [GlobalLoggingStandards.md Section 7](../standards/GlobalLoggingStandards.md#7-health-endpoints).
- Database repositories must follow the governed DACPAC delivery path.
- Class library repositories must publish the package through the governed library release path.

Any repository that requires an Azure DevOps service connection must use the currently approved shared service connection configuration defined in [GlobalAzureDevOpsPipelineStandards.md](../standards/GlobalAzureDevOpsPipelineStandards.md).

---

## 11. Repository-Local Deviations
<!-- STD-MARKER: repository-onboarding.11 -->

Any approved repository-specific deviation adopted at creation must be documented in a repository-local standards file under `Standards/` and linked from `.github/copilot-instructions.md`.

---

## 12. GlobalStandards ValidationEngine Wiring
<!-- STD-MARKER: repository-onboarding.12 -->

Every repository must be wired to the shared, compiled `ValidationEngine` (`GlobalStandards/ValidationEngine`) rather than reimplementing validation logic locally, following the setup procedure defined in [GlobalDeveloperOnboardingStandards.md](../standards/GlobalDeveloperOnboardingStandards.md) Section 3.5. The following outcomes must be verified at creation time:

- `GlobalStandards` is cloned as a sibling of the repository and the local pre-commit hook is installed and functional.
- The pipeline YAML checks out `GlobalStandards` and runs the shared standards-validation stage against the PR's merge-base diff.
- The validation pipeline's build identity holds the permissions required to post PR comments.
- `AppTypeDetector` classifies the repository correctly and the `CodeChanges` gate patterns match the repository's actual layout.

No engine code changes are required to bring a new repository under governance; the compiled `ValidationEngine` is solution-agnostic and driven entirely by `-RepositoryRoot`, git state, and the applicability matrix. This is a wiring and configuration exercise per repository, not new development.

---

## 13. Compliance Verification
<!-- STD-MARKER: repository-onboarding.13 -->

- [ ] `.github/copilot-instructions.md` exists and links first to the global governance baseline.
- [ ] A root-level `Standards/` folder exists for repository-local deviation files.
- [ ] `README.md` and `.gitignore` exist at the repository root.
- [ ] The repository uses `main` as the root branch and `dev` as the integration branch.
- [ ] Azure DevOps branch policies for `dev` and `main` match the governed repository standard unless an approved deviation replaces a default rule.
- [ ] The repository structure matches the governed solution structure standard, including a conforming test project with starter tests.
- [ ] Approved template assets are used for baseline files instead of ad hoc copies.
- [ ] The repository exposes a documented local validation script or equivalent command entry point.
- [ ] The validation pipeline exists, is registered on `dev`, and follows the governed repository-type pipeline pattern.
- [ ] Repositories that produce deployable artifacts, packages, or release bundles define a traceable promoted output.
- [ ] Repositories that produce deployable artifacts, packages, or release bundles implement the governed release or publication path.
- [ ] Web API and Web App repositories expose `/healthcheck`.
- [ ] Azure service connections created at repository creation use the approved shared configuration when the repository requires Azure DevOps environment access.
- [ ] The repository is cloned with `GlobalStandards` as a sibling directory and the local pre-commit hook wraps `RunValidationEngine.ps1 -Mode System`.
- [ ] The pipeline includes the `GlobalStandards` repository resource and the shared `change-detection.yml`/`standards-validation.yml` templates ahead of build/test/publish.
- [ ] The pipeline build identity holds `PullRequestContribute` on the repository.
- [ ] A Manual-mode ValidationEngine run confirms correct app-type detection and the resulting applicable standards set.
- [ ] Every approved repository-specific deviation is recorded under `Standards/` and linked from `.github/copilot-instructions.md`.

---

## 14. Governance
<!-- STD-MARKER: repository-onboarding.14 -->

This standard is owned by Troy Crowe. No changes to this file may be merged without Troy Crowe's explicit approval. Changes must be submitted as a pull request that includes a rationale comment explaining the reason for the update or deviation. Direct commits to `dev` or `main` are not permitted. Branch, PR, and approval rules are defined in [GlobalGovernanceStandards.md](../standards/GlobalGovernanceStandards.md).
