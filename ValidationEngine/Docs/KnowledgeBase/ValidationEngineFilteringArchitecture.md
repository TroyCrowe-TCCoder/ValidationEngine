# ValidationEngine — Rule/File Filtering Architecture

**Repository:** GlobalStandards
**Created:** 2026-05-18
**Status:** Living document — additions expected as the architecture evolves

---

## Purpose

This document captures the authoritative decision model for how rule and file filtering is layered across `ValidationEngine` and its sibling executor `ValidationEngine.Agent`. It exists so the rationale behind each layer — why it exists, what question it answers, and why it cannot be collapsed into another layer — is preserved for future reference and decision understanding, rather than needing to be re-derived from source.

`ValidationEngine.Agent` is a single consolidated application (one project file) invoked as an external tool by `ValidationEngine` via `ValidationEngineAgentInvocationFactory`. It is a pure executor: it expects `--rules` fuel from `ValidationEngine` and, if launched without it, emits a manual-review diagnostic (`AGT-000`) noting it requires being run by `ValidationEngine` rather than attempting to derive its own scope.

---

## Five Filtering Layers

Each layer answers a different question at a different granularity. None is a substitute for another — skipping or collapsing one narrows validation scope incorrectly.

### ValidationEngine (3 layers)

**Layer 1 — AppType relevance** (`ApplicableStandardsResolver` + `StandardsApplicabilityMatrix.csv` + `AppTypeDetector`)
Question: *does this entire standards file apply to this solution's detected AppType at all?*
Whole-standards-file granularity — e.g. a WebAPI repository drops `GlobalDatabaseStandards.md` entirely because it has no database layer. Automated and matrix-driven, not a manual list.

**Layer 2 — Solution-specific rule exclusion** (repo-local exclusion addendum in `.github/copilot-instructions.md`, read by `ExclusionAddendumReader`, applied in `ValidationScopeDeriver.DeriveRules`)
Question: *within a standards file Layer 1 already kept in scope, is this specific rule marker irrelevant to this particular solution?*
Rule-level, not file-level — Layer 1 cannot catch this because it only operates at whole-file granularity. Example: `GlobalSolutionStructureStandards.md` stays in scope for a monolith repository (Layer 1 keeps the whole file), but a rule inside it written for microservice-specific folder conventions doesn't apply to that monolith. This is a manual, human-curated fallback specifically because Layer 1's AppType matrix isn't granular enough to express intra-file rule applicability.

**Layer 3 — Coverage matrix collection-building** (`CoverageMatrixReader`)
Not a filtering decision — it assembles the final rule collection from whatever survived Layers 1 and 2, restricted to `Confidence == Manual-only` or `Status == Missing` rows, into the fuel handed to a sibling tool such as `ValidationEngine.Agent`. Binary/Heuristic rows never reach this collection; they're handled directly by `MechanicalValidationRules`/Roslyn analyzers inside `ValidationEngine`.

### ValidationEngine.Agent (1 layer)

**Layer 1 — File/rule-type match** (`RuleFileApplicability`)
Question: *given a rule that survived all three ValidationEngine layers, does it make sense against this specific file's type within the changeset the agent was handed?*
Does **not** exclude the rule from the run — it stays active and is still checked against other files in the same changeset that do match its relevant type. The agent iterates per file: for each changed file, it checks every rule in the fuel for relevance, resolves that file fully against all its relevant rules, then moves to the next file. Example: a `coding.*` rule is irrelevant against `azure-pipelines.yml` but still applies to `Accounts.cs` in the same changeset.

**Layer 2 — LLM-necessity gate — dropped, not implemented (decided 2026-05-19)**
Originally proposed to answer: *for a rule/file pair that survived Layer 1, can the agent resolve this deterministically itself, or does it genuinely require an LLM call?* Re-evaluated and dropped for these reasons:
1. **Layers 1–3 (ValidationEngine) plus Agent Layer 1 already minimize what reaches the LLM** — AppType relevance, solution-specific exclusion, the binary/heuristic-vs-manual-only split, and file/rule-type matching together already narrow the fuel to a small, genuinely-relevant set before any LLM call is considered. A further gate has little left to filter.
2. **Real-world changesets are small by design** — the Agent runs against a per-commit/per-PR changeset (a handful of files), not full-repository audits; it never runs across multiple repositories at once. The LLM-call volume this gate would reduce is inherently bounded already, making the savings marginal.
3. **Risk/complexity tradeoff isn't worth it** — a deterministic resolver that is subtly wrong could silently report "compliant" and suppress a real violation, which is a worse failure mode than an unnecessary LLM call. `Manual-only` classification in the coverage matrix already means "this needs judgment"; building parallel deterministic logic for these specific rules risks re-implementing — badly — the exact judgment the LLM exists to provide.

**Decision: every rule/file pair that survives Agent Layer 1 goes to the LLM. No Layer 2 exists.**

---

## Open Follow-Up: Configurable JSON File-Level Exclusions

A separate, solution-wide (not agent-only) exclusion concept has been raised: certain files (e.g. `README.md`) should be excluded from most rule validation regardless of AppType or rule marker, driven by a configurable JSON list in app configuration rather than hardcoded in source. This is distinct from all five layers above — it is a blanket per-file exclusion, not a rule-level or AppType-level decision — and has not yet been designed or implemented. Track as follow-up work before building it into either `ValidationEngine` or `ValidationEngine.Agent`.

