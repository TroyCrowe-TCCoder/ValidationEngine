# Coverage Matrix — GlobalLoggingStandards.md

**Status:** Reviewed
**Sign Off:** Troy Crowe
**Sign Off Date:** 2026-08-14
**Revision Date:**
**Columns**

| Column | Meaning |
|---|---|
| STD-MARKER | Rule id from the standards file. |
| Rule Summary | One-line paraphrase of the rule. |
| Category | Structure / Technology / Development / Procedural / **Infrastructure**. |
| Confidence | Binary (deterministic pass/fail) / Heuristic (risk-flag, needs human confirmation) / Manual-only (no automation path). |
| Severity | Hard-stop / Warning / Manual-only-comment (3-tier enforcement model). |
| Frequency | Every-Commit (validated against the files touched in the current change set on every commit/PR) / Periodic (validated on a recurring, **configurable** cadence — default suggested cadence noted per rule, but the actual interval is set by repository/organization configuration, not hardcoded here). |
| Technique | Short detection approach. |
| Status | Missing / Existing / Acknowledged-future-work. |

**Infrastructure category note:** Infrastructure rules validate provisioned cloud/platform state (e.g., a monitoring workspace exists, a retention policy is configured) rather than application code structure. These rules are always **Periodic** frequency — checked on a recurring cadence rather than on every commit/PR, since the underlying state does not change per code change. This is distinct from Structure, which in this matrix refers to the structure of the application code itself, not infrastructure topology.

**Periodic cadence configuration:** The actual interval for Periodic rules (e.g., weekly, monthly, quarterly) must be configurable rather than fixed by this matrix. Each Periodic row below lists a **Suggested Default Cadence**; the enforcing tooling/pipeline must read the real interval from a central configuration source (e.g., a `validation-schedule` setting in the repository or organization-level config) so cadence can be tuned per repository without editing this standard or the validation engine itself.

**System-mode applicability gating vs. manual full run:** When validation runs in **System mode** (staged/committed change set), Frequency does not mean "this rule always executes on every commit." Instead, each rule only executes if the current change set contains a file of the type/area that rule applies to — e.g., an Every-Commit rule scoped to logging-configuration files is skipped entirely if no logging-configuration file is part of the change set, even though the commit itself triggered System mode. Periodic rules follow the same applicability gating in addition to their cadence: they only execute when both their cadence has elapsed AND the change set contains a relevant file type. A full, ungated validation of every rule against the entire repository — regardless of which file types are present — only happens in **Manual mode** (e.g., a scheduled full compliance audit or an on-demand full-solution run), not automatically during System-mode commit/PR validation.

---

### Section 2 — Technology Baseline

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| logging.2.1 | Must use `ILogger<T>`; no `Console.Write`/`Trace`/`Debug`/static facades | Technology | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag `Console.Write*`, `Trace.Write*`, `Debug.Write*`; flag non-`ILogger<T>` logging fields — implemented as `LOG001` (`NoConsoleTraceDebugLoggingAnalyzer`) | Existing |
| logging.2.2 | Application code must use `ILogger<T>` rather than provider-specific APIs | Technology | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag direct provider SDK logging calls (e.g., `TelemetryClient.TrackTrace`) in place of `ILogger<T>` — implemented as `LOG002` (`NoProviderSpecificLoggingApiAnalyzer`) | Existing |
| logging.2.3 | Logging/observability scaffolded behind interfaces, not direct provider SDK calls in app code | Structure | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag direct `Microsoft.ApplicationInsights.*` refs outside `Infrastructure`/`Options` wiring — implemented as `LOG003` (`NoScatteredApplicationInsightsSdkAnalyzer`) | Existing |
| logging.2.4 | `Program.cs` must configure `ILogger<T>` destination (App Insights unless documented alternative) | Structure | Binary | Hard-stop | Every-Commit | Roslyn-syntax: parse `Program.cs` for `AddApplicationInsightsTelemetry` or approved-alternative marker — implemented as `LOG004` (`ProgramLoggingDestinationAnalyzer`) | Existing |
| logging.2.5 | Telemetry must integrate with Azure Monitor / Log Analytics workspace | Infrastructure | Heuristic | Warning | Every-Commit (on `Program.cs`/`appsettings*.json`/IaC changes) | Config/source parse: verify `Program.cs`/`appsettings*.json` configures an Application Insights connection string (duplicates part of logging.2.4 detection); IaC parse: verify Bicep/ARM templates provision a Log Analytics workspace and link the Application Insights resource to it via `WorkspaceResourceId` | Missing |
| logging.2.6 | All log entries must use named properties, not string interpolation | Development | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag `_logger.Log*` calls whose message arg is an interpolated string (duplicates logging.3.1 detection — same technique, two markers) — implemented as `LOG005` (`NoStringInterpolationInLogMessageAnalyzer`) | Existing |

