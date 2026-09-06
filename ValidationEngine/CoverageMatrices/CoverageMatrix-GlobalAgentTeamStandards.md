# Coverage Matrix — GlobalAgentTeamStandards.md

**Status:** Paused — see backlog `STD-010` (reassessed and rebuilt to v2.0.0; matrix regenerated to match new section numbering and new Sections 3/10. Formal sign-off still pending.)
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

**Applicability note:** This entire file is gated by the `agent-team.file` capability flag — it only applies to a repository that has declared it implements a multi-agent system. It also extends `GlobalAgentStandards.md`, so every row in that file's matrix applies in addition to these rows for any agent participating in a team.

---

### Section 2 — When to Use a Multi-Agent Team

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| agent-team.2 | Multi-agent team only justified for specific scenarios (multi-domain expertise, parallel workstreams, context exhaustion, independent verification); avoid unjustified team overhead | Development | Manual-only | Manual-only-comment | Every-Commit | Requires architectural judgment about whether a single agent could accomplish the task; not structurally checkable | Missing |

---

### Section 3 — Team Risk Aggregation and Autonomy Tiering (NEW in v2.0.0)

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| agent-team.3.1 | Team's declared risk tier equals the maximum tier of any individual member agent (per `GlobalAgentStandards.md` Section 3); team is never under-tiered relative to its riskiest member | Development | Heuristic | Warning | Every-Commit | Cross-file scan: parse the declared tier (agent.3.1) of every worker/orchestrator agent in the team and compare against the team-level declaration; flag if team tier < max(member tiers) | Missing |
| agent-team.3.2 | Emergent autonomy is evaluated explicitly — orchestration composing low-risk workers can still produce high-risk team behavior (e.g., an irreversible action assembled from several read-only steps); this is called out and re-tiered accordingly | Development | Manual-only | Manual-only-comment | Periodic | Requires human judgment about composed/emergent behavior across the whole orchestration graph, not just individual member capabilities — not structurally checkable | Missing |
| agent-team.3.3 | Adding a new worker/role or changing orchestration logic that could change emergent risk requires re-evaluating team tier before merge, called out explicitly in the PR description | Procedural | Heuristic | Warning | Every-Commit | Diff heuristic: flag PRs that add a new worker-role registration or modify orchestrator routing/aggregation logic without a corresponding "Team Risk Tier" mention in the PR description | Missing |

---

### Section 4 — Team Roles

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| agent-team.4.1 | Exactly one orchestrator; orchestrator only plans/delegates/synthesizes, does not perform domain work itself | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag orchestrator-role class containing direct domain-logic calls (e.g., data access, business rule evaluation) rather than only delegation calls | Missing |
| agent-team.4.2 | Worker agents accept subtask, execute, return structured result; do not communicate with other workers directly | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag worker-agent classes with direct references/calls to another worker-agent class instead of routing through the orchestrator | Missing |
| agent-team.4.3 | Reviewer agent (optional) only evaluates, returns `Approved`/`NeedsRevision`/`Rejected` verdict; used when output affects important/unreviewed state | Development | Manual-only | Manual-only-comment | Every-Commit | Requires judgment about when a reviewer agent is warranted; structural presence of a reviewer type is checkable but its necessity is not | Missing |

---

### Section 5 — Communication Protocol

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| agent-team.5.1 | All inter-agent messages use typed structured schemas (e.g., `AgentHandoff<T>`), not raw natural-language strings | Development | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag inter-agent handoff call sites passing a raw `string` payload instead of a typed record/DTO | Missing |
| agent-team.5.2 | Every top-level task has a unique task identifier propagated to all subtasks and log entries (plays same role as correlation ID) | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag orchestrator task-entry methods with no task-ID generation/propagation parameter | Missing |
| agent-team.5.3 | No peer-to-peer worker communication; all sharing routed through orchestrator | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag worker-to-worker direct references (duplicates agent-team.4.2 detection — same technique, two markers) | Missing |

---

### Section 6 — Handoff Rules

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| agent-team.6.1 | Explicit handoff contract: identifies worker, provides complete context, specifies expected output format | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag handoff call sites constructing a payload with missing/null context fields relative to the declared handoff schema | Missing |
| agent-team.6.2 | Workers are stateless between invocations; each handoff includes all needed context, no reliance on shared state | Development | Manual-only | Manual-only-comment | Every-Commit | Requires understanding whether a worker implementation reads external mutable state between calls; not reliably structural | Missing |
| agent-team.6.3 | Orchestrator validates worker response before incorporating it into task state (reuses `GlobalAgentStandards.md` Section 8.2 output validation) | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag worker-response consumption call sites with no schema/validation call beforehand (duplicates agent.8.2 detection) | Missing |

---

### Section 7 — Failure Handling

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| agent-team.7.1 | Retry once on worker error/malformed response with corrected prompt; escalate after two consecutive failures | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag retry-loop implementations with no explicit max-attempt bound of 2, or unbounded retry loops | Missing |
| agent-team.7.2 | Escalation returns structured failure identifying failed subtask, worker, error, and partial/invalid status; no silent absorption of failures | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag catch blocks around worker invocation that swallow the error and return a success-shaped result | Missing |
| agent-team.7.3 | Partial results explicitly marked as partial, with indication of missing component and reason | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag result-construction paths that omit a required "IsPartial"/"MissingComponent" field when a subtask failure was tolerated | Missing |
| agent-team.7.4 | Every subtask handoff has an explicit timeout; no indefinite waiting | Development | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag worker-invocation call sites (async calls, task waits) with no explicit timeout/cancellation token parameter | Missing |

---

