# Coverage Matrix — GlobalFileSpecificationStandards.md

**Status:** Reviewed
**Sign Off:** Troy Crowe
**Sign Off Date:** 2026-08-14
**Revision Date:**
**Columns**

| Column | Meaning |
|---|---|
| STD-MARKER | Rule id from the standards file. |
| Rule Summary | One-line paraphrase of the rule. |
| Category | Meta-Standard / Documentation / Governance. |
| Confidence | Binary (deterministic pass/fail) / Heuristic (risk-flag, needs human confirmation) / Manual-only (no automation path). |
| Severity | Hard-stop / Warning / Manual-only-comment. |
| Frequency | Every-Commit (change-set scoped, applies only when a `.md` file under `Docs/Standards/` changes) / Periodic. |
| Technique | Short detection approach. |
| Status | Missing / Existing / Acknowledged-future-work. |

**Applicability note:** This file is a **meta-standard** — it governs the authoring of the other standards files themselves (including this validation-matrix effort's own inputs), not application code. Its scope is exclusively `Docs/Standards/*.md` in the GlobalStandards repository, plus the `Standards/*.md` addendum files in consuming repositories. Detection is dominated by **Markdown structural/regex parsing** (heading order, marker syntax, link presence) rather than code analysis. This file is uniquely self-referential: this very Coverage Matrix effort is itself subject to file-specification.2.14 (exclusion marker system) once/if these matrices are formalized as validation inputs.

---

### Section 2 — Core Requirements

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| file-specification.2.1 | Every rule written as a directive using "must"/"must not"/"is required"/"is not permitted"; must not use "should", "consider", "it is recommended", "you may", "optionally" | Documentation | Binary | Hard-stop | Every-Commit (on `Docs/Standards/*.md` changes) | Regex/text scan: flag occurrences of the denylisted hedge words/phrases within rule body text (excluding code blocks and quoted examples) | Missing |
| file-specification.2.2 | Each file owns exactly one domain; no rule may appear in more than one file — cross-references must link, not duplicate | Meta-Standard | Heuristic | Hard-stop | Every-Commit | Cross-file text-similarity scan comparing rule paragraphs across all `Docs/Standards/*.md` files to detect near-duplicate rule statements; domain-boundary judgment (is this really the "same" rule or a related-but-distinct one) requires human confirmation, keeping this Heuristic | Missing |
| file-specification.2.3 | Every rule must be verifiable by inspection, tooling, or a defined manual check; if the verification method is not obvious it must be stated alongside the rule | Meta-Standard | Manual-only | Manual-only-comment | Every-Commit | Requires human judgment on whether a rule is meaningfully verifiable — this is precisely the judgment this entire coverage-matrix exercise performs manually; not mechanically automatable | Missing |
| file-specification.2.4 | All examples reference real repository/project/folder/code-pattern names from the active repository list; no abstract placeholders (`MyProject`, `SomeService`, `YourRepository`) | Documentation | Heuristic | Warning | Every-Commit | Regex/denylist scan: flag known placeholder-name patterns (`MyProject`, `SomeService`, `YourRepository`, `Foo`, `Bar`, etc.) inside code-block examples; confirming a name is genuinely "real" (matches an actual repo) requires cross-referencing against a repository inventory | Missing |
| file-specification.2.5 | No rationale, history, or "why"/"previously"/alternatives-considered content in the standard body | Documentation | Heuristic | Warning | Every-Commit | Regex/keyword scan: flag sentences containing rationale markers ("because", "previously", "historically", "this was chosen since", "in the past") outside of linked reference-catalog entries; semantic false-positive risk keeps this Heuristic | Missing |
| file-specification.2.6 | Every rule actionable by both human and AI without external context; required tool/process/config knowledge must be inline or linked | Documentation | Manual-only | Manual-only-comment | Periodic | Requires judgment on whether a rule presumes unstated context — not mechanically detectable | Missing |
| file-specification.2.7 | No speculative/future-state/aspirational/not-yet-implemented content in a standards file | Documentation | Heuristic | Hard-stop | Every-Commit | Regex/keyword scan: flag future-tense planning language ("will eventually", "in a future version", "not yet implemented", "TBD", "planned for") in rule body text | Missing |
| file-specification.2.8 | Header must declare `**Version:** MAJOR.MINOR.PATCH` and `**Status:** Draft \| Active \| Superseded`; version increments follow semver rules (PATCH/MINOR/MAJOR) | Meta-Standard | Binary | Hard-stop | Every-Commit | Regex: verify header block contains both fields with the exact label format and that `Status` is one of the three allowed literal values; verifying the *correctness* of the semver increment relative to the actual change requires diffing the previous version against the new one — feasible but a separate, more involved check (compare rule-count/rule-text delta against version bump size) | Missing |
| file-specification.2.9 | File must end with a Governance section using the standard footer text ("Branch, PR, and approval rules must be defined by the root governance standard.") | Meta-Standard | Binary | Hard-stop | Every-Commit | Regex: verify a `## [N]. Governance` heading exists as the final section and its body matches (or closely matches) the required footer text | Missing |
| file-specification.2.10 | Compliance Verification section required immediately before Governance; heading must be `## [N]. Compliance Verification`; each checklist item carries its own rule-item marker and is an index pointer (not a restatement) to exactly one rule; every rule-bearing section has at least one checklist item; every checklist item marker resolves to an existing section | Meta-Standard | Binary | Hard-stop | Every-Commit | Regex: verify heading text and position (second-to-last section); parse all `<!-- STD-MARKER: {base}.{n}.{item} -->` rule-item markers under the checklist and cross-reference each against the set of section/sub-section markers defined earlier in the file — flag any checklist marker with no matching section, and any rule-bearing section with zero checklist items referencing it | Missing |
| file-specification.2.11 | Three required reference-link types: (1) Reference Catalog Links — bidirectional link to `StandardsReferences.md` entry, entry has `Referenced by:` back-link, catalog outline updated; (2) Page-to-Page/Section Links — relative markdown link with `#section-anchor` to another file, even if target doesn't exist yet; (3) In-Page Section Links — `[Section N](#n-section-heading)` anchor format, not plain text like "defined in Section 3" | Documentation | Heuristic | Hard-stop | Every-Commit | Regex: parse all markdown links in the file and classify as internal-anchor vs. cross-file vs. reference-catalog; flag plain-text section references (regex for "[Ss]ection \d+" not immediately followed by/wrapped in a markdown link) as Type-3 violations; verifying Type-1 bidirectionality requires parsing `StandardsReferences.md` for the matching `Referenced by:` entry — a cross-file check | Missing |
| file-specification.2.12 | Every actual standards file must appear in the governance chain rooted at the designated root governance standard, which links directly to it; guides/templates/rollout aids/working docs must not be treated as governance-chain files | Meta-Standard | Binary | Hard-stop | Every-Commit (new file added under `Docs/Standards/`) | Cross-file link scan: parse the root governance standard file for a direct link to every file under `Docs/Standards/` matching the naming convention; flag any standards file with no inbound link from the root governance file | Missing |
| file-specification.2.13 | Global standards files use `Global` prefix and `Standards` suffix; repository-local deviation files use the matching base name without the `Global` prefix | Meta-Standard | Binary | Hard-stop | Every-Commit | Regex on filename: `^Global[A-Z][a-zA-Z]*Standards\.md$` for global files under `Docs/Standards/`; `^[A-Z][a-zA-Z]*Standards\.md$` (no `Global` prefix) for files under a consuming repo's `Standards/` folder | Missing |
| file-specification.2.14 | Every rule-bearing section/sub-section/checklist item carries an exclusion marker at one of three tiers (file/section/rule-item) using the exact syntax `{file-base-name}.{section-number}[.{item-number}]` or `{file-base-name}.file`; base-name is the lowercase-hyphenated form of the filename without `Global` prefix/`Standards` suffix/`.md`; markers must be updated when sections are renumbered/reordered; a marker not resolving to an existing section/item is non-compliant | Meta-Standard | Binary | Hard-stop | Every-Commit | Regex: parse all `<!-- STD-MARKER: {id} --!>` comments in the file; verify id format matches `{base}.{section}[.{item}]` or `{base}.file`; verify `{base}` matches the expected lowercase-hyphenated derivation of the actual filename; verify every numbered heading and every checklist bullet has an adjacent marker (structural completeness check) — this is the core machine-readable scaffolding the entire validation engine depends on, making it one of the highest-value automatable rules in the whole corpus | Missing |
| file-specification.2.15 | Repository-local addendum defines exactly three content types (deviation / addition / skip-exclusion) per the required field templates (Overridden Rule/Deviation/Requirement/Reason/Requester/Approver/Effective Date/Planned Review Date for deviations; Rule/Scope Justification/Requirement/Reason/Requester/Approver/dates for additions; Marker ID/Reason/Requester/Approver/Effective Date/Planned Review Date for skips); addendum must not restate/copy the global standard or contain non-varying implementation notes; addendum file resides in the repo's root `Standards/` folder using the matching base name without `Global` prefix | Meta-Standard | Heuristic | Hard-stop | Periodic (per-addendum review) | Regex/template-field scan: verify each documented deviation/addition/skip block contains all required labeled fields (`**Overridden Rule:**`, `**Deviation:**`, `**Requirement:**`, `**Reason:**`, `**Requester:**`, `**Approver:**`, `**Effective Date:**`, `**Planned Review Date:**` or the addition/skip equivalents); verify filename matches `Standards/{BaseName}Standards.md`; detecting whether the addendum "restates the global standard" (i.e., contains non-varying content) requires text-similarity comparison against the source global file — Heuristic | Missing |

---

### Section 3 — Required File Structure

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| file-specification.3 | Fixed section order: Header block → Purpose → domain rule sections → Compliance Verification → Governance; sections may not be omitted or reordered | Meta-Standard | Binary | Hard-stop | Every-Commit | Regex: parse top-level `##` headings in file order and verify the sequence matches Header→Purpose→(any)→Compliance Verification→Governance | Missing |

---

### Section 4 — Required Header Block

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| file-specification.4 | Header block must open with `# [Standard Name]` followed by `**Version:**`, `**Status:**`, `**Applies To:**`, `**Audience:** AI models and human developers` — all four fields required | Documentation | Binary | Hard-stop | Every-Commit | Regex: verify the first non-blank lines match the required H1 + four-field pattern in order, with `Audience` matching the exact required literal text | Missing |

---

### Section 5 — File Naming

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| file-specification.5 | Standards files named in PascalCase reflecting the governed domain; global files use `Global` prefix; all standards files use `Standards` suffix | Documentation | Binary | Hard-stop | Every-Commit | Regex on filename — duplicates file-specification.2.13 detection exactly; same technique, same marker pair reinforcing the naming rule from two sections | Missing |

---

### Section 6 — Compliance Verification (Excluded as an independent detection target)

The `file-specification.6.1`–`6.14` checklist items restate Sections 2–5 verbatim as a pre-commit checklist. No independent detection techniques are introduced; each maps 1:1 to a rule already captured above (2.1→6.1, 2.2→6.2, 2.3→6.3, 2.4→6.4, 2.5→6.5, 2.6→6.6, 2.7→6.7, 2.8→6.8, 3+2.9→6.9, 2.13/5→6.10, 2.11 Type1→6.11, 2.11 Type2→6.12, 2.11 Type3→6.13, 2.12→6.14). This section is itself the canonical example of file-specification.2.10's required "index-pointer, not restatement" pattern.

### Section 7 — Governance (Excluded)

`file-specification.7` (ownership/approval boilerplate) is excluded — not code-checkable, procedural ownership statement only.

---

**Notes:**
1. This is the **meta-standard governing the standards corpus itself** — its detection scope is exclusively Markdown structure/regex, and it is the natural place to anchor an automated "standards linter" that validates every other `GlobalXStandards.md` file (header format, section order, marker syntax, link completeness) before a matrix like this one is even drafted. Several of its rules (2.8, 2.9, 2.10, 2.13, 2.14, 3, 4, 5) are fully Binary and mechanically cheap — arguably the highest-ROI automation target in the entire corpus reviewed so far, since a single linter tool would validate structural compliance across all ~19 standards files at once.
2. file-specification.2.14 (exclusion marker system) is the load-bearing mechanism referenced by nearly every other matrix produced in this batch (e.g., `solution-structure.14.21` React-casing skip example, the CaptiveExpensesApi repo's own marker-exclusion table in its `copilot-instructions.md`). Any validation engine implementation should build the marker parser/resolver required by this rule first, since it is a prerequisite for evaluating exclusions declared by every consuming repository.
3. file-specification.2.2 (single-responsibility / no duplicate rules across files) is directly relevant to observations already made in prior matrices in this batch — e.g., the global-error-handler rule appearing in `GlobalCodingStandards.md`, `GlobalSecurityStandards.md`, and `GlobalSolutionStructureStandards.md` (solution-structure.12), and the `Working/` folder retention rule appearing in both `GlobalSolutionStructureStandards.md` (2.2) and `GlobalRepositoryStandards.md` (Section 8). Those are candidate real-world violations of this exact rule and worth flagging back to the standards authors during matrix review.
4. Most rules here require **text/semantic judgment** (hedge-word detection, rationale-language detection, placeholder-name detection, "restates the global standard" comparison) rather than pure structural parsing, so despite the file's small size it produces a higher proportion of Heuristic/Manual-only classifications than its structural rules (2.8–2.14, Sections 3–5) would suggest at first glance.

