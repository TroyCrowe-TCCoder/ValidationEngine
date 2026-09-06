# GlobalAgentStandards.md — Rationale and Context

This file documents the rationale, history, and decision context for rules in [`GlobalAgentStandards.md`](../GlobalAgentStandards.md). Per [`GlobalFileSpecificationStandards.md` Section 2.16](../GlobalFileSpecificationStandards.md#216-readme-companion-file), this content must not appear in the standard itself.

---

## v1.0.0 → v2.0.0 Restructure

**Origin:** `GlobalAgentStandards.md` and its sibling `GlobalAgentTeamStandards.md` were paused for review under backlog item STD-010, on the grounds that the reviewer lacked sufficient working knowledge of agentic AI concepts to validate whether the standard was complete. Rather than proceed on assumption, the pause was resolved by first pulling in Microsoft's own published Responsible AI guidance for agentic systems — [Responsible AI in Azure workloads](https://learn.microsoft.com/azure/well-architected/ai/responsible-ai), [Responsible AI for agent design](https://learn.microsoft.com/agents/design-guidelines/responsible-ai), and the [AI governance and security maturity model](https://learn.microsoft.com/agents/adoption-maturity-model/maturity-model-security-governance) — and then re-deriving what was missing from v1.0.0 against that authoritative source, rather than guessing.

**Finding:** v1.0.0 was not wrong — its rules on single responsibility, tool schema, structured error handling, observability, and testing are sound software-engineering discipline and were kept unchanged in substance. What v1.0.0 lacked was any explicit **risk/autonomy grounding**: nothing in v1.0.0 distinguished a harmless read-only lookup agent from an agent capable of autonomously executing irreversible, high-impact actions. Microsoft's guidance is explicit that "different types of agents require varying levels of safeguards because the potential risks and impact of their actions increase with their capabilities and autonomy" — and that agentic systems specifically require independent oversight (a circuit breaker not implemented by the agent itself), robust data ingress/egress control, and human intervention capability, none of which v1.0.0 addressed.

**Decision:** Rather than discard v1.0.0's engineering-discipline rules and start from a blank page, the two missing concerns were added as new, self-contained sections layered on top of the existing structure:

- **Section 3 (Risk Classification and Autonomy Tiering)** — a mandatory 3-tier model (Retrieval / Task-based / Fully autonomous), adapted directly from the tiering used in the Azure Well-Architected Framework's agentic-AI guidance. Every other new rule in Section 7 keys off the tier declared here, so this had to be introduced early in the document (immediately after Agent Design Principles) rather than appended at the end.
- **Section 7 (Responsible AI Safeguards)** — data ingress/egress control, auditability, independent oversight/circuit-breaker, transparency and disclosure, human override checkpoints, and fairness review — mapped one-to-one onto Microsoft's three foundational aspects of agentic Responsible AI (data ingress/egress control; data validation and integrity assurance; autonomous execution with independent guardrails) and onto the "Govern, Map, Measure, Manage" functional model in the agent design guidelines.

All subsequent sections from v1.0.0 (System Prompt Design onward) were renumbered to make room for these two additions — this is also why "Tool Design" moved from Section 4 to Section 5, "Model Selection" from Section 5 to Section 6, and so on through to Governance (now Section 13). No rule content besides the new Sections 3 and 7 was rewritten. This is why the version was bumped to 2.0.0 (structural, not a patch) rather than a minor revision.

## Section 3 — Risk Classification and Autonomy Tiering (`agent.3`)

**Clarification:** The three tiers are deliberately modeled on capability and blast radius, not on subjective judgments of "how important" an agent feels. A Tier 3 classification is triggered by any one of: multi-step unsupervised autonomy, difficult-to-reverse actions, or sensitive data classes — an agent does not need all three to require Tier 3 safeguards.

**Origin:** Directly sourced from the "Agent complexity considerations" model in [Responsible AI in Azure workloads](https://learn.microsoft.com/azure/well-architected/ai/responsible-ai#implement-agentic-ai-safeguards), which defines retrieval agents (read-only), task-based agents (read and write), and fully autonomous agents (multi-turn) as the three complexity classes requiring escalating safeguards.

## Section 7 — Responsible AI Safeguards (`agent.7`)

**Clarification:** The safeguard-by-tier scaling (7.1–7.2 for Tier 1; 7.1–7.4 for Tier 2; all of 7.1–7.6 for Tier 3) is intentional so that a simple retrieval agent is not burdened with human-checkpoint machinery it does not need, while a fully autonomous agent cannot skip any of them. Section 7.3 (Independent Oversight) explicitly requires the halting mechanism to live outside the agent's own code — an agent cannot be trusted to reliably detect and stop its own malfunction, per Microsoft's guidance on "autonomous agentic execution with independent guardrails."

**Origin:** Each rule maps to a specific element of Microsoft's Responsible AI framework for agentic systems: 7.1 to "robust data ingress and egress control," 7.2–7.3 to "autonomous agentic execution with independent guardrails" (auditability plus a circuit breaker independent of the agent), 7.4–7.5 to the transparency/accountability principles in the AI governance maturity model, and 7.6 to the fairness principle in the same source's six Responsible AI principles (fairness, transparency, accountability, reliability and safety, privacy and security, inclusiveness).

---

## Terminology — "Tool" vs. "Resource"

A **tool**, as used throughout this standard, is a discrete, schema-defined function that the agent's language model can choose to invoke (function-calling / tool-calling) — for example, `get_document_metadata` or `scan_document`. It is not the backend system or resource the operation acts on. A **resource** (e.g., a document store, a mail system, a printer, a directory service) is what a tool operates against; the tool is the narrow, single-operation interface into it. One resource may be exposed through several distinct tools, each performing exactly one operation (see [`agent.5.1`](../GlobalAgentStandards.md#51--one-tool-one-operation)).

## agent.2.2

The four required elements (inputs, outputs, tools, refusals) exist so that an agent's contract is not left implicit in its code. Documenting them explicitly in the system prompt and/or repository documentation makes the agent's scope and limits visible to anyone reviewing or maintaining it, without requiring them to trace through the implementation to infer what the agent is and is not supposed to do.

## agent.5.1

"Tool" here means a single function-calling operation exposed to the model (see Terminology above), not the resource it operates against. The rule requires each tool to perform exactly one operation (e.g., `get_document`, `scan_document`) rather than bundling multiple operations into one tool (e.g., a combined `get_and_scan_document`). This mirrors single-responsibility at the tool level: it keeps each tool independently testable, reusable across different agent flows, and separately authorizable/auditable (see `agent.5.3`). If a task needs multiple operations in sequence, that sequencing is composed by the agent's orchestration layer calling multiple single-operation tools — it is not implemented inside one tool.
