# Agent Standards

**Version:** 2.0.0
**Status:** Draft
**Applies To:** All repositories under the GlobalStandards governance model that implement custom AI agents
**Audience:** AI models and human developers
**Created:** 2026-07-12
**Last Modified:** 2026-07-13

---
<!-- STD-MARKER: agent.file -->


## 1. Purpose
<!-- STD-MARKER: agent.1 -->

This document defines the rules for designing, implementing, testing, and governing custom AI agents built within repositories under the GlobalStandards governance model. An agent, for the purposes of this document, is any component that accepts a natural-language or structured input, uses a language model to reason about it, may invoke tools or external services, and produces an output or takes an action on behalf of a user or an automated system.

These rules apply whether the agent is a standalone service, an embedded component in a larger application, or a participant in a multi-agent team. Multi-agent team-specific rules are in `GlobalAgentTeamStandards.md`.

This version supersedes v1.0.0. v1.0.0 defined sound software-engineering discipline for agents (single responsibility, typed tool schemas, structured errors, observability, testing) but omitted an explicit risk/autonomy grounding and Responsible AI safeguards. v2.0.0 adds Section 3 (Risk Classification and Autonomy Tiering) and Section 7 (Responsible AI Safeguards), both derived from Microsoft's published Responsible AI guidance for agentic systems ([Responsible AI in Azure workloads](https://learn.microsoft.com/azure/well-architected/ai/responsible-ai), [Responsible AI for agent design](https://learn.microsoft.com/agents/design-guidelines/responsible-ai), [AI governance and security maturity model](https://learn.microsoft.com/agents/adoption-maturity-model/maturity-model-security-governance)). All other sections are retained from v1.0.0, renumbered to accommodate the new material. See [`GlobalAgentStandards.readme.md`](Readme/GlobalAgentStandards.readme.md) for the rationale behind this restructure.

---

## 2. Agent Design Principles
<!-- STD-MARKER: agent.2 -->

### 2.1 — Single Responsibility
<!-- STD-MARKER: agent.2.1 -->

Each agent has one well-defined responsibility, scoped to a single dev-purpose category (e.g., API, Database, UI, Service, Repository, Pipeline Management). An agent scoped to one dev-purpose category does not also perform work belonging to another category — for example, a database-focused agent does not also generate UI, and a pipeline-management agent does not also make billing decisions. When a task requires crossing into another dev-purpose category, the agent delegates to a specialized agent or service.

### 2.2 — Explicit Boundaries
<!-- STD-MARKER: agent.2.2 -->

Every agent must have an explicit definition of:

- What inputs it accepts.
- What outputs it produces.
- What tools it may invoke.
- What it will refuse to do.

