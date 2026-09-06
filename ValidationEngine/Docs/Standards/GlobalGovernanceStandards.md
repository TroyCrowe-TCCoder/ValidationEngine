# Global Governance Standards

**Version:** 2.5.0
**Status:** Active
**Applies To:** All repositories under the GlobalStandards governance model
**Audience:** AI models and human developers
**Created:** 2025-01-01
**Last Modified:** 2026-07-25

---
<!-- STD-MARKER: governance.file -->


## 1. Purpose
<!-- STD-MARKER: governance.1 -->

This standard must serve as the root governance standard for all repositories governed by GlobalStandards. It must define the root governance rules, must define rule precedence and the deviation process, and must point to the child standards that contain the more specific directives for repository rules, pipeline rules, onboarding, engineering practice, compliance, AI governance, and standards-file structure.

This standard must include a relevant governance section for every actual standards file in the standards library so the full standards library remains chained from the root governance document.

---

## 2. Rule Precedence
<!-- STD-MARKER: governance.2 -->

Rule precedence must be applied consistently across all governed repositories so that global governance remains authoritative unless an approved lower-level deviation overrides it. Detailed directives for repository-level rule application are defined in [`GlobalRepositoryStandards.md`](GlobalRepositoryStandards.md).

The following hierarchy governs all rule conflicts.

