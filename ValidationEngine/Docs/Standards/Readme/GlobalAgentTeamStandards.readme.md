# GlobalAgentTeamStandards.md — Rationale and Context

This file documents the rationale, history, and decision context for rules in [`GlobalAgentTeamStandards.md`](../GlobalAgentTeamStandards.md). Per [`GlobalFileSpecificationStandards.md` Section 2.16](../GlobalFileSpecificationStandards.md#216-readme-companion-file), this content must not appear in the standard itself.

---

## v1.0.0 → v2.0.0 Restructure

**Origin:** This standard was reassessed alongside `GlobalAgentStandards.md` under backlog item STD-010. The individual-agent standard's v2.0.0 rebuild introduced risk tiering and Responsible AI safeguards; this file's rebuild asks the follow-on question specific to multi-agent teams: can a team of individually low-risk agents collectively perform an action that no single agent's risk tier would have flagged? The answer is yes — an orchestrator that chains a Tier 1 "read customer data" worker with a Tier 2 "send email" worker across multiple unsupervised turns can produce Tier 3 behavior (autonomous, hard-to-reverse, customer-facing) that neither individual worker's own classification would surface. v1.0.0 had no rule that would have caught this.

**Decision:** Two new sections were added on top of v1.0.0's existing orchestration/communication discipline (which was sound and kept unchanged):

- **Section 3 (Team Risk Aggregation and Autonomy Tiering)** — establishes that a team's risk tier is at minimum the maximum of its members' individual tiers (`GlobalAgentStandards.md` Section 3.1), and adds an explicit "emergent autonomy escalation" rule for the chaining scenario described above, which no per-agent tier could catch in isolation.
- **Section 10 (Team-Level Responsible AI Safeguards)** — extends the per-agent safeguards in `GlobalAgentStandards.md` Section 7 to the orchestration layer specifically: the independent oversight mechanism must be able to halt the orchestrator itself (not just pause one worker while the orchestrator keeps issuing new handoffs), aggregate-action checkpoints must show a human the full cross-worker provenance of a composed decision rather than just the orchestrator's summary, and the audit trail must be correlatable across the entire team from a single task ID.

All subsequent sections from v1.0.0 (Team Roles onward) were renumbered to accommodate these two additions, and every internal cross-reference to `GlobalAgentStandards.md`'s section numbers was updated to match that standard's own v2.0.0 renumbering (for example, the handoff-validation rule now correctly points to `GlobalAgentStandards.md` Section 8.2 for output validation, not the old Section 6.2). This is why the version was bumped to 2.0.0 rather than a minor revision.

## Section 3 — Team Risk Aggregation and Autonomy Tiering (`agent-team.3`)

**Clarification:** "Emergent autonomy escalation" (3.2) is deliberately framed as independent of the max-of-members rule (3.1) because it is possible for every individual member to be correctly tiered and yet the composed behavior to still exceed what any one tier accounted for — the risk lives in the orchestration pattern, not in any single agent's capability list. This is why the rule requires re-evaluating the *team*, not just re-checking each member's own tier.

**Origin:** This is a direct, team-specific extension of the "different types of agents require varying levels of safeguards" principle in [Responsible AI in Azure workloads](https://learn.microsoft.com/azure/well-architected/ai/responsible-ai#implement-agentic-ai-safeguards) — applied to the composition of agents rather than a single agent's own capabilities.

## Section 10 — Team-Level Responsible AI Safeguards (`agent-team.10`)

**Clarification:** Section 10.1 (Team-Level Independent Oversight) is intentionally stricter than the per-agent version in `GlobalAgentStandards.md` Section 7.3: halting one misbehaving worker is not sufficient if the orchestrator can simply reassign the failed subtask to another worker or route around it. The kill switch must be able to stop the orchestrator's ability to issue new handoffs at all.

**Origin:** Derived from the same "autonomous agentic execution with independent guardrails" principle used in `GlobalAgentStandards.md` Section 7.3, applied at the orchestration layer per Microsoft's guidance that oversight mechanisms must "operate independently from the agents themselves" — in a team, that includes the orchestrator, which is itself an agent capable of malfunctioning or being manipulated.
