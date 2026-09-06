# Logging Standards

**Version:** 1.1.0
**Status:** Active
**Applies To:** All repositories under the GlobalStandards governance model
**Audience:** AI models and human developers
**Created:** 2026-07-12
**Last Modified:** 2026-07-21

---
<!-- STD-MARKER: logging.file -->


## 1. Purpose
<!-- STD-MARKER: logging.1 -->

This document defines the rules for structured logging, correlation identifiers, audit logging, telemetry emission, health endpoints, and sensitive data handling. These rules apply to every hosted service in every repository under the GlobalStandards governance model.

Observability is not optional. A service that cannot be diagnosed in production is not production-ready. These rules define the minimum logging and telemetry surface required before any service is considered ready for a staging or production deployment.

---

## 2. Technology Baseline
<!-- STD-MARKER: logging.2 -->

### 2.1 — Logging Framework
<!-- STD-MARKER: logging.2.1 -->

Application code must use `Microsoft.Extensions.Logging` through `ILogger<T>`. Static logging singletons, `Console.Write`, `Trace`, `Debug`, and third-party logging facades that bypass `ILogger<T>` must not be used.

### 2.2 — Provider-Specific APIs
<!-- STD-MARKER: logging.2.2 -->

Application code must use `ILogger<T>` rather than provider-specific APIs.

### 2.3 — Logging and Observability Integration
<!-- STD-MARKER: logging.2.3 -->

Logging and observability integrations must be scaffolded behind interfaces and classes so that provider onboarding is accomplished primarily through configuration and account setup, not through rewrites of application code.

### 2.4 — Log Destination
<!-- STD-MARKER: logging.2.4 -->

The repository must configure the destination for `ILogger<T>` output in `Program.cs`. Application Insights must be the standard destination unless an approved alternative is documented in the repository addendum.

### 2.5 — Monitoring Platform
<!-- STD-MARKER: logging.2.5 -->

Repository telemetry must integrate with Azure Monitor. Application Insights and the configured Log Analytics workspace must be part of the standard monitoring setup unless an approved alternative is documented in the repository addendum.

### 2.6 — Structured Logging
<!-- STD-MARKER: logging.2.6 -->

All log entries must use named properties rather than string interpolation in the message template.

---

## 3. Structured Logging Rules
<!-- STD-MARKER: logging.3 -->

### 3.1 — Message Templates
<!-- STD-MARKER: logging.3.1 -->

All log messages must use named placeholders. String interpolation must not be used in a log message template.

```csharp
// CORRECT — structured, queryable properties
_logger.LogInformation("Document uploaded. {DocumentId} {ClientId} {FileName}", doc.Id, doc.ClientId, doc.FileName);

// WRONG — string interpolation loses queryability
_logger.LogInformation($"Document uploaded. {doc.Id} {doc.ClientId} {doc.FileName}");
```

### 3.2 — Log Levels
<!-- STD-MARKER: logging.3.2 -->

Log levels must be used according to the following definitions:

| Level | When to Use |
|---|---|
| `Trace` | Extremely detailed developer diagnostic data. Never enabled in production. |
| `Debug` | Diagnostic information useful during development or incident investigation. Disabled in production by default. |
| `Information` | Normal operational events that record progress through a workflow. |
| `Warning` | Recoverable conditions that indicate a potential problem or degraded behavior. |
| `Error` | A failure that prevented the current operation from completing. The application continues. |
| `Critical` | An unrecoverable failure. The application cannot continue without intervention. |

`Information` must not be used for events that are only useful during debugging. Errors must not be downgraded to `Warning` to suppress alert noise.

### 3.3 — Log Entry Content
<!-- STD-MARKER: logging.3.3 -->

Every `Information` log entry and every higher-severity log entry must include at minimum:

