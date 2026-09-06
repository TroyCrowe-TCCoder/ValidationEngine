# Agent Team Standards

**Version:** 2.0.0
**Status:** Draft
**Applies To:** All repositories under the GlobalStandards governance model that implement multi-agent systems
**Audience:** AI models and human developers
**Created:** 2026-07-12
**Last Modified:** 2026-07-13

---
<!-- STD-MARKER: agent-team.file -->


## 1. Purpose
<!-- STD-MARKER: agent-team.1 -->

This document defines the rules for designing, implementing, and governing multi-agent teams — systems where two or more AI agents cooperate, delegate, and communicate to accomplish a task that exceeds the scope or capability of a single agent. It extends `GlobalAgentStandards.md`, which defines the rules for individual agents. All individual agent rules apply to agents participating in a team, including the risk-tiering and Responsible AI safeguard rules in `GlobalAgentStandards.md` Sections 3 and 7.

This version supersedes v1.0.0. v1.0.0 defined sound orchestration and communication discipline (single orchestrator, typed handoffs, structured failure handling, least privilege) but treated team-level risk as fully covered by the sum of individual agent rules. In practice, a team introduces risk that does not exist at the single-agent level — no individual agent may cross a dangerous threshold, but the team's aggregate/orchestrated behavior can. v2.0.0 adds Section 3 (Team Risk Aggregation and Autonomy Tiering) and Section 10 (Team-Level Responsible AI Safeguards), both derived from the same Microsoft Responsible AI guidance referenced in `GlobalAgentStandards.md` v2.0.0. All other sections are retained from v1.0.0, renumbered to accommodate the new material and to keep cross-references to `GlobalAgentStandards.md`'s v2.0.0 section numbers correct. See [`GlobalAgentTeamStandards.readme.md`](Readme/GlobalAgentTeamStandards.readme.md) for the rationale behind this restructure.

---

## 2. When to Use a Multi-Agent Team
<!-- STD-MARKER: agent-team.2 -->

A multi-agent team is justified when:

- The task requires specialized expertise across multiple domains that cannot be encoded into a single system prompt without degrading performance.
- The task has parallel workstreams that can be executed concurrently by independent agents.
- A single agent's context window would be exhausted by the full scope of the task.
- Independent verification of an output by a second agent reduces risk of undetected error.

