# GlobalGovernanceStandards.md — Rationale and Context

This file documents the rationale, history, and decision context for rules in [`GlobalGovernanceStandards.md`](../GlobalGovernanceStandards.md). Per [`GlobalFileSpecificationStandards.md` Section 2.16](../GlobalFileSpecificationStandards.md#216-readme-companion-file), this content must not appear in the standard itself.

## Section 10 — Documentation Governance (`governance.10`)

**Clarification:** "Component" in this rule means any distinct tool, feature, or automation unit added to a repository — not every individual file or code change. The `<ToolName>.Planning.md` is intentionally a historical record and is not expected to be rewritten after the fact; only the `README.md` is required to stay current as the component evolves. This distinction exists so reviewers do not treat an unchanged `Planning.md` as a compliance gap when a component's behavior changes — only a stale `README.md` is a gap.

**Origin:** This rule originated from a discussion about documenting the `ValidationEngine` periodic-reminder/cadence automation feature, where it was decided that this expectation is promoted from a one-off preference into a durable, governed rule.