See [`GlobalAgentStandards.readme.md#agent22`](Readme/GlobalAgentStandards.readme.md#agent22).

These boundaries must be documented in the agent's system prompt and in the repository documentation. Undocumented agent behavior is a governance violation.

### 2.3 — Predictability Over Cleverness
<!-- STD-MARKER: agent.2.3 -->

An agent must produce consistent behavior for a given class of input within its documented boundaries (see [Section 2.2](#22--explicit-boundaries)); the same class of input must not yield materially different actions or outputs across runs. An agent must not implement undocumented fallback or "best effort" behavior for input outside its documented boundaries. When input falls outside the documented boundaries, the agent must fail explicitly with a documented refusal or error rather than attempting an undocumented alternative behavior.

---

## 3. Risk Classification and Autonomy Tiering
<!-- STD-MARKER: agent.3 -->

This section is new in v2.0.0. It requires every agent to be classified into a risk tier before design proceeds. The tier determines which Responsible AI safeguards in [Section 7](#7-responsible-ai-safeguards) are mandatory. This model is adapted directly from Microsoft's published agent-complexity risk model for agentic AI systems.

### 3.1 — Mandatory Tier Declaration
<!-- STD-MARKER: agent.3.1 -->

Every agent must declare its risk tier in its system prompt header comment (alongside the version, per [Section 4.2](#42--system-prompt-versioning)) and in its repository documentation. An agent without a declared tier is non-compliant and must not be deployed. The tier is one of:

| Tier | Name | Definition | Examples |
|---|---|---|---|
| **1** | **Retrieval (read-only)** | The agent only reads/retrieves data and returns information. It cannot create, modify, delete, or trigger any action with a side effect. | A documentation-lookup agent; a status-reporting agent. |
| **2** | **Task-based (read and write)** | The agent can perform actions with side effects (create, update, delete, send, trigger) but each action is scoped, reversible, or low-blast-radius, and the agent does not operate autonomously across multiple unsupervised turns. | An agent that files a bug ticket; an agent that updates a single record on explicit request. |
| **3** | **Fully autonomous (multi-turn)** | The agent plans and executes multi-step sequences of actions with side effects across multiple turns without a human approving each step, and/or its actions are difficult or impossible to reverse, and/or it operates on sensitive data classes (financial, legal, medical, security-relevant, PII at scale). | An agent that autonomously reconciles financial records and posts corrections; an agent that manages infrastructure deployments end-to-end. |

### 3.2 — Tier Determines Required Safeguards
<!-- STD-MARKER: agent.3.2 -->

Tier 1 agents must satisfy the baseline rules in this document ([Sections 2](#2-agent-design-principles), [4](#4-system-prompt-design), [5](#5-tool-design), [6](#6-model-selection-and-configuration)) plus audit logging ([Section 9](#9-observability)). Tier 2 agents must additionally satisfy the full Responsible AI Safeguards in [Section 7](#7-responsible-ai-safeguards) at the "standard" level. Tier 3 agents must satisfy Section 7 at the "elevated" level, which includes mandatory independent oversight, a human-accessible kill switch, and a pre-action checkpoint for any irreversible or high-impact operation. An agent must not be assigned a lower tier than its actual capabilities warrant in order to avoid the corresponding safeguards — this is a governance violation subject to the same review as any other undocumented behavior.

### 3.3 — Re-tiering on Capability Change
<!-- STD-MARKER: agent.3.3 -->

Adding a new tool, expanding a tool's scope, or changing the model's autonomy configuration (e.g., increasing the number of unsupervised turns) requires re-evaluating the agent's tier before the change is merged. A tier change is a breaking governance change and must be called out explicitly in the pull request description.

---

## 4. System Prompt Design
<!-- STD-MARKER: agent.4 -->

### 4.1 — Required System Prompt Sections
<!-- STD-MARKER: agent.4.1 -->

Every agent must have a system prompt that includes the following sections in order:

1. **Identity** — One sentence describing what this agent is and what it does.
2. **Scope** — What the agent is responsible for and what it explicitly is not responsible for.
3. **Behavior rules** — Numbered, specific behavioral directives. Each rule governs one behavior.
4. **Output format** — The required structure of every response. Specify format, schema, or examples.
5. **Refusal rules** — Explicit conditions under which the agent must decline to act and what response it must produce when declining.

### 4.2 — System Prompt Versioning
<!-- STD-MARKER: agent.4.2 -->

System prompts are code. Store them in version-controlled files (`.txt`, `.md`, or embedded in the agent's configuration class). Do not hardcode system prompts as string literals inside business logic methods.

Name prompt files after the agent and include a version comment at the top, including the risk tier declared in [Section 3.1](#31--mandatory-tier-declaration):

```
# DocumentAnalysisAgent System Prompt
# Version: 1.2.0
# Risk Tier: 1 (Retrieval)
# Last Modified: 2026-07-12
```

### 4.3 — No Jailbreak Surface
<!-- STD-MARKER: agent.4.3 -->

System prompts must not include instructions that could be rephrased by a user prompt to override the agent's behavior. Do not include phrases like "ignore previous instructions" or "you can also do X if asked." Test prompts against known jailbreak patterns before deployment.

---

## 5. Tool Design
<!-- STD-MARKER: agent.5 -->

### 5.1 — One Tool, One Operation
<!-- STD-MARKER: agent.5.1 -->

Each tool performs exactly one operation. A tool that retrieves a document is not also responsible for scanning it. Compound operations are composed at the agent orchestration layer, not inside a single tool.

### 5.2 — Tool Schema
<!-- STD-MARKER: agent.5.2 -->

Every tool must declare a complete, typed schema for its inputs and outputs. Schemas must include:

- A name in `snake_case`.
- A description that a language model can use to decide whether to invoke the tool.
- Typed parameter definitions with descriptions for every parameter.
- Explicit indication of which parameters are required.

```json
{
  "name": "get_document_metadata",
  "description": "Retrieves metadata for a document by its ID. Use this when the user asks about a specific document's properties, upload date, or status.",
  "parameters": {
    "type": "object",
    "properties": {
      "document_id": {
        "type": "string",
        "description": "The unique identifier of the document."
      }
    },
    "required": ["document_id"]
  }
}
```

### 5.3 — Tool Safety
<!-- STD-MARKER: agent.5.3 -->

Tools that perform write operations (create, update, delete, send message, trigger workflow) must:

- Require explicit confirmation from the agent's reasoning layer before execution, or be invocable only from agent steps explicitly designated as write operations.
- Log every invocation with the invoking agent identity, input parameters, timestamp, and outcome.
- Be guarded by the same authorization rules as the equivalent HTTP endpoint. An agent tool that updates a document must enforce the same client/tenant isolation as the `PUT /documents/{id}` endpoint.

### 5.4 — Tool Error Handling
<!-- STD-MARKER: agent.5.4 -->

Tools must return structured error responses, not throw unhandled exceptions to the agent. The agent needs to reason about failure — it cannot do that with a raw exception.

```csharp
public record ToolResult<T>(bool Success, T? Data, string? Error);
```

---

## 6. Model Selection and Configuration
<!-- STD-MARKER: agent.6 -->

### 6.1 — Model Declaration
<!-- STD-MARKER: agent.6.1 -->

Every agent must declare which model it uses in its configuration. Do not select a model at runtime without a documented selection strategy.

### 6.2 — Temperature
<!-- STD-MARKER: agent.6.2 -->

- For deterministic tasks (data extraction, classification, structured output): `temperature = 0`.
- For tasks that benefit from slight variation (summarization, drafting): `temperature = 0.3` to `0.5`.
- For creative tasks: `temperature` above `0.7` requires documented justification.

Do not use the model default without explicitly setting temperature. Undeclared temperature is an undeclared behavior contract.

### 6.3 — Token Limits
<!-- STD-MARKER: agent.6.3 -->

Set `max_tokens` on every completion call. Do not allow unbounded completion lengths. If the agent's output is unexpectedly long, the model is likely hallucinating or reasoning in circles. A token cap forces the agent to be concise and provides a safety boundary.

---

## 7. Responsible AI Safeguards
<!-- STD-MARKER: agent.7 -->

This section is new in v2.0.0. It is the direct resolution of the STD-010 backlog gap: agentic AI systems carry risks beyond traditional software (autonomous decision-making, data flowing across trust boundaries, difficult-to-reverse actions) and require safeguards beyond code review and unit tests. This section is grounded in the Microsoft Responsible AI framework for agentic systems: robust data ingress/egress control, data validation and integrity assurance, and autonomous execution with independent guardrails.

Required safeguard level scales with the [risk tier](#3-risk-classification-and-autonomy-tiering) declared in Section 3: Tier 1 requires 7.1—7.2 only; Tier 2 requires 7.1—7.4; Tier 3 requires all of 7.1—7.6.

### 7.1 — Data Ingress and Egress Control
<!-- STD-MARKER: agent.7.1 -->

Every agent must document what external data sources it reads from and what external systems it can write to or send data to. Data crossing this boundary must be validated at the boundary (schema, size, content type) before being passed to the model or a tool, and before being sent to an external system. This extends the input/output validation rules in [Section 8](#8-input-and-output-validation) with an explicit boundary-mapping requirement: an agent's documented tool list ([Section 2.2](#22--explicit-boundaries)) is also its data-boundary map.

### 7.2 — Auditability of Agent Actions
<!-- STD-MARKER: agent.7.2 -->

Every action an agent takes with a side effect (per the tool-safety logging rule in [Section 5.3](#53--tool-safety)) must be reconstructable after the fact: who/what triggered the agent, what the agent decided, what tool it invoked, and what the outcome was. This is the audit-trail requirement referenced by [Section 9](#9-observability); it is called out here as a Responsible AI requirement, not merely an operational one, because auditability is what allows a human to review and contest an autonomous decision after it has occurred.

### 7.3 — Independent Oversight (Circuit Breaker)
<!-- STD-MARKER: agent.7.3 -->

Required for Tier 2 and Tier 3 agents. There must be a monitoring or control mechanism that can halt or pause the agent's execution and that is **not implemented by the agent itself** — an agent cannot be relied upon to reliably detect and stop its own malfunction. This may be an external health check with an automatic disable switch, a rate/anomaly monitor wired to an operational alert, or a manual kill switch exposed to an operator. Document where this mechanism lives and who can trigger it.

### 7.4 — Transparency and Disclosure
<!-- STD-MARKER: agent.7.4 -->

Required for Tier 2 and Tier 3 agents. Users interacting with an agent must be able to tell they are interacting with an AI system rather than a human, unless the agent is a purely internal/backend automation with no user-facing conversational surface. When an agent's output drives a decision that materially affects a user (approval/denial, financial calculation, escalation), the agent must be able to produce an explanation of the basis for that decision on request — this does not require exposing raw model reasoning, but does require the decision to be traceable to specific inputs and rule/tool outcomes captured under [Section 7.2](#72--auditability-of-agent-actions).

### 7.5 — Human Override and Checkpoints
<!-- STD-MARKER: agent.7.5 -->

Required for Tier 3 agents. Any action that is irreversible or high-impact (financial transaction above a documented threshold, deletion of production data, external communication sent on the organization's behalf, infrastructure changes) must pause for an explicit human approval checkpoint before execution, regardless of how confident the agent's reasoning appears. The checkpoint must present the pending action and its basis, not merely a generic confirmation prompt.

### 7.6 — Fairness and Consistency Review
<!-- STD-MARKER: agent.7.6 -->

Required for Tier 3 agents. If an agent's decisions affect different users differently based on inputs correlated with protected or sensitive characteristics (even indirectly, e.g., through historical data patterns), the agent's decision logic must be periodically reviewed for unintended bias. This does not require a formal fairness audit for every agent, but does require the review to be scheduled and recorded, not skipped by default.

---

## 8. Input and Output Validation
<!-- STD-MARKER: agent.8 -->

### 8.1 — Input Validation
<!-- STD-MARKER: agent.8.1 -->

Validate all user-supplied inputs before passing them to the model or to any tool. Apply the same input validation rules as the rest of the application (see `GlobalSecurityStandards.md` Section 5). Do not trust model-generated values as safe inputs to tools without validation.

### 8.2 — Output Validation
<!-- STD-MARKER: agent.8.2 -->

When an agent produces structured output (JSON, a classification label, an action decision), validate that the output matches the declared schema before acting on it. Do not assume the model will always produce well-formed output.

Use retry-with-correction on validation failure: feed the validation error back to the model with a correction prompt before escalating to a fallback or error response.

### 8.3 — Prompt Injection Defense
<!-- STD-MARKER: agent.8.3 -->

Treat all externally sourced content (user messages, retrieved documents, API responses) as potentially hostile. Do not allow external content to be injected into the system prompt. Use clear delimiters when passing external content into the conversation and instruct the model explicitly that content within those delimiters is data, not instructions.

---

## 9. Observability
<!-- STD-MARKER: agent.9 -->

Apply the logging rules from `GlobalLoggingStandards.md` to all agent operations:

- Log every model invocation: agent name, model, input token count, output token count, latency, success/failure.
- Log every tool invocation: tool name, input parameters (excluding PII), outcome, latency.
- Attach the correlation identifier to all agent log entries.
- Emit telemetry for model calls and tool calls as external dependency events.
- Record the agent's declared risk tier ([Section 3.1](#31--mandatory-tier-declaration)) as log/telemetry metadata so operational dashboards can be filtered or alerted by tier.

Do not log the full content of user messages or model responses unless the repository is explicitly classified as a PII-safe logging context. Log metadata (lengths, categories, outcomes) rather than content.

---

## 10. Conversation and Memory Management
<!-- STD-MARKER: agent.10 -->

### 10.1 — Context Window Discipline
<!-- STD-MARKER: agent.10.1 -->

Do not accumulate the full conversation history indefinitely. Summarize older turns when the context window is approaching its limit. Retain only the summary and the most recent turns.

### 10.2 — Memory Scope
<!-- STD-MARKER: agent.10.2 -->

Short-term memory (the current conversation turn) lives in the message array. Long-term memory (facts that persist across conversations) must be stored externally in a retrieval system, not in the system prompt. Do not hardcode facts that change over time into the system prompt.

### 10.3 — Stateless Tool Calls
<!-- STD-MARKER: agent.10.3 -->

Tool calls must be stateless with respect to the conversation. A tool does not read from or write to the conversation context. State that needs to persist beyond a tool call is returned in the tool response and managed by the agent's orchestration layer.

---

## 11. Testing
<!-- STD-MARKER: agent.11 -->

### 11.1 — Unit Tests for Tools
<!-- STD-MARKER: agent.11.1 -->

Every tool function must have unit tests covering the success path, error paths, and boundary conditions. Tool tests must not call live model APIs.

### 11.2 — Prompt Regression Tests
<!-- STD-MARKER: agent.11.2 -->

Maintain a suite of prompt regression tests that verify the agent's behavior on a fixed set of representative inputs. Run these tests before any change to the system prompt, model version, or tool schema. Record expected output categories, not exact output strings (model output is non-deterministic).

### 11.3 — Adversarial Prompt Tests
<!-- STD-MARKER: agent.11.3 -->

Include adversarial test cases:

- Attempts to override system instructions.
- Requests outside the declared scope.
- Malformed tool inputs.
- Inputs that contain prompt injection attempts.

Verify that the agent refuses, falls back, or produces a safe response for each adversarial case.

### 11.4 — Tier-3 Checkpoint Tests
<!-- STD-MARKER: agent.11.4 -->

For any agent declared Tier 3 ([Section 3.1](#31--mandatory-tier-declaration)), maintain a test verifying that the irreversible/high-impact action checkpoint in [Section 7.5](#75--human-override-and-checkpoints) actually pauses execution and does not proceed without a simulated approval.

---

## 12. Compliance Verification
<!-- STD-MARKER: agent.12 -->

- [ ] Agent has a documented single responsibility.
- [ ] Agent has a declared risk tier (1, 2, or 3) recorded in its system prompt and repository documentation.
- [ ] Tier-appropriate Responsible AI safeguards (Section 7) are implemented and documented.
- [ ] System prompt includes Identity, Scope, Behavior Rules, Output Format, and Refusal Rules sections.
- [ ] System prompt is stored in a versioned file, not hardcoded in business logic, and includes the declared risk tier.
- [ ] Each tool performs exactly one operation.
- [ ] Every tool has a complete typed schema with descriptions.
- [ ] Write-operation tools log every invocation and enforce authorization rules.
- [ ] Tools return structured error responses rather than throwing exceptions.
- [ ] Model, temperature, and `max_tokens` are explicitly declared.
- [ ] Data ingress/egress boundaries are documented and validated (Tier 2/3).
- [ ] An independent oversight/circuit-breaker mechanism exists and is documented (Tier 2/3).
- [ ] Users can tell they are interacting with an AI agent; decisions are explainable on request (Tier 2/3).
- [ ] Irreversible/high-impact actions require a human approval checkpoint (Tier 3).
- [ ] A fairness/consistency review is scheduled for decisions affecting users differently (Tier 3).
- [ ] All agent inputs are validated before model or tool invocation.
- [ ] All structured agent outputs are validated against the declared schema.
- [ ] External content is delimited and not injected into the system prompt.
- [ ] Model invocations and tool calls are logged with the correlation identifier and risk tier.
- [ ] Unit tests exist for all tool functions.
- [ ] Prompt regression tests exist and run before system prompt changes.
- [ ] Adversarial prompt tests are included in the test suite.
- [ ] Tier-3 checkpoint tests exist and verify execution pauses for approval.

---

## 13. Governance
<!-- STD-MARKER: agent.13 -->

This standard is owned by Troy Crowe. No changes to this file may be merged without Troy Crowe's explicit approval. Changes must be submitted as a pull request that includes a rationale comment explaining the reason for the update or deviation. Direct commits to `dev` or `main` are not permitted.
