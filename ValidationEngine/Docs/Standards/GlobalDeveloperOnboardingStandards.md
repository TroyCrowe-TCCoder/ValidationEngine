# Developer Onboarding Standards

**Version:** 1.5.0
**Status:** Active
**Applies To:** All repositories and solutions under the GlobalStandards governance model
**Audience:** AI models and human developers
**Created:** 2026-07-21
**Last Modified:** 2026-07-23

---
<!-- STD-MARKER: developer-onboarding.file -->


## 1. Purpose
<!-- STD-MARKER: developer-onboarding.1 -->

This document defines the required steps for onboarding a developer to work within the GlobalStandards governance model. Every developer who will contribute to a governed repository must complete this onboarding baseline before making changes, opening pull requests, or approving work.

Developer onboarding is distinct from repository onboarding. Repository onboarding establishes repository compliance and tooling. Developer onboarding establishes that the developer can work correctly within the existing standards, governance, branching, and validation model.

Developer onboarding must reduce the learning curve of a new developer by explicitly walking that developer from repository acquisition, to standards discovery, to local build and validation capability, to governed pull-request execution.

---

## 2. Onboarding Sequence
<!-- STD-MARKER: developer-onboarding.2 -->

Developer onboarding must be completed in the following order. Do not skip steps and return later. Later steps depend on earlier steps being complete.

| Order | Step | Goal |
|---|---|---|
| 1 | Clone the GlobalStandards repository | Ensure the developer starts by cloning the canonical standards repository locally before working in any governed repository. |
| 2 | Enable local standards validation enforcement | Ensure the developer has the required local standards-validation process in place so check-in is blocked until compliance passes. |
| 3 | Configure development tools | Ensure the developer has the required local tooling and settings. |
| 4 | Learn the standards and system rules | Ensure the developer understands the standards set, the system structure, and the repository-specific rules that govern implementation. |
| 5 | Learn the governance workflow | Ensure the developer understands the branch, PR, approval, and validation flow. |
| 6 | Validate working readiness | Ensure the developer can clone, build, test, validate, and work within the governed model. |

---

## 3. Step 1 — Clone the GlobalStandards Repository
<!-- STD-MARKER: developer-onboarding.3 -->

### 3.1 Clone the GlobalStandards Repository First
<!-- STD-MARKER: developer-onboarding.3.1 -->

Every developer must clone the GlobalStandards repository before cloning or modifying any governed application repository.

Step 1 begins only when the developer has created a local clone of the `GlobalStandards` repository in their working environment.

The local copy of GlobalStandards is the canonical working reference for:

- the root governance baseline
- the global engineering standards
- the repository and pipeline standards
- all other domain standards used by governed repositories

A developer must not begin work in a governed repository until the GlobalStandards repository is available locally.

### 3.2 Keep the Standards Repository Available During Work
<!-- STD-MARKER: developer-onboarding.3.2 -->

The local GlobalStandards repository must remain available while the developer is working in governed repositories. Do not treat the standards set as a one-time read. The standards repository is an active reference used during implementation, review, and validation.

### 3.3 Read the Repository Entry Point Before Work Begins
<!-- STD-MARKER: developer-onboarding.3.3 -->

Before the first work session in any governed repository, the developer must read that repository's `.github/copilot-instructions.md` file. That file defines the baseline entry into the standards chain and any approved repository-local deviations.

### 3.4 Acquire the Working Repository Set
<!-- STD-MARKER: developer-onboarding.3.4 -->

Before implementation work begins, the developer must clone or otherwise acquire local access to the governed repositories required for the assigned work.

At minimum, the developer must know:

- which repository contains the code to be changed
- which repository contains the governing standards baseline
- which sibling repositories are dependencies or related systems for the work area

### 3.5 Required Sibling Clone Layout
<!-- STD-MARKER: developer-onboarding.3.5 -->

Every governed repository, including `GlobalStandards`, must be cloned as a sibling directory under one common local root directory. No governed repository may be cloned inside another governed repository's directory tree.

```
<CommonRoot>/
├── GlobalStandards/
├── CaptiveExpensesApi/
├── CaptiveExpensesDB/
├── HttpClientManager/
└── ...
```

