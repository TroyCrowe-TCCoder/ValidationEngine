# Coverage Matrix — GlobalAgentStandards.md

**Status:** Paused — see backlog `STD-010` (reassessed and rebuilt to v2.0.0; matrix regenerated to match new section numbering and new Sections 3/7. Formal sign-off still pending.)
**Sign Off:**
**Sign Off Date:**
**Revision Date:** 2026-07-13
**Columns**

| Column | Meaning |
|---|---|
| STD-MARKER | Rule id from the standards file. |
| Rule Summary | One-line paraphrase of the rule. |
| Category | Structure / Technology / Development / Procedural / Infrastructure. |
| Confidence | Binary (deterministic pass/fail) / Heuristic (risk-flag, needs human confirmation) / Manual-only (no automation path). |
| Severity | Hard-stop / Warning / Manual-only-comment (3-tier enforcement model). |
| Frequency | Every-Commit (validated against the files touched in the current change set) / Periodic (validated on a recurring, configurable cadence). |
| Technique | Short detection approach. |
| Status | Missing / Existing / Acknowledged-future-work. |

**Applicability note:** This entire file is gated by the `agent.file` capability flag (see `StandardsCapabilityFlags.csv`) — it only applies to a repository that has declared it implements a custom AI agent. Every row below is conditional on that flag; if a repository has not enabled it, none of these rules are in play regardless of Frequency.

---

### Section 2 — Agent Design Principles

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| agent.2.1 | Single responsibility — agent scoped to one dev-purpose category (API, Database, UI, Service, Repository, Pipeline Management, etc.); does not cross into another category's work | Development | Heuristic | Warning | Every-Commit | File-path/project-type scan: verify the agent's implementation file resides in a folder/project matching its declared dev-purpose category (per `GlobalSolutionStructureStandards.md` folder-naming conventions and project type/kind); flag references to APIs/types belonging to a different dev-purpose category's folder or project | Missing |
| agent.2.2 | Explicit boundaries (inputs/outputs/tools/refusals) documented in system prompt and repo docs | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax/text-scan: system prompt file contains recognizable "Scope"/"Refuse"-style sections | Missing |
| agent.2.3 | Consistent behavior for a given input class within documented boundaries; no undocumented fallback behavior; explicit documented refusal/error when input falls outside boundaries | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax/text-scan: flag catch-all/undocumented-fallback branches in agent request-handling code lacking a corresponding documented refusal path; cross-check refusal paths against the boundaries documented per agent.2.2 | Missing |

---

### Section 3 — Risk Classification and Autonomy Tiering (NEW in v2.0.0)

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| agent.3.1 | Every agent declares a risk tier (1/2/3) in its system prompt header and repository documentation; undeclared tier is non-compliant | Development | Binary | Hard-stop | Every-Commit | Text-scan: prompt file header comment block must contain a recognizable "Risk Tier: N" line; repo docs must contain a matching declaration | Missing |
| agent.3.2 | Tier determines which Section 7 safeguards are mandatory; agent must not be under-tiered relative to its actual capabilities | Development | Manual-only | Manual-only-comment | Periodic | Requires human judgment comparing declared tier against actual tool/capability surface — not structurally checkable; flag for reviewer attention whenever tool list changes materially | Missing |
| agent.3.3 | Adding/expanding a tool or increasing autonomy configuration requires re-evaluating tier before merge; tier change called out explicitly in the PR description | Procedural | Heuristic | Warning | Every-Commit | Diff heuristic: flag PRs that add a new tool registration or modify max-turn/autonomy configuration without a corresponding "Risk Tier" mention in the PR description or a diff to the declared tier | Missing |

---

### Section 4 — System Prompt Design

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| agent.4.1 | System prompt has 5 required sections in order: Identity, Scope, Behavior rules, Output format, Refusal rules | Development | Heuristic | Warning | Every-Commit | Text-scan: prompt file contains 5 recognizable section headers in order | Missing |
| agent.4.2 | System prompts stored in version-controlled files, not hardcoded string literals in business logic; named/versioned per convention, including declared risk tier | Structure | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag long string literals resembling a system prompt inside non-configuration `.cs` files; file-presence check for prompt file with version header including risk tier | Missing |
| agent.4.3 | No jailbreak surface — system prompt must not contain override-able phrasing | Development | Heuristic | Warning | Every-Commit | Text-scan: flag known jailbreak-adjacent phrases ("ignore previous instructions", "you can also") in prompt files | Missing |