- The method signature or operation name as a named property.
- The relevant entity identifiers (`DocumentId`, `ClientId`, etc.) as named properties.
- The correlation identifier as defined in [Section 4](#4-correlation-identifiers).

Free-text descriptions of internal code paths must not be used in place of business events and named identifiers.

---

## 4. Correlation Identifiers
<!-- STD-MARKER: logging.4 -->

### 4.1 — Inbound Request Correlation
<!-- STD-MARKER: logging.4.1 -->

Every inbound HTTP request must have a correlation identifier attached at the application entry point before any log entries are written.

- Accept the identifier from the `X-Correlation-Id` or `X-Request-Id` request header if the caller provides one.
- Generate a new `Guid` if the caller does not provide one.
- Store the identifier in `HttpContext.TraceIdentifier` or a scoped ambient context.
- Return the identifier in the response header so the caller can correlate their own logs.

```csharp
app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
        ?? Guid.NewGuid().ToString();
    context.TraceIdentifier = correlationId;
    context.Response.Headers["X-Correlation-Id"] = correlationId;
    await next();
});
```

### 4.2 — Log Scope Enrichment
<!-- STD-MARKER: logging.4.2 -->

The correlation identifier must be pushed into the log scope for the lifetime of the request so that every log entry written during request processing includes it:

```csharp
using (_logger.BeginScope(new Dictionary<string, object>
{
    ["CorrelationId"] = context.TraceIdentifier
}))
{
    await next();
}
```

### 4.3 — Downstream Propagation
<!-- STD-MARKER: logging.4.3 -->

When making outbound HTTP calls to other services, the correlation identifier must be forwarded as the `X-Correlation-Id` header on the outbound request. Application-to-application HTTP calls must use the `HttpClientManager` library so that forwarding is handled consistently.

---

## 5. Audit Logging
<!-- STD-MARKER: logging.5 -->

### 5.1 — What Must Be Audited
<!-- STD-MARKER: logging.5.1 -->

Write an audit log entry for every operation that:

- Creates, modifies, or deletes application data.
- Accesses sensitive data (PII, financial records, documents with restricted access).
- Grants, revokes, or changes authorization (role assignments, client access changes).
- Results in a security-relevant decision (authentication failure, authorization denial, token validation failure).

### 5.2 — Audit Entry Format
<!-- STD-MARKER: logging.5.2 -->

Audit logging is a distinct logging stream from application/operational logging (`ILogger<T>`) — like analytics telemetry (see [Section 6.1](#61--analytics-telemetry)), it is written through its own dedicated audit-logging mechanism/store, not through `_logger.LogInformation` or any other `ILogger<T>` call. Every audit entry must record:

`Outcome` is required independently of error handling: exception/error-handler logging captures *system* failures (an unhandled exception, a database timeout), while `Outcome` captures *business and security* results that occur on a normal, non-exceptional code path — an authorization denial, an authentication failure, or a rejected business validation are not exceptions and never reach the global error handler, but they are exactly the security-relevant decisions Section 5.1 requires an audit entry for. Without `Outcome`, an audit trail could not distinguish a successful action from a denied or failed one, which defeats the purpose of auditing (e.g., detecting a pattern of repeated authorization denials against a sensitive record).

| Field | Description |
|---|---|
| `Who` | The authenticated principal identity (`sub` claim, service identity, or system). |
| `What` | The operation performed (e.g., `DocumentUploaded`, `ClientAccessDenied`). |
| `When` | Timestamp recorded for the audit event. |
| `Outcome` | `Success` or `Failure`. |
| `CorrelationId` | The request correlation identifier. |
| `EntityType` | The type of entity affected (`Document`, `Client`, etc.). |
| `EntityId` | The identifier of the affected entity. |

```csharp
// CORRECT — written through the dedicated audit-logging mechanism, not ILogger<T>
_auditLogger.Record(
    what: "DocumentUploaded", outcome: "Success",
    entityType: "Document", entityId: document.Id,
    who: User.GetSubjectId(), when: DateTime.UtcNow, correlationId: correlationId);

// WRONG — audit entry written through ILogger<T>, mixing audit data into operational logs
_logger.LogInformation(
    "Audit: {What} {Outcome}. {EntityType} {EntityId} by {Who}. {When} {CorrelationId}",
    "DocumentUploaded", "Success", "Document", document.Id, User.GetSubjectId(), DateTime.Now, correlationId);
```

### 5.3 — Audit Log Retention
<!-- STD-MARKER: logging.5.3 -->

Audit logs must be retained for a minimum of 90 days in an accessible query-ready store. A repository that requires a longer retention period must document the higher requirement in its repository addendum before production use.

---

## 6. Telemetry and External Dependency Tracking
<!-- STD-MARKER: logging.6 -->

Telemetry must be emitted for every call to an external dependency:

- Database queries.
- Outbound HTTP calls.
- Azure Storage operations.
- Message queue sends and receives.
- Cache reads and writes (on miss or error).

At minimum the telemetry entry must record the dependency name, operation name, duration, success or failure indicator, and correlation identifier.

Application Insights dependency tracking must be enabled when the repository uses the standard Application Insights integration. Dependencies that are not captured by auto-collection must be emitted through explicit dependency tracking code.

Any additional observability provider integrated into a repository must preserve the structured logging, correlation, audit logging, sensitive-data exclusion, and telemetry requirements in this file.

### 6.1 — Analytics Telemetry
<!-- STD-MARKER: logging.6.1 -->

Analytics telemetry must be captured for user behavior and application usage patterns that support product improvement decisions. Analytics events must be retained for a minimum of 6 months in a query-ready store. Analytics events must not include any prohibited data defined in [Section 8](#8-sensitive-data-exclusion).

---

## 7. Health Endpoints
<!-- STD-MARKER: logging.7 -->

Every hosted service must expose the following endpoint:

| Endpoint | Purpose | Authentication Required |
|---|---|---|
| `/healthcheck` | Liveness — confirms the process is running and not deadlocked. | No |

The health check endpoint must be excluded from JWT authentication requirements. Health checks must not expose internal configuration details, connection strings, or stack traces in their response bodies.

Register health checks using `IHealthChecksBuilder`:

```csharp
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>()
    .AddCheck("self", () => HealthCheckResult.Healthy());

app.MapHealthChecks("/healthcheck").AllowAnonymous();
```

---

## 8. Sensitive Data Exclusion
<!-- STD-MARKER: logging.8 -->

The following data must never appear in log properties, telemetry, or health endpoint responses:

- Passwords and secrets.
- Bearer tokens, access tokens, or refresh tokens.
- PII (full names, email addresses, phone numbers) unless the repository addendum explicitly documents the approved fields and required masking or redaction rules.
- Full credit card or financial account numbers.
- Connection strings.
- Private key material.

When logging entities that contain sensitive fields, only the approved safe fields may be projected into the log entry:

```csharp
// CORRECT
_logger.LogInformation("User authenticated. {UserId} {TenantId}", user.Id, user.TenantId);

// WRONG
_logger.LogInformation("User authenticated. {User}", user); // may serialize sensitive fields
```

---

## 9. Rate Limiting and Concurrency Guards
<!-- STD-MARKER: logging.9 -->

Apply rate limiting or concurrency controls anywhere a shared resource is exposed to external clients. The primary concern is protecting log sinks, queues, and databases from write floods triggered by external traffic.

Log entries written in a tight loop (for example, processing a large import batch) must be rate-limited or aggregated. A summary entry must be emitted rather than one entry per item when batch sizes exceed 100 items. Large data imports that use 100-row batch inserts must emit no more than one summary entry per 100 records processed.

```csharp
// Aggregate batch logging
_logger.LogInformation("Batch import complete. {Processed} records processed, {Failed} failed. {CorrelationId}",
    processed, failed, correlationId);
```

---

## 10. Remediation
<!-- STD-MARKER: logging.10 -->

- Define the reusable onboarding pattern for additional approved observability providers so provider onboarding is accomplished through configuration and account setup while preserving `ILogger<T>`, Azure Monitor, Application Insights, Log Analytics workspace, correlation, audit logging, sensitive-data exclusion, and telemetry requirements. Track this work in `Working/ProjectBacklog.md` as `STD-025`.
- Build the analytics telemetry implementation and storage pattern needed to satisfy the 6-month analytics retention requirement. Track this work in `Working/ProjectBacklog.md` as `STD-026`.

---

## 11. Compliance Verification
<!-- STD-MARKER: logging.11 -->

- [ ] All log entries use named structured properties — no string interpolation in message templates.
- [ ] Application code uses `ILogger<T>` rather than provider-specific APIs.
- [ ] Logging and observability integrations are scaffolded behind interfaces and classes so provider onboarding is accomplished primarily through configuration and account setup.
- [ ] The repository configures the `ILogger<T>` log destination in `Program.cs`.
- [ ] Repository telemetry integrates with Azure Monitor and includes Application Insights plus the configured Log Analytics workspace unless an approved alternative is documented in the repository addendum.
- [ ] Log levels are used according to the definitions in Section 3.2.
- [ ] Every `Information` log entry and every higher-severity log entry includes the method signature or operation name, relevant entity identifiers, and the correlation identifier.
- [ ] Unhandled exceptions that result in `Error` or `Critical` log entries are logged by the global error handler and pass the exception object to the logging call.
- [ ] Correlation identifier is generated or accepted on every inbound request.
- [ ] Correlation identifier is propagated into the log scope for the full request lifetime.
- [ ] Correlation identifier is forwarded as `X-Correlation-Id` on all outbound service calls.
- [ ] Audit entries are written for all state-changing, sensitive-access, and security-relevant operations.
- [ ] Audit entries include Who, What, When, Outcome, CorrelationId, EntityType, EntityId.
- [ ] Telemetry is emitted for all external dependency calls.
- [ ] Analytics telemetry is retained for a minimum of 6 months in a query-ready store.
- [ ] `/healthcheck` exists and is unauthenticated.
- [ ] No sensitive data (passwords, tokens, PII, connection strings) appears in log properties.
- [ ] High-volume loops emit aggregated log summaries rather than per-item entries, and 100-row batch inserts emit no more than one summary entry per 100 records.

---

## 12. Governance
<!-- STD-MARKER: logging.12 -->

This standard is owned by Troy Crowe. No changes to this file may be merged without Troy Crowe's explicit approval. Changes must be submitted as a pull request that includes a rationale comment explaining the reason for the update or deviation. Direct commits to `dev` or `main` are not permitted. Branch, PR, and approval rules must be defined by the root governance standard.