This layout is required so that local tooling in any governed repository — including local standards-validation enforcement — can locate the canonical `GlobalStandards` repository using a fixed relative path (`../GlobalStandards`) regardless of which governed repository the tooling is invoked from. A developer whose local clone layout does not conform to this structure must correct it before local validation enforcement in [Section 4](#4-step-2--enable-local-standards-validation-enforcement) can function correctly.

---

## 4. Step 2 — Enable Local Standards Validation Enforcement
<!-- STD-MARKER: developer-onboarding.4 -->

---

### 4.1 Required Local Validation Enforcement
<!-- STD-MARKER: developer-onboarding.4.1 -->

After cloning `GlobalStandards`, every developer environment must have the required local standards-validation process installed, configured, or otherwise enabled before implementation work begins.

The governed model requires PowerShell-based validation automation that runs as part of local check-in or equivalent local commit workflow.

Step 2 is not complete until that local validation enforcement is available and active in the developer's environment.

### 4.2 Required Enforcement Behavior
<!-- STD-MARKER: developer-onboarding.4.2 -->

The local validation process must execute standards-compliance checks before a local check-in is allowed to complete.

Local check-in must be blocked when required standards validations fail.

Pipeline validation is required, but it must not be the first point where standards non-compliance is detected when local enforcement can prevent the check-in.

At minimum, the local validation enforcement model must support:

- standards compliance validation
- pre-check execution during local check-in
- local verification before PR creation

### 4.3 Current-State and Target-State Expectation
<!-- STD-MARKER: developer-onboarding.4.3 -->

If the required PowerShell validation scripts are not yet implemented in a repository or in the standards baseline, that gap must be treated as missing governed capability rather than as permission to skip the requirement.

Developer onboarding must teach the required target state even when implementation work remains to finish the automation.

### 4.4 Developer Responsibility
<!-- STD-MARKER: developer-onboarding.4.4 -->

Developers must not skip local validation enforcement, defer it until after coding begins, or rely only on pipeline validation to identify standards violations.

If the required validation automation is present but fails to install, configure, or run correctly, the developer must resolve the issue before onboarding continues.

### 4.5 Required PowerShell Module Dependencies
<!-- STD-MARKER: developer-onboarding.4.5 -->

The local standards-validation engine and its test suite require the Pester PowerShell module at version 5.0 or later. Windows PowerShell ships with an outdated built-in Pester (3.x), which is not compatible with the validation engine's tests and must not be relied upon.

Before Step 2 is considered complete, the developer must install or update Pester to at least version 5.0 for the current user:

```powershell
Install-Module Pester -MinimumVersion 5.0 -Scope CurrentUser -Force -SkipPublisherCheck
```

The developer must confirm a compatible version is available by running:

```powershell
Get-Module -ListAvailable Pester | Select-Object Name, Version
```

If only the built-in 3.x module is present, or if `Import-Module Pester -MinimumVersion 5.0` fails, the developer must resolve this before proceeding. This step must be repeated whenever the governed validation engine's test suite reports a Pester version incompatibility.

### 4.6 Validation Enforcement as a Prerequisite for Work
<!-- STD-MARKER: developer-onboarding.4.6 -->

A developer is not ready to begin implementation work until the required local standards-validation enforcement is available and capable of preventing check-in when standards compliance fails.

---

## 5. Step 3 — Configure Development Tools
<!-- STD-MARKER: developer-onboarding.5 -->

### 5.1 Required Tooling
<!-- STD-MARKER: developer-onboarding.5.1 -->

Every developer must have the tools required by the target repository type installed and functioning before work begins.

At minimum, this includes:

- Git
- the repository's required .NET SDK or other declared language/runtime SDK
- the required IDE or editor tooling for the repository type
- access to the organization's package sources and feeds

### 5.2 Interactive Access Authentication
<!-- STD-MARKER: developer-onboarding.5.2 -->

Every developer who requires interactive access to Azure or Azure DevOps during governed work must authenticate using the documented device login flow before work begins.

Developer onboarding must include the device login flow used by the governed environment and must confirm that the developer can complete that flow successfully.

### 5.3 Repository and Package Feed Access
<!-- STD-MARKER: developer-onboarding.5.3 -->

Before taking a work item, the developer must confirm they can access:

- the required Azure DevOps organization and project
- the governed repositories in scope
- the package sources and internal feeds required by the target repositories

If a repository restores from Azure Artifacts or another governed package source, the developer must confirm package restore succeeds from that source before onboarding is considered complete.

### 5.4 Repository Build and Test Capability
<!-- STD-MARKER: developer-onboarding.5.4 -->

Before taking a work item, the developer must confirm they can restore packages, build the solution, and run the applicable tests locally for at least one governed repository in their scope.

A developer who cannot build and test locally is not ready to begin implementation work.

### 5.5 Local Validation Entry Point Capability
<!-- STD-MARKER: developer-onboarding.5.5 -->

Before taking a work item, the developer must know how to run the repository's documented local validation script or equivalent command entry point.

The local validation entry point must execute the repository's required pre-PR gates, which must include the applicable restore, build, test, and validation checks for that repository type.

A developer who cannot run the local validation entry point successfully is not ready to begin implementation work.

### 5.6 Standards-Aware Workspace Setup
<!-- STD-MARKER: developer-onboarding.5.6 -->

The developer's workspace must make the standards repository and the governed repository both accessible during the work session. Do not work from an environment where the standards cannot be reviewed while changes are being made.

---

## 6. Step 4 — Learn the Standards and System Rules
<!-- STD-MARKER: developer-onboarding.6 -->

### 6.1 Read the Applicable Standards Set
<!-- STD-MARKER: developer-onboarding.6.1 -->

Before beginning implementation work, the developer must read the standards that govern the assigned repository type and work area.

At minimum, the developer must understand:

- [`GlobalGovernanceStandards.md`](../standards/GlobalGovernanceStandards.md)
- [`GlobalRepositoryStandards.md`](../standards/GlobalRepositoryStandards.md)
- [`GlobalAzureDevOpsPipelineStandards.md`](../standards/GlobalAzureDevOpsPipelineStandards.md)
- [`GlobalSolutionStructureStandards.md`](../standards/GlobalSolutionStructureStandards.md)
- [`GlobalCodingStandards.md`](../standards/GlobalCodingStandards.md)
- any repository-type-specific standards that apply to the target system

### 6.2 Understand the Repository-Specific Rule Chain
<!-- STD-MARKER: developer-onboarding.6.2 -->

The developer must understand how the target repository enters the standards chain through `.github/copilot-instructions.md` and how repository-local deviation files under `Standards/` alter the baseline for that repository.

### 6.3 Understand System Structure and Responsibilities
<!-- STD-MARKER: developer-onboarding.6.3 -->

Before taking work in a repository, the developer must understand the solution and project layout, the major folders used by the system, and the role of the repository within the larger governed environment.

At minimum, the developer must be able to identify:

- the primary project or projects in the solution
- the test project
- the location of the local validation entry point
- the pipeline files or pipeline entry points used by the repository
- any shared libraries, databases, or related repositories that the repository depends on

### 6.4 Learn the Running System or Repository-Type Execution Path
<!-- STD-MARKER: developer-onboarding.6.4 -->

Before taking work in a repository, the developer must complete the applicable repository-type readiness action:

- For a hosted Web API or Web App repository, run the application locally and exercise the basic startup path and at least one representative workflow. If the repository exposes `/healthcheck`, confirm that endpoint responds successfully.
- For a database repository, identify the project entry point, the validation entry point, and the deployment verification path used by the governed pipeline.
- For a shared library repository, build the library, run its tests, and identify the package project, package identity, and the public registration or entry point used by consuming repositories.
- For a documentation-only repository, run the documented local validation entry point and identify the active workflow files that govern validation and promotion.

Developer onboarding is not complete until the developer can explain how the assigned repository is executed, validated, or consumed in practice.

### 6.5 Understand the Work-Slice Standards Responsibility
<!-- STD-MARKER: developer-onboarding.6.5 -->

The developer must identify the standards that apply to the assigned work slice before making changes. Standards discovery after implementation begins is not permitted as the normal work pattern.

---

## 7. Step 5 — Learn the Governance Workflow
<!-- STD-MARKER: developer-onboarding.7 -->

### 7.1 Branch Flow
<!-- STD-MARKER: developer-onboarding.7.1 -->

Every developer must understand and follow the governed branch flow:

```text
feature/* → dev → main
```

Direct commits to protected branches are not permitted.

### 7.2 Pull Request Workflow
<!-- STD-MARKER: developer-onboarding.7.2 -->

Every developer must understand that changes are promoted through pull requests, that branch policies must pass before merge, and that merges to protected branches must follow the approved repository governance model.

Every developer must understand the workflow split:

- `feature/* → dev` validates the change and produces the traceable output to be promoted later when the repository type requires one
- `dev → main` acts as the approval and promotion record
- for repository types that require environment-bound delivery, the delivery pipeline performs deployment, deployment validation, and rollback

### 7.3 Validation Responsibility
<!-- STD-MARKER: developer-onboarding.7.3 -->

Every developer is responsible for ensuring that local build, test, and standards checks are complete before creating or updating a pull request. Pipeline validation confirms compliance; it does not replace the developer's responsibility to validate their own changes first.

### 7.4 Local Gate Script Responsibility
<!-- STD-MARKER: developer-onboarding.7.4 -->

Every developer must run the repository's documented local validation script or equivalent command entry point before creating a pull request and before pushing updates intended to satisfy pull-request feedback or validation failures.

Skipping the local validation entry point and relying on the pipeline as the first check is not permitted.

### 7.5 Standards Usage Responsibility
<!-- STD-MARKER: developer-onboarding.7.5 -->

Every developer must know which standards apply to their work slice before beginning implementation. Do not start code changes and look up the standards afterward. The standards must guide the work, not merely audit it after the fact.

### 7.6 Promotion Responsibility
<!-- STD-MARKER: developer-onboarding.7.6 -->

Every developer must understand that promotion is based on the already-validated change set. For repositories that produce a deployable artifact, package, or release bundle, the developer must preserve traceability from the originating feature work through the promoted output and the `dev → main` promotion record.

For shared library repositories, the promoted output is the governed NuGet package artifact used by consuming repositories rather than a production rollout deployment.

---

## 8. Step 6 — Validate Working Readiness
<!-- STD-MARKER: developer-onboarding.8 -->

### 8.1 Clone, Build, and Test a Governed Repository
<!-- STD-MARKER: developer-onboarding.8.1 -->

As part of onboarding, every developer must successfully:

1. clone a governed repository
2. restore dependencies
3. build the repository
4. run the applicable tests
5. run the repository's documented local validation script or equivalent command entry point
6. review the repository entry point and locate any approved local deviations
7. complete the applicable repository-type readiness action defined in [Section 6.4](#64-learn-the-running-system-or-repository-type-execution-path)

### 8.2 Create and Push a Feature Branch
<!-- STD-MARKER: developer-onboarding.8.2 -->

Before being considered fully onboarded, the developer must demonstrate they can create and push a feature branch that follows repository branch naming rules.

### 8.3 Create a Pull Request or Equivalent Trial Run
<!-- STD-MARKER: developer-onboarding.8.3 -->

The onboarding process must include either:

- creation of a real pull request for an approved onboarding change, or
- a documented walkthrough of the repository's pull request creation and validation flow

A developer must understand how the PR title, description, auto-complete behavior, review flow, and validation behavior work in the governed repositories they will use.

---

## 9. Expectations for AI-Assisted Development
<!-- STD-MARKER: developer-onboarding.9 -->

When a developer uses AI assistance in a governed repository:

- the developer must ensure the repository entry point file is read first
- the developer must verify that AI-generated output complies with the applicable standards before accepting it
- the developer must not allow AI-generated changes to bypass branch, PR, review, or validation controls
- the developer remains accountable for all accepted changes, regardless of whether the content was written by a human or an AI model

AI assistance changes the implementation method. It does not change the developer's accountability, review responsibilities, or standards obligations.

---

## 10. Compliance Verification
<!-- STD-MARKER: developer-onboarding.10 -->

- [ ] The developer cloned the GlobalStandards repository before beginning work in governed repositories.
- [ ] The developer's local clone layout places every governed repository as a sibling directory under one common root, with no governed repository nested inside another.
- [ ] The developer can access the required Azure DevOps organization, project, and repositories.
- [ ] The developer can fetch, pull, push a feature branch, and create a pull request.
- [ ] The developer can complete the documented device login flow required for interactive Azure and Azure DevOps access.
- [ ] The developer has the required local SDKs, IDE/editor support, and package-feed access installed and working.
- [ ] The developer can access the required internal package sources and restore packages from them when the target repository depends on them.
- [ ] The developer can restore, build, and test at least one governed repository in scope.
- [ ] The developer can run the repository's documented local validation script or equivalent command entry point.
- [ ] The developer has installed or updated the Pester PowerShell module to version 5.0 or later for the current user.
- [ ] The developer understands the `feature/* → dev → main` branch flow.
- [ ] The developer understands that direct commits to protected branches are not permitted.
- [ ] The developer reviewed the target repository's `.github/copilot-instructions.md` file before beginning work.
- [ ] The developer acquired local access to the repositories needed for the assigned work area.
- [ ] The developer understands which standards files govern the target repository and work area.
- [ ] The developer understands the target system structure, related repositories, and dependency boundaries for the assigned work area.
- [ ] The developer completed the applicable repository-type readiness action and can explain how the repository is run, validated, deployed, or consumed.
- [ ] The developer can identify the applicable standards for their work slice before implementation begins.
- [ ] The developer understands that local validation is required before pull request creation or update.
- [ ] The developer understands that the local validation script or equivalent command entry point must be run before pull request creation and update.
- [ ] The developer can create and push a conforming feature branch.
- [ ] The developer understands the repository pull request and validation flow.
- [ ] The developer understands that `feature/* → dev` owns validation, `dev → main` owns promotion and approval recording, and repository-type-specific delivery or package-consumption behavior follows from that promotion record.
- [ ] The developer understands the traceability expectation for promoted artifacts, packages, or release bundles when the repository type produces them.
- [ ] The developer understands that AI-assisted changes remain the developer's responsibility.

---

## 11. Governance
<!-- STD-MARKER: developer-onboarding.11 -->

This standard is owned by Troy Crowe. No changes to this file may be merged without Troy Crowe's explicit approval. Changes must be submitted as a pull request that includes a rationale comment explaining the reason for the update or deviation. Direct commits to `dev` or `main` are not permitted. Branch, PR, and approval rules are defined in [`GlobalGovernanceStandards.md`](../standards/GlobalGovernanceStandards.md).