---

### Section 5 — Tool Design

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| agent.5.1 | Each tool performs exactly one operation; no compound operations inside a single tool | Development | Manual-only | Manual-only-comment | Every-Commit | Requires semantic understanding of what a tool "does"; not structurally checkable | Missing |
| agent.5.2 | Tool schema is complete/typed: `snake_case` name, description, typed params with descriptions, required-params marked | Development | Binary | Hard-stop | Every-Commit | Roslyn-syntax/JSON-schema validation: parse tool registration/schema definitions for required fields | Missing |
| agent.5.3 | Write-operation tools require confirmation gating, log every invocation (identity/params/timestamp/outcome), and enforce same authZ as the equivalent HTTP endpoint | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag tool methods with create/update/delete-like names lacking authorization attribute or audit-logging call | Missing |
| agent.5.4 | Tools return structured error responses (e.g., `ToolResult<T>`), not unhandled exceptions to the agent | Development | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag tool method signatures that can throw without a try/catch boundary returning a structured result type | Missing |

---

### Section 6 — Model Selection and Configuration

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| agent.6.1 | Model explicitly declared in configuration; no undocumented runtime model selection | Structure | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag model-client construction without an explicit model identifier read from config | Missing |
| agent.6.2 | Temperature explicitly set per task type (0 for deterministic, 0.3–0.5 for drafting, >0.7 requires documented justification) | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag completion calls with no explicit `temperature` parameter; flag `>0.7` values lacking a nearby justification comment | Missing |
| agent.6.3 | `max_tokens` set on every completion call; no unbounded completion length | Development | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag completion calls missing an explicit token-limit parameter | Missing |

---

### Section 7 — Responsible AI Safeguards (NEW in v2.0.0)

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| agent.7.1 | Agent documents external data sources read and external systems written to; boundary data validated (schema/size/content-type) at the crossing | Development | Heuristic | Warning | Every-Commit | Cross-reference agent.2.2 documented tool list against actual tool implementations reading/writing external systems; flag undocumented external calls | Missing |
| agent.7.2 | Every side-effect action is reconstructable after the fact (trigger, decision, tool, outcome) — auditability | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: same detection as agent.5.3 invocation logging, cross-checked for completeness of the four required fields | Missing |
| agent.7.3 | Tier 2/3 agents require an independent oversight/circuit-breaker mechanism not implemented by the agent itself | Infrastructure | Manual-only | Manual-only-comment | Periodic | Requires verifying an external monitor/kill-switch exists and is operated independently of the agent's own code — not detectable from the agent's source alone; verify against declared tier (agent.3.1) | Missing |
| agent.7.4 | Tier 2/3 agents disclose AI-interaction to users and can explain the basis for a decision on request | Development | Manual-only | Manual-only-comment | Periodic | Requires human judgment about UX disclosure and explainability quality; verify against declared tier (agent.3.1) | Missing |
| agent.7.5 | Tier 3 agents require a human approval checkpoint before any irreversible/high-impact action, presenting the pending action and its basis | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: for agents declared Tier 3, flag write-operation tool invocations (per agent.5.3) with no preceding approval-gate call in the same execution path | Missing |
| agent.7.6 | Tier 3 agents require a scheduled, recorded fairness/consistency review for decisions affecting users differently | Procedural | Manual-only | Manual-only-comment | Periodic | Requires a recorded review artifact/cadence; verify against declared tier (agent.3.1) — not code-checkable | Missing |

---

### Section 8 — Input and Output Validation

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| agent.8.1 | User-supplied inputs validated before passing to model/tools per `GlobalSecurityStandards.md` Section 5 | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag tool/model invocation call sites where the argument traces to unvalidated request input | Missing |
| agent.8.2 | Structured agent output validated against declared schema before acting on it; retry-with-correction on validation failure | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag structured-output deserialization without a subsequent schema/validation call | Missing |
| agent.8.3 | Prompt injection defense — externally sourced content delimited and treated as data, not instructions | Development | Heuristic | Warning | Every-Commit | Text-scan/Roslyn-syntax: flag string concatenation of external content directly into a system-prompt variable without delimiter markers | Missing |

---