---

## Evaluated Alternative: GitHub Copilot Code Review for Azure Repos (2026-05-19)

Before continuing implementation of the Agent Layer 2 (LLM-necessity gate), a build-vs-buy check was performed against **GitHub Copilot code review for Azure Repos** (limited public preview) to confirm the custom build isn't reinventing an existing capability.

**What it is:** A first-party Azure DevOps feature that reviews PRs using Copilot, reading the same `.github/copilot-instructions.md` (or `.azuredevops/copilot-instructions.md`) this repository already maintains, with org/project/repo-scoped custom instructions.

**Sources consulted:**
- [Configure Copilot code review instructions](https://learn.microsoft.com/azure/devops/repos/git/configure-copilot-code-review-instructions?view=azure-devops)
- [Get started with Copilot code review for pull requests](https://learn.microsoft.com/azure/devops/repos/git/copilot-code-reviews?view=azure-devops)
- [Troubleshoot Copilot code review](https://learn.microsoft.com/azure/devops/repos/git/copilot-code-reviews-faq?view=azure-devops)

**Decision: do not switch — continue building the custom `ValidationEngine`/`ValidationEngine.Agent` gate.**

Reasons:
1. **No merge gate.** Per Microsoft's own docs, Copilot code review always leaves a Comment-type review — it explicitly *"doesn't satisfy required-reviewer policies and doesn't block merging."* This repository's model requires a hard gate (build-validation-style block), which this feature structurally cannot provide.
2. **No scoping/boundaries — reviews everything, every time.** It has no equivalent of Layers 1–3 (AppType relevance, solution-specific rule exclusion, coverage-matrix scoping) or Agent Layer 1 (file/rule-type match). It runs Copilot against the full diff on every PR regardless of whether a rule is already mechanically enforced by an analyzer, making it a heavier, less-bounded working layer than the filtered pipeline already built.
3. **No incremental value once the custom system exists.** Once the LLM-necessity gate is built, this repository will already have deterministic pre-filtering plus a real gate — adopting Copilot code review afterward would add licensing/usage cost (a separate Azure DevOps preview feature with its own concurrency limits) for capability that's already covered, with no gate benefit gained in return.
4. Preview constraints reinforce this: 100-file/10 GB PR caps, no SLA, limited concurrency (2 reviews/user, 5/org), and data residency that doesn't align with Azure DevOps org data-residency boundaries — real constraints for a governance-critical gate.

**Logged for future review, not dismissed permanently.** If Copilot code review for Azure Repos exits preview and adds (a) a blocking/required status check equivalent, and/or (b) scoped/filtered review (e.g. skip rules already covered by analyzers), revisit this decision — it could become a viable *supplement* to, or even partial replacement for, the Agent-side layers. Re-check this section before any major new investment in Agent Layer 2.

---

## Resume Point (as of 2026-05-18 end of session)

**Completed this session:**
- `ValidationEngine.Agent` consolidated into a single app/project (was two sibling apps); solution, invocation factory (`ValidationEngineAgentInvocationFactory`), and its tests renamed accordingly. Build verified green (0 errors).
- Confirmed via code inspection: `ValidationScopeDeriver.DeriveRules` (Layer 2) and `CoverageMatrixReader` (Layer 3) ownership, and `RuleFileApplicability` (Agent Layer 1) purpose — not scope creep, a legitimate per-file relevance optimization.
- This document created to permanently record the finalized 3-layer ValidationEngine + 2-layer Agent model, superseding the need to re-derive it from conversation history.

**Not yet started / next session should pick up here:**
1. **Agent Layer 2 — LLM-necessity gate.** Design is only conceptual (see section above). Needs: the actual predicate/pattern attachment mechanism on a rule entry, default-to-"needs LLM" behavior, and where in `ValidationEngine.Agent/Program.cs` the gate check should sit relative to `RuleFileApplicability`.
2. **Configurable JSON file-level exclusions** (e.g. `README.md`). Needs: where the JSON config lives (likely `appsettings.json` in `ValidationEngine`, since it's solution-wide, not agent-only), what shape the list takes (simple filename/glob list vs. per-file rule-category exclusions), and where in the `ValidationEngine` pipeline it applies (likely before or alongside Layer 1 AppType relevance, since it's file-level not rule-level).
3. No code has been written for either #1 or #2 — both are design-only at this point. Start next session by re-reading this document's "ValidationEngine.Agent (2 layers)" and "Open Follow-Up" sections, then propose a concrete design for whichever the user wants to tackle first.

**Key files to re-open next session:**
- `ValidationEngine.Agent/Program.cs` (where the LLM-necessity gate would be wired in)
- `ValidationEngine.Agent/Rules/RuleFileApplicability.cs` (Agent Layer 1, adjacent to where Layer 2 would sit)
- `ValidationEngine/Infrastructure/ValidationScopeDeriver.cs` (closest existing analog for how a new exclusion mechanism should be structured)
- This document, for the full rationale before proposing any implementation.