### Section 8 — Authorization and Security

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| agent-team.8.1 | Least privilege per agent — each agent has access only to tools/data it needs, not orchestrator-level permissions | Development | Manual-only | Manual-only-comment | Every-Commit | Requires judgment about whether a granted permission set matches actual subtask needs; not structurally checkable | Missing |
| agent-team.8.2 | No trust escalation between agents — worker tool calls subject to same authZ as if the user made the request directly | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag worker tool-invocation call sites that bypass an authorization check present on the equivalent direct-user-request path | Missing |
| agent-team.8.3 | Input validation at every agent boundary independently — worker must not trust orchestrator pre-validation | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag worker-agent entry points with no input-validation call at the start of subtask execution | Missing |

---

### Section 9 — Observability

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| agent-team.9 | Team logs every handoff (from/to/task ID/payload size/timestamp), every aggregation step, and end-to-end task duration; must reconstruct full execution path from task ID alone | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag handoff/aggregation call sites lacking an adjacent logging call carrying the task ID | Missing |

---

### Section 10 — Team-Level Responsible AI Safeguards (NEW in v2.0.0)

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| agent-team.10.1 | Team has an orchestrator-level oversight/circuit-breaker mechanism, independent of the orchestrator's own code, that can halt the entire team (not just one worker) | Infrastructure | Manual-only | Manual-only-comment | Periodic | Requires verifying an external monitor/kill-switch exists at the team/orchestration level and is operated independently — not detectable from source alone; verify against declared team tier (agent-team.3.1) | Missing |
| agent-team.10.2 | Tier 3 team-level aggregate actions (irreversible/high-impact results assembled from multiple worker outputs) require a human approval checkpoint before the orchestrator commits the aggregate action | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: for teams declared Tier 3, flag orchestrator aggregation/commit call sites with no preceding approval-gate call (parallels agent.7.5 at the team level) | Missing |
| agent-team.10.3 | Audit trail is continuous and cross-worker — a single task ID must be traceable through every worker/handoff/aggregation step, not just within one worker's own logs | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: cross-check that the task-ID propagation validated in agent-team.5.2 appears in every worker's own logging calls (per agent.9), not only in orchestrator-level logs | Missing |

---

### Section 11 — Testing

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| agent-team.11.1 | Team integration tests exist covering orchestrator routing, aggregation, and failure paths with mock worker agents | Procedural | Heuristic | Warning | Every-Commit | File-pairing heuristic: flag orchestrator source changes with no corresponding integration-test file touched | Missing |
| agent-team.11.2 | End-to-end scenario test suite exists, run with mocked/recorded model responses (no live model calls in CI) | Procedural | Heuristic | Warning | Every-Commit | File-pairing heuristic: flag team-source changes with no corresponding end-to-end scenario test touched; text-scan test files for live-API call patterns | Missing |
| agent-team.11.3 | Chaos tests exist for worker timeout, malformed output, and error response failure modes | Procedural | Manual-only | Manual-only-comment | Every-Commit | Requires judgment about chaos-test *coverage quality* across failure modes, not just file presence | Missing |
| agent-team.11.4 | Tier-3 teams have a test verifying the orchestrator-level aggregate-action checkpoint actually pauses execution without simulated approval | Procedural | Heuristic | Warning | Every-Commit | Test-pairing heuristic: for teams declared Tier 3, flag missing test file/test method matching the checkpoint gate identified in agent-team.10.2 | Missing |

---

### Section 12 — Compliance Verification (Checklist)

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| agent-team.12 | Compliance checklist — restates Sections 2–11 as a checklist; not independently checkable beyond its constituent rules | Procedural | Manual-only | Manual-only-comment | Every-Commit | Duplicate coverage of Sections 2–11; no new detection needed | Missing |

---

### Section 13 — Governance (Excluded)

`agent-team.13` (ownership/PR-approval boilerplate) is excluded — not code-checkable, procedural ownership statement only.

---

**Notes:**
1. Applicability is entirely capability-flag gated (`agent-team.file`); today it resolves to "No — excluded for all repositories" per `StandardsCapabilityFlags.csv`.
2. This file extends `GlobalAgentStandards.md` — a repository with the `agent-team.file` flag enabled must also satisfy every row in `CoverageMatrix-GlobalAgentStandards.md` for each individual agent in the team. The two matrices are complementary, not overlapping duplicates.
3. agent-team.4.2 and agent-team.5.3 both describe "no peer-to-peer worker communication" from two angles (role definition vs. communication protocol) — flagged as a duplicate-coverage note rather than merged, since both markers exist in the source document (same pattern as logging.2.6/logging.3.1 in the logging matrix).
4. agent-team.6.3 explicitly reuses `GlobalAgentStandards.md` Section 8.2's output-validation rule — detection technique should share implementation once both are built.
5. agent-team.8.1/8.2/8.3 (Authorization and Security) explicitly parallel `GlobalSecurityStandards.md`'s defense-in-depth principle — worth cross-checking against that file's matrix once built for shared detection opportunities.
6. The new Section 3 (Team Risk Aggregation) and Section 10 (Team-Level RAI Safeguards) rows are, like their `GlobalAgentStandards.md` counterparts, skewed toward Manual-only/Periodic (agent-team.3.2, agent-team.10.1) because they concern emergent/composed behavior and independently-operated oversight infrastructure — not source-pattern-detectable. See `Docs/Working/ToolingRoutingMatrix.md` for the routing rationale.
7. agent-team.3.1 depends on `GlobalAgentStandards.md`'s agent.3.1 (individual tier declaration) already being satisfied for every member agent — this is the one place team-level detection is a strict superset/aggregation of the individual-agent matrix rather than a new independent check.