| Level | Source | Authority |
|---|---|---|
| 1 — Lowest Specificity | Global standards ([`Docs/Standards/`](../standards/GlobalFileSpecificationStandards.md)) | Baseline; applies universally unless overridden by an approved lower-level deviation |
| 2 | Repository-local entry point and linked local deviation files ([`.github/copilot-instructions.md`](../../.github/copilot-instructions.md), repository-local addendum files in `Standards/` as defined in [`GlobalFileSpecificationStandards.md` Section 2.15](GlobalFileSpecificationStandards.md#215-repository-local-addendum-definition-required)) | Overrides global where the deviation is explicitly documented and approved through [Section 7](#7-exception-process) |
| 3 | Pipeline and environment configuration | Overrides repository-level where the environment requires it and the deviation is approved |
| 4 — Highest Specificity | Inline pipeline steps | Overrides pipeline configuration where explicitly documented and approved |

---

## 3. Branching and Check-In Governance
<!-- STD-MARKER: governance.3 -->

Branching and check-in governance must enforce the approved branch model, pull-request-only promotion, and protected-branch controls across all governed repositories. Detailed directives are defined in [`GlobalRepositoryStandards.md`](GlobalRepositoryStandards.md).

The global governance baseline for every governed repository is:

- The standard root branch name is `main`.
- The integration branch is `dev`.
- Direct commits to protected branches are not permitted.
- Changes must promote through the approved pull request flow.

---

## 4. Pull Request and Merge Governance
<!-- STD-MARKER: governance.4 -->

Pull request and merge governance must enforce controlled promotion, reviewer controls, merge controls, and validation gates across all governed repositories. Detailed directives are defined in [`GlobalRepositoryStandards.md`](GlobalRepositoryStandards.md) and [`GlobalAzureDevOpsPipelineStandards.md`](GlobalAzureDevOpsPipelineStandards.md).

The root governance baseline for pull requests and merges is:

- Changes must be promoted through pull requests.
- Protected branch merges must satisfy required branch policies.
- Validation must complete successfully before merge is permitted.
- Approved repository-specific deviations may alter default reviewer or merge behavior only through the exception process defined in [Section 7](#7-exception-process).

---

## 5. Deployment Governance
<!-- STD-MARKER: governance.5 -->

Pipeline and deployment governance must enforce the approved validation, promotion, and deployment control model across all governed repositories. Detailed directives are defined in [`GlobalAzureDevOpsPipelineStandards.md`](GlobalAzureDevOpsPipelineStandards.md).

The root governance baseline for deployment is:

- Automated validation, promotion, and deployment controls must follow the approved pipeline model.
- Automated deployment controls must follow the approved promotion and deployment rules.
- Azure rollout automation artifacts must preserve a two-layer model consisting of raw source captures and rollout packages grouped by deployment unit.
- Deployment behavior must not bypass required governance controls.

---

## 6. Environment Governance
<!-- STD-MARKER: governance.6 -->

Environment governance must enforce controlled promotion boundaries, environment-specific validation, and approved environment controls across all governed repositories. Detailed directives are defined in [`GlobalAzureDevOpsPipelineStandards.md`](GlobalAzureDevOpsPipelineStandards.md) and [`GlobalRepositoryStandards.md`](GlobalRepositoryStandards.md).

The root governance baseline for environment control is:

- Environment promotion must apply the approved boundary controls.
- Promotion controls must be consistent with repository governance and pipeline governance.
- Repository-specific deviations affecting environment promotion must be documented and approved through the exception process defined in [Section 7](#7-exception-process).

---

## 7. Exception Process
<!-- STD-MARKER: governance.7 -->

Approved repository-specific deviations must be the only mechanism used to override a conflicting global governance rule for a specific repository. Repository-specific implementation is applied through the repository entry point file and the linked repository-local deviation files. A repository-local deviation record must be documented as a repository-local addendum file, structured per [`GlobalFileSpecificationStandards.md` Section 2.15](GlobalFileSpecificationStandards.md#215-repository-local-addendum-definition-required).

Both human developers and AI models must use this process for any deviation from global governance. A deviation that has not completed this process is not approved and must be treated as a violation.

Every approved deviation record must include:

- **Rule** — the specific rule being deviated from
- **Reason** — why the deviation is necessary
- **Requester** — who is requesting the deviation
- **Approver** — who has authorized the deviation
- **Effective date** — when the deviation takes effect
- **Planned review date** — when the deviation will be reviewed for removal or continuation

---

## 8. Repository Remediation
<!-- STD-MARKER: governance.8 -->

Repository remediation governance must require non-conforming repositories to complete the approved onboarding and remediation process before new development begins. Detailed directives are defined in [`GlobalRepositoryOnboardingStandards.md`](GlobalRepositoryOnboardingStandards.md).

The root governance baseline for remediation is:

- A repository that does not conform to the approved governance model must be remediated before new development begins.
- Remediation must follow the approved onboarding and repository-setup process.
- Remediation progress must be tracked in the repository's own onboarding or governance notes.

---

## 9. Standards Library Governance Chain
<!-- STD-MARKER: governance.9 -->

The standards library governance chain must link every actual standards file from the root governance standard so both human developers and AI models can traverse the full standards library from this document. Detailed directives remain in the linked child standards.

### 9.1 Repository Standards
<!-- STD-MARKER: governance.9.1 -->

Repository governance must define repository-specific branch flow, branch protection, pull request controls, merge behavior, branch-policy enforcement, and temporary-file handling. Detailed directives are defined in [`GlobalRepositoryStandards.md`](GlobalRepositoryStandards.md).

### 9.2 Repository Onboarding Standards
<!-- STD-MARKER: governance.9.2 -->

Repository onboarding governance must define the onboarding, remediation, entry-point setup, and repository-local deviation setup rules required before repository work begins. Detailed directives are defined in [`GlobalRepositoryOnboardingStandards.md`](GlobalRepositoryOnboardingStandards.md).

### 9.3 Developer Onboarding Standards
<!-- STD-MARKER: governance.9.3 -->

Developer onboarding governance must define the steps required for a developer to become ready to work within the governed standards, repository, and pull-request model. Detailed directives are defined in [`GlobalDeveloperOnboardingStandards.md`](GlobalDeveloperOnboardingStandards.md).

### 9.4 Azure DevOps Pipeline Standards
<!-- STD-MARKER: governance.9.4 -->

Pipeline governance must define validation, carry-forward pull request automation, deployment sequencing, environment controls, and pipeline implementation rules applied across governed repositories. Detailed directives are defined in [`GlobalAzureDevOpsPipelineStandards.md`](GlobalAzureDevOpsPipelineStandards.md).

### 9.5 Solution Structure Standards
<!-- STD-MARKER: governance.9.5 -->

Solution structure governance must define repository layout, project layout, standards-folder placement, structural entry-point rules, and default folder and file casing rules. Detailed directives are defined in [`GlobalSolutionStructureStandards.md`](GlobalSolutionStructureStandards.md).

### 9.6 Coding Standards
<!-- STD-MARKER: governance.9.6 -->

Coding governance must define the coding rules, implementation structure rules, and code-level quality rules applied across governed repositories. Detailed directives are defined in [`GlobalCodingStandards.md`](GlobalCodingStandards.md).

### 9.7 Security Standards
<!-- STD-MARKER: governance.9.7 -->

Security governance must define the authentication, authorization, secret-handling, isolation, and secure communication rules applied across governed repositories. Detailed directives are defined in [`GlobalSecurityStandards.md`](GlobalSecurityStandards.md).

### 9.8 Caching Standards
<!-- STD-MARKER: governance.9.8 -->

Caching governance must define the caching rules, cache-scope rules, and invalidation controls applied across governed repositories. Detailed directives are defined in [`GlobalCachingStandards.md`](GlobalCachingStandards.md).

### 9.9 Performance Standards
<!-- STD-MARKER: governance.9.9 -->

Performance governance must define the performance, efficiency, and measurement rules applied across governed repositories. Detailed directives are defined in [`GlobalPerformanceStandards.md`](GlobalPerformanceStandards.md).

### 9.10 Testing Standards
<!-- STD-MARKER: governance.9.10 -->

Testing governance must define the test structure, test coverage, and validation rules applied across governed repositories. Detailed directives are defined in [`GlobalTestingStandards.md`](GlobalTestingStandards.md).

### 9.11 Logging Standards
<!-- STD-MARKER: governance.9.11 -->

Logging governance must define the logging, telemetry, correlation, and audit-trace rules applied across governed repositories. Detailed directives are defined in [`GlobalLoggingStandards.md`](GlobalLoggingStandards.md).

### 9.12 Database Standards
<!-- STD-MARKER: governance.9.12 -->

Database governance must define schema, migration, query, and database-delivery rules applied to governed database repositories. Detailed directives are defined in [`GlobalDatabaseStandards.md`](GlobalDatabaseStandards.md).

### 9.13 NuGet Library Standards
<!-- STD-MARKER: governance.9.13 -->

Shared-library governance must define NuGet package authoring, versioning, publication, and consumption rules for governed library repositories and must define that class libraries do not require a production rollout. Detailed directives are defined in [`GlobalNuGetLibraryStandards.md`](GlobalNuGetLibraryStandards.md).

### 9.14 TypeScript Standards
<!-- STD-MARKER: governance.9.14 -->

TypeScript governance must define the TypeScript rules applied in governed repositories that use TypeScript. Detailed directives are defined in [`GlobalTypeScriptStandards.md`](GlobalTypeScriptStandards.md).

### 9.15 React Project Standards
<!-- STD-MARKER: governance.9.15 -->

React project governance must define the React-specific structure, component, and project rules applied in governed repositories that use React. Detailed directives are defined in [`GlobalReactProjectStandards.md`](GlobalReactProjectStandards.md).

### 9.16 Agent Standards
<!-- STD-MARKER: governance.9.16 -->

Agent governance must define the standards that govern custom AI agent structure, tool use, and execution behavior. Detailed directives are defined in [`GlobalAgentStandards.md`](GlobalAgentStandards.md).

### 9.17 Agent Team Standards
<!-- STD-MARKER: governance.9.17 -->

Agent team governance must define the standards that govern multi-agent roles, collaboration, and handoff behavior. Detailed directives are defined in [`GlobalAgentTeamStandards.md`](GlobalAgentTeamStandards.md).

### 9.18 Standards File Specification
<!-- STD-MARKER: governance.9.18 -->

Standards-file governance must define the required structure, naming, linking, and compliance-section rules applied to all standards files in this library. Detailed directives are defined in [`GlobalFileSpecificationStandards.md`](GlobalFileSpecificationStandards.md).

### 9.19 Feature Management Standards
<!-- STD-MARKER: governance.9.19 -->

Detailed directives are defined in `GlobalFeatureManagementStandards.md` (pending; tracked as backlog item STD-001 in `Working/ProjectBacklog.md`).

### 9.20 Documentation Governance
<!-- STD-MARKER: governance.9.20 -->

Documentation governance must define the planning-document and README documentation rules applied to new tools, features, and automation built across governed repositories. Detailed directives are defined in [Section 10](#10-documentation-governance) of this standard.

---

## 10. Documentation Governance
<!-- STD-MARKER: governance.10 -->

Documentation governance must require that new tools, features, and automation built for the organization are captured as organizational knowledge so future expansion and onboarding does not depend on tribal memory. Detailed directives are defined in [`GlobalFileSpecificationStandards.md`](GlobalFileSpecificationStandards.md).

The root governance baseline for documentation is:

- Every new tool, feature, or automation component must have both of the following, placed alongside the component it describes:
  - A **`<ToolName>.Planning.md`** file — a historical record of why the component was built the way it was and the steps taken to reach the end result (design discussion, alternatives considered, decisions made). This file captures history and is not expected to be rewritten after the fact.
  - A standard **`README.md`** — the current state and purpose of the component (the end result): what it is, what it does, and how it is used, kept up to date as the component evolves.
- A component whose behavior has materially changed without a corresponding `README.md` update is not considered complete.
- Repository-specific documentation conventions (e.g. file naming or folder placement for the planning document) may be defined through the exception process in [Section 7](#7-exception-process) where the default placement does not fit a repository's structure.

---

## 11. Compliance Verification
<!-- STD-MARKER: governance.11 -->

Governance verification must confirm that every governed standards file provides a checklist usable for governance checks by both human reviewers and agent-driven pre-check gates. Detailed directives are defined in the compliance sections of the child standards.

Use the following root-governance checklist before marking governance work complete:

- [x] Org-level default branch is set to `main` in Azure DevOps Organization Settings.
- [ ] Rule precedence is defined and applied consistently.
- [ ] Approved repository-specific deviations follow the exception process in this file.
- [ ] Each applicable child governance standard is linked from this root governance standard.
- [ ] Each applicable child standard provides a compliance checklist used for governance checks.
- [ ] Repository governance, pipeline governance, and onboarding/remediation governance are enforced through their child standards.
- [ ] Azure rollout work preserves raw source captures separately from rollout packages grouped by deployment unit.
- [ ] New tools, features, and automation built for the organization include a companion planning document and an up-to-date README per [Section 10](#10-documentation-governance).

---

## 12. Governance
<!-- STD-MARKER: governance.12 -->

This standard is owned by Troy Crowe. No changes to this file may be merged without Troy Crowe's explicit approval. Changes must be submitted as a pull request that includes a rationale comment explaining the reason for the update or deviation. Direct commits to `dev` or `main` are not permitted. Branch, PR, and approval rules are defined in this document; all standards files are governed by [`GlobalFileSpecificationStandards.md`](../standards/GlobalFileSpecificationStandards.md).
