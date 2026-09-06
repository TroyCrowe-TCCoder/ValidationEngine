# Standards File Specification

**Version:** 1.7.0  
**Status:** Active  
**Applies To:** All files in `Docs/Standards/` in the GlobalStandards repository  
**Audience:** AI models and human developers authoring or reviewing standards files  

---
<!-- STD-MARKER: file-specification.file -->


## 1. Purpose
<!-- STD-MARKER: file-specification.1 -->

This document defines the authoring specification for all standards files in the GlobalStandards repository. Every standards file must conform to this specification before it is committed. This document is the first reference consulted when creating, reviewing, or modifying any standards file.

The standards library is designed to be navigable by both human developers and AI models. Cross-file linking is a first-class design goal, not a convenience. Every standards file must link to the files it depends on and be linked from the files that depend on it. This allows a model or developer to traverse the full context graph by following links rather than searching, guessing at file names, or loading the entire library. A well-linked standards library makes AI assistance faster, more accurate, and less dependent on broad context loading.

---

## 2. Core Requirements
<!-- STD-MARKER: file-specification.2 -->

Every standards file must satisfy all of the following requirements without exception.

### 2.1 Directives, Not Options
<!-- STD-MARKER: file-specification.2.1 -->

Every rule must be written as a directive. Standards files state what must be done. They do not present choices, preferences, or suggestions.

Must not use: "should", "consider", "it is recommended", "you may", "optionally"  
Must use: "must", "must not", "is required", "is not permitted"

### 2.2 Single Responsibility
<!-- STD-MARKER: file-specification.2.2 -->

Each file owns exactly one domain. A domain is a distinct, named area of concern such as solution structure, branching governance, coding standards, or testing standards. No rule may appear in more than one file. If a rule is referenced across files it must live in one file and be linked from the others.

### 2.3 Verifiable
<!-- STD-MARKER: file-specification.2.3 -->

Every rule must be verifiable by inspection, tooling, or a defined manual check. A rule that cannot be checked for compliance must not be included. If the verification method is not obvious it must be stated alongside the rule.

### 2.4 Grounded in Real Examples
<!-- STD-MARKER: file-specification.2.4 -->

All examples must reference actual repository names, project names, folder names, or code patterns from this codebase. Abstract placeholder names such as `MyProject`, `SomeService`, or `YourRepository` must not be used. Use real names from the active repository list.

### 2.5 No Rationale or History
<!-- STD-MARKER: file-specification.2.5 -->

Standards files state what must be done. They do not explain why a decision was made, what was previously in place, or what alternatives were considered. Rationale belongs in PR descriptions and commit messages, not in the standard itself.

### 2.6 Audience Complete
<!-- STD-MARKER: file-specification.2.6 -->

Every rule must be actionable by both a human developer and an AI model without requiring additional context outside the standards files. If a rule requires knowledge of a tool, process, or configuration, that knowledge must be included in the rule or linked to another standards file that defines it.

### 2.7 No Speculative Content
<!-- STD-MARKER: file-specification.2.7 -->

Future state plans, aspirational goals, and notes about features not yet implemented must not appear in a standards file. Content that cannot be acted on today must not be included. If future work needs to be tracked it belongs in a backlog or a repository-level note, not in a global standard.

### 2.8 Versioned with Status
<!-- STD-MARKER: file-specification.2.8 -->

Every standards file must declare a version number and a status in its header using the following format:

```
**Version:** MAJOR.MINOR.PATCH
**Status:** Draft | Active | Superseded
```

- **Draft** — under review; must not be enforced until promoted to Active
- **Active** — approved and enforceable
- **Superseded** — replaced by a newer file; retained for reference only

Version increments follow semantic versioning:
- `PATCH` — clarification or wording correction with no change to the rule
- `MINOR` — new rule added or existing rule expanded; fully backwards compatible
- `MAJOR` — existing rule changed or removed in a way that requires existing work to be updated

### 2.9 Owned and Governed
<!-- STD-MARKER: file-specification.2.9 -->