### Section 9 — Observability

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| agent.9 | Model/tool invocations logged (name, tokens/params, latency, outcome) with correlation ID and declared risk tier; no full message/response content logged outside PII-safe context | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag model/tool invocation call sites lacking an adjacent logging call; flag logging calls that pass the full prompt/response object instead of metadata; verify risk-tier metadata field present | Missing |

---

### Section 10 — Conversation and Memory Management

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| agent.10.1 | Conversation history summarized/truncated near context-window limit, not accumulated indefinitely | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag conversation-history collections appended to with no summarization/eviction logic nearby | Missing |
| agent.10.2 | Long-term memory stored externally in a retrieval system, not hardcoded into the system prompt | Development | Heuristic | Warning | Every-Commit | Text-scan: flag system prompt files containing data patterns that look like facts expected to change over time (dates, counts, names outside a template placeholder) | Missing |
| agent.10.3 | Tool calls are stateless with respect to conversation context | Development | Manual-only | Manual-only-comment | Every-Commit | Requires semantic understanding of tool implementation; not structurally checkable with confidence | Missing |

---

### Section 11 — Testing

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| agent.11.1 | Unit tests exist for every tool function (success/error/boundary paths); tool tests must not call live model APIs | Development | Heuristic | Warning | Every-Commit | Test-pairing heuristic: flag tool source files with no corresponding test file changed in the same change set | Missing |
| agent.11.2 | Prompt regression test suite exists, run before system-prompt/model/tool-schema changes; asserts output categories not exact strings | Procedural | Heuristic | Warning | Every-Commit | File-pairing heuristic: flag system-prompt file changes with no corresponding regression-test file touched | Missing |
| agent.11.3 | Adversarial prompt test cases exist (override attempts, out-of-scope requests, malformed inputs, injection attempts) | Procedural | Manual-only | Manual-only-comment | Every-Commit | Requires judgment about adversarial test *coverage quality*, not just test file presence | Missing |
| agent.11.4 | Tier-3 agents have a test verifying the irreversible/high-impact checkpoint actually pauses execution without simulated approval | Procedural | Heuristic | Warning | Every-Commit | Test-pairing heuristic: for agents declared Tier 3, flag missing test file/test method matching the checkpoint gate identified in agent.7.5 | Missing |

---

### Section 12 — Compliance Verification (Checklist)

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| agent.12 | Compliance checklist — restates Sections 2–11 as a checklist; not independently checkable beyond its constituent rules | Procedural | Manual-only | Manual-only-comment | Every-Commit | Duplicate coverage of Sections 2–11; no new detection needed | Missing |

---

### Section 13 — Governance (Excluded)

`agent.13` (ownership/PR-approval boilerplate) is excluded from this matrix per the same reasoning as other standards files' Governance sections — not code-checkable, procedural ownership statement only.

---

**Notes:**
1. This file's applicability is entirely capability-flag gated (`agent.file`); today it resolves to "No — excluded for all repositories" per `StandardsCapabilityFlags.csv`, so every row here is currently dormant until a repository declares the capability.
2. Several rules (agent.2.1, agent.2.3, agent.5.1, agent.10.3, agent.11.3) require semantic/business judgment that static analysis cannot reliably provide — marked Manual-only rather than forcing a low-confidence heuristic. This is unchanged from v1.0.0.
3. The new Section 3 (Risk Classification) and Section 7 (Responsible AI Safeguards) rows skew more heavily Manual-only/Periodic than the rest of the file (agent.3.2, agent.7.3, agent.7.4, agent.7.6) — this is expected and correct, not a coverage gap: these rules concern intent, human-accessible controls, and organizational process (an external kill switch, a scheduled fairness review), which are not expressible as source-code patterns. See `Docs/Working/ToolingRoutingMatrix.md` for the full routing rationale on why Agent/Agent Team governance resists Roslyn/lint automation more than most other standards files.
4. agent.9 (Observability) intentionally cross-references `GlobalLoggingStandards.md` — its detection technique should reuse whatever logging-call-site detection is built for that file rather than duplicating logic.
5. agent.8.1 cross-references `GlobalSecurityStandards.md` Section 5 (input validation) — detection may be able to share a technique with that file's equivalent rule once both are implemented.
6. agent.3.1, agent.4.2, and agent.7.5 are the few Binary/Heuristic-with-clear-technique rows in the new Sections 3/7 — these are structurally checkable (a declared tier string, a header field, a call-site pattern) even though most of their siblings are not.