Do not create a multi-agent team when a single agent with well-designed tools can accomplish the task. Team overhead — routing, serialization, error propagation, observability — has a cost. Apply it only when justified. A team also carries additional governance overhead per [Section 3](#3-team-risk-aggregation-and-autonomy-tiering) below; this is a further reason not to form a team unless the task genuinely requires it.

---

## 3. Team Risk Aggregation and Autonomy Tiering
<!-- STD-MARKER: agent-team.3 -->

This section is new in v2.0.0. It closes the gap where a team of individually low-risk agents could collectively perform a high-risk action that no single agent's tier would have flagged.

### 3.1 — Team Tier Is the Maximum of Its Members
<!-- STD-MARKER: agent-team.3.1 -->

A team's risk tier, per `GlobalAgentStandards.md` Section 3.1, is at minimum the highest tier of any member agent (orchestrator, worker, or reviewer). If any single worker is Tier 3, the team as a whole must satisfy Tier 3 Responsible AI safeguards even if the orchestrator and other workers are individually Tier 1 or 2.

### 3.2 — Emergent Autonomy Escalation
<!-- STD-MARKER: agent-team.3.2 -->

Independently of member tiers, a team must be re-evaluated as Tier 3 if the orchestrator can chain multiple Tier 1/2 worker actions into a multi-step sequence with a side effect that no single member's declared tier accounted for (for example, an orchestrator that combines a "read customer record" retrieval worker with a "send email" task worker to autonomously message customers based on retrieved data, across multiple turns, without a human checkpoint). This is an explicit instance of the general principle in `GlobalAgentStandards.md` Section 3.2: an agent (or team) must not be under-tiered relative to what it can actually do.

### 3.3 — Declared Team Tier
<!-- STD-MARKER: agent-team.3.3 -->

The orchestrator's system prompt and the team's repository documentation must declare the team's overall risk tier, computed per [Sections 3.1](#31--team-tier-is-the-maximum-of-its-members) and [3.2](#32--emergent-autonomy-escalation), in addition to each member agent's individual tier.

---

## 4. Team Roles
<!-- STD-MARKER: agent-team.4 -->

Every agent team has exactly one orchestrator and one or more worker agents.

### 4.1 — Orchestrator
<!-- STD-MARKER: agent-team.4.1 -->

The orchestrator is responsible for:

- Decomposing the top-level task into subtasks.
- Assigning subtasks to the appropriate worker agents.
- Aggregating worker outputs.
- Handling worker failures and deciding whether to retry, escalate, or abort.
- Producing the final output to the caller.

The orchestrator does not perform domain work itself. It plans, delegates, and synthesizes. An orchestrator that also does domain work is a design smell that requires the team to be restructured.

### 4.2 — Worker Agents
<!-- STD-MARKER: agent-team.4.2 -->

Worker agents:

- Accept a well-defined subtask from the orchestrator.
- Execute that subtask using their tools and capabilities.
- Return a structured result to the orchestrator.
- Do not communicate with other worker agents directly. All inter-agent communication flows through the orchestrator.

Worker agents must comply with all rules in `GlobalAgentStandards.md`, including single responsibility, risk tiering, tool safety, and output validation.

### 4.3 — Reviewer Agent (Optional)
<!-- STD-MARKER: agent-team.4.3 -->

A reviewer agent is a specialized worker whose sole task is to evaluate the output of another worker agent for accuracy, safety, or policy compliance. A reviewer does not generate new content — it evaluates and returns a structured verdict: `Approved`, `NeedsRevision`, or `Rejected`, with a reason.

Use a reviewer agent when:

- The primary worker's output will be presented to a user or acted upon without further human review.
- The output affects important state (financial, legal, medical, security-relevant).
- The team's declared tier ([Section 3.3](#33--declared-team-tier)) is 3 — a reviewer is a required, not optional, mitigation at Tier 3 in addition to the human checkpoint required by [Section 10](#10-team-level-responsible-ai-safeguards).

---

## 5. Communication Protocol
<!-- STD-MARKER: agent-team.5 -->

### 5.1 — Structured Messages
<!-- STD-MARKER: agent-team.5.1 -->

All messages exchanged between agents must be structured. Do not pass raw natural-language strings between agents unless the receiving agent's sole purpose is to interpret natural language. Use typed message schemas for all handoffs.

```csharp
public record AgentHandoff<T>(
    string TaskId,
    string FromAgent,
    string ToAgent,
    T Payload,
    DateTimeOffset IssuedAt);
```

### 5.2 — Task Identifiers
<!-- STD-MARKER: agent-team.5.2 -->

Every top-level task assigned to the orchestrator must have a unique task identifier. That identifier must be propagated to all subtask assignments and all log entries generated during that task's execution. Use the correlation ID rules from `GlobalLoggingStandards.md` — the task identifier plays the same role as the request correlation ID.

### 5.3 — No Peer-to-Peer Communication
<!-- STD-MARKER: agent-team.5.3 -->

Worker agents do not communicate with each other directly. All information sharing between workers is routed through the orchestrator. This constraint keeps the communication topology simple, makes the orchestrator the single source of truth for task state, and prevents circular communication paths.

---

## 6. Handoff Rules
<!-- STD-MARKER: agent-team.6 -->

### 6.1 — Explicit Handoff Contracts
<!-- STD-MARKER: agent-team.6.1 -->

Every handoff from the orchestrator to a worker must be explicit. The orchestrator must:

- Identify the specific worker being invoked.
- Provide the complete context the worker needs to execute its subtask. Do not assume the worker has memory of prior conversation turns.
- Specify the expected output format.

### 6.2 — Context Completeness
<!-- STD-MARKER: agent-team.6.2 -->

Workers are stateless between invocations. Each handoff must include all context the worker needs in that single message. Do not rely on shared state between invocations.

### 6.3 — Handoff Validation
<!-- STD-MARKER: agent-team.6.3 -->

The orchestrator must validate the worker's response before incorporating it into the task state or passing it to another worker. Use the same output validation rules from `GlobalAgentStandards.md` Section 8.2.

---

## 7. Failure Handling
<!-- STD-MARKER: agent-team.7 -->

### 7.1 — Retry Policy
<!-- STD-MARKER: agent-team.7.1 -->

When a worker returns an error or a malformed response, the orchestrator may retry the subtask once with a corrected prompt that includes the error detail. After two consecutive failures on the same subtask, the orchestrator must escalate rather than continue retrying.

### 7.2 — Escalation
<!-- STD-MARKER: agent-team.7.2 -->

Escalation means the orchestrator returns a structured failure response to its caller, identifying:

- Which subtask failed.
- The worker that was assigned to it.
- The error or malformed output that caused the failure.
- Whether the overall task result is partially complete or entirely invalid.

Do not silently absorb worker failures and return a partial result as if it were complete.

### 7.3 — Partial Results
<!-- STD-MARKER: agent-team.7.3 -->

If a task can be meaningfully completed without the output of a failed subtask, the orchestrator may return a partial result. Partial results must be explicitly marked as partial in the response, with a clear indication of which component is missing and why.

### 7.4 — Timeout
<!-- STD-MARKER: agent-team.7.4 -->

Every subtask handoff must have an explicit timeout. The orchestrator must not wait indefinitely for a worker response. If the timeout elapses, treat it as an error and apply the retry/escalation policy.

---

## 8. Authorization and Security
<!-- STD-MARKER: agent-team.8 -->

### 8.1 — Least Privilege per Agent
<!-- STD-MARKER: agent-team.8.1 -->

Each agent in the team must have access only to the tools and data it needs to execute its assigned subtasks. Do not grant all agents in a team the same permissions as the orchestrator.

### 8.2 — No Trust Escalation Between Agents
<!-- STD-MARKER: agent-team.8.2 -->

An agent does not gain elevated permissions by receiving a message from another agent. Tool calls made by a worker agent are subject to the same authorization rules as if the user had made the request directly. The orchestrator cannot grant a worker agent permissions that the original user does not have.

### 8.3 — Input Validation Across Boundaries
<!-- STD-MARKER: agent-team.8.3 -->

Validate all inputs at every agent boundary. A worker agent must not trust that the orchestrator has pre-validated its inputs. Each agent validates its own inputs independently. This is the same defense-in-depth principle applied to standard service boundaries in `GlobalSecurityStandards.md`, and the same principle as the data ingress/egress control rule in `GlobalAgentStandards.md` Section 7.1 applied at each internal team boundary, not just the team's external boundary.

---

## 9. Observability
<!-- STD-MARKER: agent-team.9 -->

In addition to the per-agent observability rules in `GlobalAgentStandards.md` Section 9, multi-agent teams must log:

- Every handoff: from agent, to agent, task ID, payload size (not payload content unless in a PII-safe context), timestamp.
- Every aggregation step: which worker results were combined, how many workers were invoked, how many succeeded and failed.
- End-to-end task duration: from the orchestrator receiving the top-level task to producing the final output.
- The team's declared risk tier ([Section 3.3](#33--declared-team-tier)) as trace/telemetry metadata, alongside each member agent's individual tier.

Build a trace that allows an operator to reconstruct the full execution path of any task from its task ID alone.

---

## 10. Team-Level Responsible AI Safeguards
<!-- STD-MARKER: agent-team.10 -->

This section is new in v2.0.0. It supplements the per-agent safeguards in `GlobalAgentStandards.md` Section 7 with safeguards that only make sense at the team/orchestration level, per the team's declared tier ([Section 3.3](#33--declared-team-tier)).

### 10.1 — Team-Level Independent Oversight
<!-- STD-MARKER: agent-team.10.1 -->

Required for Tier 2 and Tier 3 teams. The independent oversight/circuit-breaker mechanism required by `GlobalAgentStandards.md` Section 7.3 must be capable of halting the **orchestrator**, not just an individual worker — pausing one worker while the orchestrator continues retrying or reassigning is not sufficient. Document how the kill switch stops the orchestrator's ability to issue new handoffs.

### 10.2 — Aggregate Action Checkpointing
<!-- STD-MARKER: agent-team.10.2 -->

Required for Tier 3 teams. When the orchestrator's aggregation of worker outputs results in an irreversible or high-impact action (per `GlobalAgentStandards.md` Section 7.5), the human approval checkpoint must present the aggregated action and its full provenance — which workers contributed which parts of the decision — not just the orchestrator's final summary. This is what distinguishes team-level checkpointing from single-agent checkpointing: the human reviewer needs to see the composed chain of reasoning, not a black-box result.

### 10.3 — Cross-Agent Audit Trail Continuity
<!-- STD-MARKER: agent-team.10.3 -->

The auditability requirement in `GlobalAgentStandards.md` Section 7.2 extends across the whole team: an operator must be able to reconstruct, from the task ID alone, the complete chain of which agent decided what, in what order, and why, spanning the orchestrator and every worker involved — not just each agent's own local audit log in isolation. This is a stronger requirement than [Section 9](#9-observability)'s general logging rule; it specifically demands that individual agent logs be correlatable into one coherent trail.

---

## 11. Testing
<!-- STD-MARKER: agent-team.11 -->

In addition to the per-agent testing rules in `GlobalAgentStandards.md` Section 11, multi-agent teams require:

### 11.1 — Team Integration Tests
<!-- STD-MARKER: agent-team.11.1 -->

Test the orchestrator's routing logic with mock worker agents that return controlled responses. Verify that:

- Subtask assignments route to the correct workers.
- Worker outputs are correctly aggregated.
- Failure paths trigger the correct retry and escalation behavior.

### 11.2 — End-to-End Scenario Tests
<!-- STD-MARKER: agent-team.11.2 -->

Maintain a suite of representative end-to-end scenarios. Run these against the full team with real model calls disabled (use recorded/mocked model responses) to verify the team's behavior on known inputs without incurring model API cost per CI run.

### 11.3 — Chaos Tests
<!-- STD-MARKER: agent-team.11.3 -->

Introduce deliberate failures in worker mock responses (timeout, malformed output, error response) and verify that the orchestrator handles each failure mode according to the failure handling rules in Section 7.

### 11.4 — Team Tier-3 Checkpoint Tests
<!-- STD-MARKER: agent-team.11.4 -->

For any team declared Tier 3 ([Section 3.3](#33--declared-team-tier)), maintain a test verifying that the aggregate action checkpoint in [Section 10.2](#102--aggregate-action-checkpointing) pauses execution, presents full provenance across contributing workers, and does not proceed without a simulated approval.

---

## 12. Compliance Verification
<!-- STD-MARKER: agent-team.12 -->

- [ ] Team has exactly one orchestrator and one or more workers.
- [ ] Team's overall risk tier is declared and computed as the maximum of member tiers, including emergent-autonomy escalation.
- [ ] Orchestrator only plans, delegates, and synthesizes — it does not perform domain work.
- [ ] All inter-agent messages use typed structured schemas.
- [ ] Every top-level task has a unique task identifier propagated to all subtasks and log entries.
- [ ] Worker agents do not communicate with each other directly.
- [ ] Every handoff includes the complete context the worker needs.
- [ ] Orchestrator validates every worker response before using it.
- [ ] Retry policy limits retries to one before escalating.
- [ ] Escalation produces a structured failure response with subtask, worker, and error detail.
- [ ] Every subtask handoff has an explicit timeout.
- [ ] Each agent has least-privilege tool and data access.
- [ ] No trust escalation occurs across agent boundaries.
- [ ] Inputs are validated at every agent boundary independently.
- [ ] Team logs every handoff and aggregation step with the task identifier and declared risk tier.
- [ ] An independent oversight mechanism can halt the orchestrator itself, not just individual workers (Tier 2/3).
- [ ] Aggregate action checkpoints present full cross-worker provenance before a human approves (Tier 3).
- [ ] The audit trail is correlatable across the whole team from the task ID alone.
- [ ] A reviewer agent is present for Tier 3 teams, in addition to the human checkpoint.
- [ ] Team integration tests exist for routing, aggregation, and failure paths.
- [ ] End-to-end scenario tests exist with mock model responses.
- [ ] Chaos tests exist for worker timeout, malformed output, and error responses.
- [ ] Team Tier-3 checkpoint tests exist and verify execution pauses with full provenance.

---

## 13. Governance
<!-- STD-MARKER: agent-team.13 -->

This standard is owned by Troy Crowe. No changes to this file may be merged without Troy Crowe's explicit approval. Changes must be submitted as a pull request that includes a rationale comment explaining the reason for the update or deviation. Direct commits to `dev` or `main` are not permitted.