Every standards file must end with a governance section that identifies the owner and the process for making changes. The standard footer is:

```
## [N]. Governance

Branch, PR, and approval rules must be defined by the root governance standard.
```

### 2.10 Compliance Section Required
<!-- STD-MARKER: file-specification.2.10 -->

Every standards file must include a compliance verification section immediately before the governance section. The compliance section must contain a checklist of items that can be verified against a repository or codebase. Each item must be a hard requirement. The heading must be:

```
## [N]. Compliance Verification
```

Every checklist item is the authoritative pointer to exactly one rule and must carry its own rule-item marker as defined in [Section 2.14](#214-exclusion-marker-system-required). A checklist item is not a restatement of the rule; it is the index entry that identifies which rule must be verified. The rule text itself lives once, in the section that defines it, and is read from that section at validation time.

Every rule-bearing section or sub-section must have at least one corresponding checklist item, and every checklist item must reference an existing section. A section with no checklist item, or a checklist item whose marker does not resolve to an existing section or sub-section, is non-compliant.

### 2.11 Reference Linking Required
<!-- STD-MARKER: file-specification.2.11 -->

Three linking types are defined for use across all standards files. Each serves a distinct purpose and must be applied consistently.

**Type 1 — Reference Catalog Links**
When a rule is supported by an external reference, a research post, or a documented decision, that rule must include an inline link to the corresponding entry in [`Docs/References/StandardsReferences.md`](../references/StandardsReferences.md). The reference entry must in turn include a `Referenced by:` link back to the standard and section. This linking must be bidirectional. A rule that has a known reference but omits the link is non-compliant. The catalog outline at the top of [`Docs/References/StandardsReferences.md`](../references/StandardsReferences.md) must be updated with each new entry so the file remains navigable by both human developers and AI models.

**Type 2 — Page-to-Page and Section Links**
When a rule in a standards file defers to another standards file for detail, it must include a direct relative markdown link to that file and the specific section within it using a `#section-anchor` fragment. Links must be created even if the target file does not yet exist. A broken link to a not-yet-created file is expected and acceptable during active standards development. A missing link to an existing file is not acceptable.

**Type 3 — In-Page Section Links**
When a rule references another section within the same file, it must use an in-page anchor link in the format `[Section N](#n-section-heading)`. Plain text references such as "defined in Section 3" or "per Section 12" are not acceptable when the target section exists in the same file.

All three linking types serve the same design goal: allowing both human developers and AI models to traverse the full context graph by following links rather than searching or loading the entire library.

### 2.12 Governance Chain Required
<!-- STD-MARKER: file-specification.2.12 -->

Every actual standards file must be included in the governance chain rooted at the designated root governance standard file. The root governance standard must include a relevant section for each actual standards file and must link directly to that file.

Guides, templates, rollout aids, working documents, and other non-standards artifacts must not be treated as governance-chain files.

### 2.13 Naming Convention Required
<!-- STD-MARKER: file-specification.2.13 -->

Every global standards file must use the `Global` prefix. Every standards file must use the `Standards` suffix. Repository-local deviation files must use the matching base name without the `Global` prefix.

Correct global standards file names:

- `GlobalFileSpecificationStandards.md`
- `GlobalRepositoryStandards.md`

Correct repository-local deviation file names:

- `RepositoryStandards.md`
- `GovernanceStandards.md`

Incorrect file names:

- `engineering-governance.md`
- `standards-file-specification.md`
- `SolutionStructure.md`
- `global-repository-standards.md`

### 2.14 Exclusion Marker System Required
<!-- STD-MARKER: file-specification.2.14 -->

Every rule-bearing section, sub-section, and Compliance Verification checklist item within a standards file must carry an exclusion marker so that individual repositories can declare, in a machine-readable way, which rules do not apply to them, and so that automated validation can locate the exact rule text associated with each checklist item. Markers exist to support a repository-local exclusion list and rule-driven validation; they do not by themselves grant an exclusion.

Three marker tiers are defined, from broadest to narrowest scope:

| Tier | Syntax | Scope | Placement |
|---|---|---|---|
| File | `{file-base-name}.file` | Excludes or references the entire standards file | Once, directly beneath the file's top-level header block |
| Section | `{file-base-name}.{section-number}` | Excludes or references an entire numbered section or sub-section | Immediately following the section or sub-section heading it identifies |
| Rule-item | `{file-base-name}.{section-number}.{item-number}` | Excludes or references one specific checklist item / individually verifiable rule statement | Immediately following the checklist bullet it identifies, on the same line or the line directly beneath it |

- `{file-base-name}` is the standards file name without the `Global` prefix, the `Standards` suffix, and the `.md` extension, converted to lowercase-hyphenated form (for example, `GlobalSolutionStructureStandards.md` becomes `solution-structure`).
- `{section-number}` is the exact numbered heading the marker follows (for example, `2.1`).
- `{item-number}` is the 1-based ordinal position of the checklist item within its Compliance Verification section (for example, the third bullet under Section 14 is `14.3`).
- A marker referencing an entire file rather than a section must use the literal segment `file` in place of a section number, in the form `{file-base-name}.file`.

Marker IDs must be updated in the same change that renumbers or renames a section, or that reorders a checklist. A marker ID that does not resolve to an existing section, sub-section, or checklist item is non-compliant.

A repository excludes a rule by listing the marker ID, at the appropriate tier, in its own repository-local addendum under `Standards/` as defined in [Section 2.15](#215-repository-local-addendum-definition-required), not by deleting or altering the marker in the GlobalStandards file. Excluding a whole-file marker excludes every section and rule-item marker within that file. Excluding a section marker excludes every rule-item marker within that section. A rule-item marker exclusion is scoped to that single checklist item only.

### 2.15 Repository-Local Addendum Definition Required
<!-- STD-MARKER: file-specification.2.15 -->

A repository-local addendum is a file that documents one of exactly three kinds of repository-specific content:

1. **A deviation** — an approved, repository-specific modification or narrowing of an existing global standard rule.
2. **An addition** — a rule that governs this repository only and has no corresponding global rule, used only when the rule would not be appropriate as a global rule for all repositories of the same application type (for example, all APIs or all Web Apps). If a rule would reasonably apply across multiple repositories of the same application type, it must be proposed and added as a global rule instead of recorded as a repository-local addition.
3. **A skip/exclusion** — an approved, time-bounded exemption from one or more specific rules, recorded at the file, section, or rule-item marker tier, for a repository where the rule does not apply or cannot yet be met.

An addendum does not restate or copy the global standard; a deviation records only the specific rule being overridden, the deviation itself, and the approval record for that deviation. An addition records only the repository-specific rule itself and its approval record. A skip/exclusion records only the marker ID(s) being excluded and its approval record.

Every skip/exclusion entry must contain:

- **Marker ID** — the exact file, section, or rule-item marker being excluded.
- **Reason** — the justification for the exclusion.
- **Requester** and **Approver** — the individuals who requested and approved the exclusion. The Approver must be Troy Crowe.
- **Effective Date** and **Planned Review Date** — when the exclusion took effect and when it must be reviewed. A skip/exclusion entry past its Planned Review Date without renewal is non-compliant and must be treated as an active violation.

A skip/exclusion entry must use the following format:

```
### Skip: {Marker ID}

**Marker ID:** {file-base-name}.{section-number}[.{item-number}]
**Reason:** {justification for the exclusion}
**Requester:** {name}
**Approver:** Troy Crowe
**Effective Date:** {YYYY-MM-DD}
**Planned Review Date:** {YYYY-MM-DD}
```

For example, a repository-local exclusion of the React component-casing checklist item in a repository that contains no React code:

```
### Skip: solution-structure.14.21

**Marker ID:** solution-structure.14.21
**Reason:** Repository contains no React application; component casing rule does not apply.
**Requester:** Troy Crowe
**Approver:** Troy Crowe
**Effective Date:** 2026-01-01
**Planned Review Date:** 2027-01-01
```


Every addendum file must reside in the target repository's root-level `Standards/` folder and must use the matching base file name without the `Global` prefix, as defined in [2.13 Naming Convention Required](#213-naming-convention-required) (for example, `GlobalRepositoryStandards.md` -> `Standards/RepositoryStandards.md`). The `Standards/` folder name and every addendum file name follow the PascalCase default defined in [`GlobalSolutionStructureStandards.md` Section 3.6](GlobalSolutionStructureStandards.md#36-default-folder-and-file-casing).

Every addendum file must contain, for each documented deviation:

- **Overridden Rule** — a direct link to the specific global standard file and section being overridden.
- **Deviation** — the repository-specific rule that replaces or narrows the overridden rule for that repository only.
- **Requirement** — one or more directive statements describing what the repository must do under the deviation.
- **Reason** — the justification for the deviation.
- **Requester** and **Approver** — the individuals who requested and approved the deviation.
- **Effective Date** and **Planned Review Date** — when the deviation took effect and when it must be reviewed.

Every addendum file must contain, for each documented addition:

- **Rule** — the repository-specific rule, stated as a directive.
- **Scope Justification** — a statement of why this rule does not qualify as a global rule for all repositories of the same application type.
- **Requirement** — one or more directive statements describing what the repository must do under the addition.
- **Reason** — the justification for the addition.
- **Requester** and **Approver** — the individuals who requested and approved the addition.
- **Effective Date** and **Planned Review Date** — when the addition took effect and when it must be reviewed.

An addendum file must not contain full copies or restatements of a global standard, rules that do not vary from the global standard, implementation conventions with no deviation or repository-specific rule behind them, or notes, scratch content, or session state; that content belongs in the repository's working-documents area or project-level README files, not in `Standards/`.

A global standards file may be excluded outright by a repository using the exclusion marker system defined in [2.14 Exclusion Marker System Required](#214-exclusion-marker-system-required) rather than an addendum. An addendum is used only when a repository must follow a modified version of a rule or a repository-specific rule not otherwise covered, not when the rule does not apply at all.

### 2.16 Readme Companion File
<!-- STD-MARKER: file-specification.2.16 -->

A standards file may have exactly one companion readme file. A companion readme file holds the rationale, history, and explanatory context that [Section 2.5](#25-no-rationale-or-history) prohibits from appearing in the standards file itself.

A companion readme file must be named `{StandardFileName}.readme.md`, matching the base name of the standards file it accompanies exactly, and must be placed in `Docs/Standards/Readme/`.

A companion readme file must contain one entry for each rule-bearing section, sub-section, or rule-item marker in its corresponding standards file that has documented rationale. Each entry must:

- Open with a heading that states the exact marker ID it explains, in the form `## {marker-id}`.
- Contain the rationale, history, or decision context for that rule.
- Not restate or duplicate the rule text itself; the rule text lives once, in the standards file.

Each rule-bearing section or sub-section in the standards file that has a corresponding readme entry must link to it using a Type 2 page-to-page link as defined in [Section 2.11](#211-reference-linking-required), pointing to the `## {marker-id}` heading in the companion readme file. This link is a navigational pointer only and must not be accompanied by explanatory text in the standards file.

A companion readme file must not contain rules, directives, or content that would itself require compliance verification. A companion readme file is not a standards file and is not subject to [Section 3](#3-required-file-structure) or [Section 4](#4-required-header-block).

---

## 3. Required File Structure
<!-- STD-MARKER: file-specification.3 -->

Every standards file must follow this section order. Sections may not be omitted or reordered.

| Order | Section | Required |
|---|---|---|
| 1 | Header block (Version, Status, Applies To, Audience) | Yes |
| 2 | Purpose | Yes |
| 3 | One or more domain rule sections | Yes |
| 4 | Compliance Verification | Yes |
| 5 | Governance | Yes |

---

## 4. Required Header Block
<!-- STD-MARKER: file-specification.4 -->

Every standards file must open with the following header block. All four fields are required.

```
# [Standard Name]

**Version:** [MAJOR.MINOR.PATCH]
**Status:** [Draft | Active | Superseded]
**Applies To:** [Scope description]
**Audience:** AI models and human developers
```

---

## 5. File Naming
<!-- STD-MARKER: file-specification.5 -->

Standards files must be named in PascalCase, following the default casing rule defined in [`GlobalSolutionStructureStandards.md` Section 3.6](GlobalSolutionStructureStandards.md#36-default-folder-and-file-casing). The name must reflect the domain the file governs. Global standards files must use the `Global` prefix. All standards files must use the `Standards` suffix.

Correct: `Global<Domain>Standards.md`, `GlobalCodingStandards.md`, `GlobalTestingStandards.md`  
Incorrect: `SolutionStructure.md`, `coding standards.md`, `standards1.md`, `global-coding-standards.md`

---

## 6. Compliance Verification
<!-- STD-MARKER: file-specification.6 -->

Before committing a new or modified standards file, verify the following. A file that fails any item must not be committed.

- [ ] All rules are written as directives using "must" or "must not". <!-- STD-MARKER: file-specification.6.1 -->
- [ ] The file owns exactly one domain; no rules are duplicated from another file. <!-- STD-MARKER: file-specification.6.2 -->
- [ ] Every rule is verifiable by inspection, tooling, or a defined manual check. <!-- STD-MARKER: file-specification.6.3 -->
- [ ] All examples use real repository and project names from this codebase. <!-- STD-MARKER: file-specification.6.4 -->
- [ ] No rationale, history, or decision context appears in the file body. <!-- STD-MARKER: file-specification.6.5 -->
- [ ] Every rule is actionable without requiring context outside the standards files. <!-- STD-MARKER: file-specification.6.6 -->
- [ ] No speculative or future-state content is present. <!-- STD-MARKER: file-specification.6.7 -->
- [ ] The header block declares a version number and a valid status. <!-- STD-MARKER: file-specification.6.8 -->
- [ ] The file ends with a Compliance Verification section followed by a Governance section. <!-- STD-MARKER: file-specification.6.9 -->
- [ ] The file name is PascalCase, uses the `Standards` suffix, and uses the `Global` prefix when the file is a global standards file. <!-- STD-MARKER: file-specification.6.10 -->
- [ ] Every rule supported by an external reference links to the corresponding entry in [`Docs/References/StandardsReferences.md`](../references/StandardsReferences.md), and that entry links back (Type 1). <!-- STD-MARKER: file-specification.6.11 -->
- [ ] Every rule that defers to another standards file uses a direct relative markdown link to that file and section (Type 2). <!-- STD-MARKER: file-specification.6.12 -->
- [ ] Every in-file section reference uses an in-page anchor link rather than plain text (Type 3). <!-- STD-MARKER: file-specification.6.13 -->
- [ ] The file is included in the governance chain rooted at the designated root governance standard if it is an actual standards file. <!-- STD-MARKER: file-specification.6.14 -->
- [ ] Every rule-bearing section and sub-section carries a `<!-- STD-MARKER: {file-base-name}.{section-number} -->` marker, the file itself carries a `{file-base-name}.file` marker beneath its header block, and every Compliance Verification checklist item carries its own `{file-base-name}.{section-number}.{item-number}` rule-item marker. <!-- STD-MARKER: file-specification.6.15 -->
- [ ] If a companion readme file exists, it is named `{StandardFileName}.readme.md`, is placed in `Docs/Standards/Readme/`, and every section it documents links to it by marker ID using a Type 2 link. <!-- STD-MARKER: file-specification.6.16 -->

---

## 7. Governance
<!-- STD-MARKER: file-specification.7 -->

This standard and all standards files in the GlobalStandards repository are owned by Troy Crowe. No changes to any standards file may be merged without Troy Crowe's explicit approval. Changes must be submitted as a pull request that includes a rationale comment explaining the reason for the update or deviation. Direct commits to `dev` or `main` are not permitted. Branch, PR, and approval rules must be defined by the root governance standard.