**Notes:** Section 2 was originally one bullet list under a single `logging.2` marker; the standards file has been updated to split it into `logging.2.1`–`logging.2.6` sub-markers so each rule can be independently referenced and tracked. Azure Monitor/Log Analytics (2.5) is source/IaC-diffable via `Program.cs`/`appsettings*.json` connection-string presence and Bicep/ARM `WorkspaceResourceId` linkage, reclassified from Manual-only to Heuristic. Note logging.2.6 and logging.3.1 describe the same underlying rule (structured logging, no interpolation) from two places in the standard — flagged as a duplicate-coverage note rather than merged, since both markers exist in the source document.

---

### Section 3 — Structured Logging Rules

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| logging.3.1 | Named placeholders required; no string interpolation in message templates | Development | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag `_logger.Log*` calls whose message arg is an interpolated string — implemented as `LOG005` (`NoStringInterpolationInLogMessageAnalyzer`, shared with logging.2.6) | Existing |
| logging.3.2 | Log levels used per defined semantics (no downgrading errors, no Info for debug-only) | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag suspicious patterns (e.g. `LogWarning` in `catch` that also returns 500) — implemented as `LOG006` (`ExceptionLogSeverityAnalyzer`) | Existing |
| logging.3.3 | Info+ entries include operation name, entity ids, correlation id as named properties | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag `_logger.Log*` calls with fewer than N named placeholders — implemented as `LOG007` (`LogEntryMissingNamedPropertiesAnalyzer`) | Existing |

**Notes:** logging.3.4 (Exception Logging) was removed from `GlobalLoggingStandards.md` — confirmed obsolete now that Application Insights automatically captures an exception's true origin (stack trace, source location) from the exception object, making a source-code rule about global-handler-only logging and layer re-logging unnecessary. Its rows have been removed from this matrix accordingly.

---

### Section 4 — Correlation Identifiers

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| logging.4.1 | Correlation id attached at entry point (from header or generated), returned in response header | Development | Binary | Hard-stop | Every-Commit | Roslyn-syntax: search middleware/`Program.cs` for `X-Correlation-Id`/`X-Request-Id` + `TraceIdentifier` — implemented as `LOG008` (`CorrelationIdNotAttachedAnalyzer`) | Existing |
| logging.4.2 | Correlation id pushed into log scope via `BeginScope` for request lifetime | Development | Binary | Hard-stop | Every-Commit | Roslyn-syntax: search for `_logger.BeginScope` with `CorrelationId` key — implemented as `LOG009` (`CorrelationIdNotInLogScopeAnalyzer`) | Existing |
| logging.4.3 | Correlation id forwarded on outbound HTTP calls via `HttpClientManager` | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag outbound `HttpClient`/`HttpRequestMessage` bypassing `HttpClientManager` — implemented as `LOG010` (`DirectHttpClientCallBypassesManagerAnalyzer`) | Existing |

---

### Section 5 — Audit Logging

Audit logging is a distinct stream from `ILogger<T>` operational logging, comparable to analytics telemetry (logging.6.1) — detection techniques below target the repository's dedicated audit-logging mechanism (e.g., an `IAuditLogger`/`_auditLogger` abstraction or equivalent), not `_logger.Log*` calls.

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| logging.5.1 | Audit entry required for: (a) CRUD on app data, (b) sensitive-data access, (c) authorization changes, (d) security-relevant decisions | Development | Heuristic | Warning | Every-Commit | Naming-convention: flag `Create*`/`Update*`/`Delete*`/`Grant*`/`Revoke*` methods or sensitive-entity types lacking a nearby audit-logging call (e.g., `_auditLogger.Record`/`IAuditLogger` invocation) — implemented as `LOG011` (`MissingAuditLogEntryAnalyzer`) | Existing |
| logging.5.2 | Audit entry includes Who/What/When/Outcome/CorrelationId/EntityType/EntityId | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: for calls identified via 5.1, check the dedicated audit-logging call's arguments against required field list (not `_logger.Log*` named placeholders) — implemented as `LOG012` (`AuditEntryMissingRequiredFieldsAnalyzer`) | Existing |
| logging.5.3 | Audit logs retained minimum 90 days in query-ready store | Infrastructure | Manual-only | Manual-only-comment | Periodic (suggested default: quarterly) | Retention-policy concern, not inspectable from source | Missing |

**Notes:** logging.5.1 categories (a)-(d) are explicit in the rule text — heuristic detection is scoped to those four categories, not an open-ended judgment call.

---

### Section 6 — Telemetry & Analytics

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| logging.6 | Telemetry emitted for every external dependency call (DB, HTTP, storage, queue, cache) | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag known dependency-call patterns not wrapped in dependency-tracking telemetry — implemented as `LOG013` (`DependencyCallMissingTelemetryAnalyzer`) | Existing |
| logging.6.1 | Analytics telemetry retained minimum 6 months | Infrastructure | Manual-only | Manual-only-comment | Periodic (suggested default: quarterly) | Retention-policy concern | Missing |

**Notes:** logging.6 needs tuning — some dependency calls may already be covered by App Insights auto-collection, which reduces false-positive risk but requires validation.

---

### Section 7 — Health Endpoints

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| logging.7 | `/healthcheck` endpoint required, unauthenticated, no internal details exposed | Structure | Binary | Hard-stop | Every-Commit | Roslyn-syntax: search `Program.cs` for `MapHealthChecks("/healthcheck")` + `.AllowAnonymous()` — implemented as `LOG014` (`MissingHealthCheckEndpointAnalyzer`) | Existing |

---

### Section 8 — Sensitive Data Exclusion

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| logging.8 | Sensitive data (passwords, tokens, PII, card numbers, connection strings, private keys) must never appear in log properties | Development | Heuristic | Hard-stop | Every-Commit | Roslyn-syntax: flag log calls with sensitive-sounding identifiers (`password`, `token`, `secret`, `ssn`, `creditcard`) or logging an entire entity object instead of named safe fields — implemented as `LOG015` (`SensitiveDataInLogEntryAnalyzer`) | Existing |

**Notes:** Heuristic confidence + Hard-stop severity is intentional here — this is about data-leak risk, not detection difficulty. Confirmed and locked in.

---

### Section 9 — Rate Limiting and Concurrency Guards

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| logging.9 | Rate limiting/aggregation required in tight loops; batches over 100 items must emit summary logs, not per-item logs | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag `_logger.Log*` calls inside `for`/`foreach`/`while` loop bodies — implemented as `LOG016` (`LogEntryInsideLoopAnalyzer`) | Existing |

---

### Sections 10–11 — Remediation & Compliance Checklist

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| logging.10 | Remediation backlog items (STD-025 provider onboarding pattern, STD-026 analytics telemetry implementation) | Procedural | N/A | N/A | N/A | Standard explicitly acknowledges these are not yet built | Acknowledged-future-work |
| logging.11 | Compliance Verification checklist (mirrors Sections 2–9 as checkboxes) | — | — | — | — | Mined directly into the rows above; no separate row needed | — |

---

## Resolved Discussion Points for This File (confirmed by repository owner)

1. **logging.2 (Azure Monitor/Log Analytics integration)** — Confirmed manual-only; no automated source-only or infra-as-code check exists today.
2. **logging.3.4 (Exception Logging)** — Removed entirely from `GlobalLoggingStandards.md`. Confirmed Application Insights automatically captures an exception's true origin (stack trace, source location) from the exception object without needing a source-code rule about global-handler-only logging or layer re-logging; the marker and its matrix rows have been retired as obsolete rather than left as an inactive/N-A row.
3. **logging.5.1/5.2 (audit logging is not `ILogger<T>`)** — Clarified: the rule text is explicit about the four qualifying categories (CRUD on app data, sensitive-data access, authorization changes, security-relevant decisions). "Audit-worthy" was the assistant's imprecise paraphrase, not part of the standard. Row reworded to cite the rule's actual categories; heuristic detection remains scoped to those four categories. Additionally confirmed: audit logging is a distinct logging stream from application/operational `ILogger<T>` logging, comparable to analytics telemetry (logging.6.1) — it is written through its own dedicated audit-logging mechanism/store, not `_logger.LogInformation`/`_logger.Log*`. The standard's 5.2 example and this matrix's detection techniques were corrected to reflect that.
4. **logging.8 (sensitive data exclusion)** — Confirmed: this concerns data content risk, not detection difficulty. Heuristic confidence + Hard-stop severity is intentional and locked in.
5. **Overlapping violations on the same line** — Confirmed this was the assistant's own implementation question, not a rule requirement. Each violation is reported independently regardless of whether multiple rules fire on the same line; no de-duplication logic is needed.
6. **Check frequency for infrastructure state** — Confirmed a new **Infrastructure** category is needed for rules that validate provisioned cloud/platform state rather than application code (logging.2.5, logging.5.3, logging.6.1). A dedicated **Frequency** column (Every-Commit / Periodic) has been added to the matrix so any rule — not just Infrastructure-category ones — can be marked for recurring-cadence validation instead of per-commit checking. Infrastructure-category rules are always Periodic by definition.
7. **Configurable cadence for Periodic rules** — Confirmed the actual interval for Periodic rules must not be hardcoded in the standard or matrix. Each Periodic row lists a Suggested Default Cadence only; the real interval must be read from repository/organization-level configuration (e.g., a `validation-schedule` setting) so cadence can be tuned without editing the standard or validation engine.
