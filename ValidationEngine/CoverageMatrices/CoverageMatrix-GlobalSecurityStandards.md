# Coverage Matrix — GlobalSecurityStandards.md

**Status:** Reviewed
**Sign Off:** Troy Crowe
**Sign Off Date:** 2026-08-14
**Revision Date:**
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

**Applicability note:** Applies broadly across all repositories, but individual sections are gated to specific file types: Section 2 (secrets) triggers on any changed file; Section 3 (auth) and Section 7 (cancellation tokens) trigger on C# files with async methods/endpoints; Section 4/5 (SQL/input validation) trigger on repository/controller files; Section 6 (sensitive data) triggers on logging/caching/error-handling call sites; Section 8/9 (engineering environment, pipeline gates) are Infrastructure/ADO-configuration facts, not source-diffable; Section 10 restates the checklist.

---

### Section 2 — Secrets Management

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| security.2.1 | Defined categories of secrets (connection strings, API keys, JWT signing keys, storage keys, passwords, managed identity client IDs, any `Key`/`Secret`/`Password`/`Token`/`ConnectionString`-labeled value) must never appear in source, config, YAML, or logs | Development | Heuristic | Hard-stop | Every-Commit | Text-scan/regex: standard secret-scanning patterns (entropy + keyword-labeled key/value pairs) against changed files, including pipeline YAML — this is the same detection already required at the pipeline gate (security.9.1); duplicates that check as a pre-commit/local layer | Missing |
| security.2.2 | Secrets stored per environment: Key Vault (prod/staging), ADO variable groups linked to Key Vault (CI/CD), .NET User Secrets (local dev) — never `appsettings.Development.json` | Technology | Binary | Hard-stop | Every-Commit | Roslyn/config-scan: flag secret-shaped values present in any `appsettings*.json` file, and specifically in `appsettings.Development.json` | Missing |
| security.2.3 | Secrets accessed only through the standard configuration pipeline (`IConfiguration`/strongly typed options); services must not call Key Vault SDK directly; no inline-constructed/concatenated connection strings | Technology | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag `SecretClient`/`KeyVaultClient` usage outside `Program.cs`/startup composition; flag string-concatenated connection-string construction | Missing |
| security.2.5

---

### Section 3 — Authentication and Authorization

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| security.3.1 | Global fallback authorization policy requires authenticated user by default; `[AllowAnonymous]` endpoints must be explicit with a documented reason comment; no custom authentication implementations | Structure | Heuristic | Hard-stop | Every-Commit | Roslyn-syntax: verify `SetFallbackPolicy`/`RequireAuthenticatedUser` global registration exists in `Program.cs`; flag `[AllowAnonymous]` attributes with no adjacent comment explaining the reason | Missing |
| security.3.2 | Authorization policies defined in dedicated policy classes, not inline in `Program.cs`/controllers; named policies registered at entry point; policies unit-testable in isolation | Structure | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag inline `RequireRole`/`RequireClaim`/policy-builder lambdas defined directly inside controller action methods rather than referencing a named policy — duplicates `GlobalCodingStandards.md` coding.2.6 detection | Missing |
| security.3.3 | Least privilege applied at every layer (app roles/claims, managed identities, database accounts, ADO service connections) | Infrastructure | Manual-only | Manual-only-comment | Periodic (suggested default: quarterly) | Requires inspecting live Azure RBAC role assignments, database account permission grants, and ADO service-connection scope — not source-diffable | Missing |
| security.3.4 | Four-layer tenant/client isolation defense-in-depth (identity, JWT validation, `ClientAccessAuthorizer` three-way match, repository-level `int?`/`int` tenant/client scope parameters); `MultitenancyOptions.IsEnabled` + nullable-to-required promotion dual-gate activation model; startup fail-fast validator required when enabled | Development | Heuristic | Hard-stop | Every-Commit | Roslyn-syntax: flag new repository/service method signatures missing `tenantId`/`clientId` leading nullable parameters; flag `MultitenancyOptions.IsEnabled` guard blocks missing the null-check-and-throw pattern; verify a corresponding startup validator exists when the flag registration changes | Missing |
| security.3.5 | JWT Bearer validation requires `ValidateIssuer`, `ValidateAudience`, `MapInboundClaims = false`, `RequireHttpsMetadata = true` — none may be omitted or weakened | Technology | Binary | Hard-stop | Every-Commit (when `Program.cs`/auth startup files change) | Roslyn-syntax: parse `JwtBearerOptions` configuration call site for all four required parameter assignments and their exact required values | Missing |
| security.3.6 | Token refresh via `Microsoft.Identity.Web`/`ITokenAcquisition.GetAccessTokenForUserAsync` before every outbound call; `AddDistributedTokenCaches()` (Redis-backed) required, not `AddInMemoryTokenCaches()`; `MicrosoftIdentityWebChallengeUserException` caught and redirected to login, not surfaced as 500 | Technology | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag `AddInMemoryTokenCaches()` registration; flag manual token caching by the caller instead of calling `GetAccessTokenForUserAsync` per request; flag missing catch for `MicrosoftIdentityWebChallengeUserException` around token-acquisition call sites | Missing |

---

### Section 4 — Inline SQL Prohibition

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| security.4.1 | All database operations through stored procedures; inline SQL — including parameterized inline SQL — prohibited in any service/repository/infrastructure class; stored procedure is the sole query contract and application must not construct/alter queries at runtime | Technology | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag raw SQL string literals passed to `SqlCommand`/Dapper `QueryAsync`/ADO.NET execute calls instead of a stored-procedure name — duplicates `GlobalCodingStandards.md` coding.2.7 and `GlobalDatabaseStandards.md` Section 5 detection; same check, listed independently per source markers | Missing |
| security.4.1 | All database operations through stored procedures; inline SQL — including parameterized inline SQL — prohibited in any service/repository/infrastructure class; stored procedure is the sole query contract and application must not construct/alter queries at runtime | Technology | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag raw SQL string literals passed to `SqlCommand` or assigned to `CommandText` or supplied to Dapper-style `QueryAsync`/`Execute*` methods. | SEC005 |

---

### Section 5 — Input Validation and File Handling

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| security.5.1 | Entry-point guard-clause validation must cover null/empty, range/format, and allowlist checks; general guard-clause-at-entry-point requirement now cross-references `GlobalCodingStandards.md` coding.7.2 instead of restating it | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: verify entry-point guard clauses cover all three categories (null/empty, range/format, allowlist) — general guard-clause presence check itself is `GlobalCodingStandards.md` coding.3.4/coding.7.2's detection | Missing |
| security.5.2 | Uploaded files validated on three dimensions: content-type allowlist, configured max size (no hardcoded limits), file-signature (magic bytes) match | Development | Heuristic | Hard-stop | Every-Commit | Roslyn-syntax: flag file-upload handling code missing one of the three checks; flag hardcoded numeric size-limit literals instead of a configuration-sourced value | Missing |
| security.5.3 | Malware scanning mandatory on all uploads, cannot be disabled in any environment; unrecognized/missing provider must throw at startup; `NoOpMalwareScanner` restricted to test projects only | Technology | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag `NoOpMalwareScanner`/pass-through `IMalwareScanner` registrations outside test project scope; verify a startup validator throws on an unrecognized provider value | Missing |
| security.5.4 | Query strings validated inbound (global coarse filter + `[ValidateQueryString]` attribute; manual validation required for raw `HttpContext.Request.Query` access) and encoded outbound (via `HttpClientManager` parameterized overloads, never string concatenation; `Uri.EscapeDataString` not `Uri.EscapeUriString`) | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag direct `HttpContext.Request.Query`/`QueryString` access without adjacent validation; flag string-interpolated/concatenated URLs passed to `HttpClientManager`/`HttpClient` calls with caller-supplied values; flag `Uri.EscapeUriString` usage | Missing |

---

### Section 6 — Sensitive Data

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| security.6.1 | Defined sensitive data categories (credentials, PII, financial data, health information) | Development | Manual-only | Manual-only-comment | Every-Commit | Classification reference row only; detection lives in security.6.2's sub-rules | Missing |
| security.6.2 | Sensitive data must not appear in log output, cache entries, error responses, or source control — now a pure cross-reference table to `GlobalLoggingStandards.md`, `GlobalCachingStandards.md` caching.10.1, `GlobalCodingStandards.md` coding.7.5/7.7, and Section 2 of this file | Development | Duplicate | Duplicate | Every-Commit | Pure cross-reference; detection already captured in `GlobalLoggingStandards.md`, `GlobalCachingStandards.md`, and `GlobalCodingStandards.md` coverage matrices | Missing |
| security.6.3 | All data in transit uses TLS; non-TLS connections not permitted in any environment | Technology | Binary | Hard-stop | Every-Commit (when connection configuration changes) | Config-scan: flag connection strings/`ConfigurationOptions` with TLS explicitly disabled — duplicates `GlobalCachingStandards.md` caching.10.2 detection for Redis; extends the same pattern to other connection types (SQL, storage, messaging) | Missing |
| security.6.4 | Data at rest encrypted: Azure SQL TDE, Blob Storage Service Encryption, Redis encryption-at-rest, Key Vault-managed encryption keys (CMK when required), column-level encryption for PII/financial fields when required | Infrastructure | Manual-only | Manual-only-comment | Periodic (suggested default: quarterly) | Requires inspecting live Azure resource configuration (TDE status, storage encryption settings, Redis encryption settings) — not source-diffable except for the column-level-encryption design decision, which is itself a design-time judgment | Missing |

---

### Section 7 — Cancellation Tokens

**Note:** `security.7.1` (general CancellationToken forwarding rule) was removed as an exact duplicate of `GlobalCodingStandards.md` coding.8.4 and no longer appears in the standards file or this matrix.

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| security.7.2
| security.7.3 | Long-running operations (batch, billing runs, bulk imports, external calls) respect cancellation; polling loops check `IsCancellationRequested` each iteration or call `ThrowIfCancellationRequested()` | Development | Heuristic | Warning | Every-Commit | Roslyn-syntax: flag `while`/`for` loops containing `await Task.Delay`/repeated async work with no cancellation check in the loop body | Missing |

---

### Section 8 — Engineering Environment Security

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| security.8.1 | NuGet packages from official/approved feeds only; pinned versions (no floating ranges); `dotnet list package --vulnerable` run before opening PR when packages change | Technology | Binary | Hard-stop | Every-Commit (when `.csproj`/`packages.lock.json` changes) | Config-scan: flag floating version specifiers (`*`, `1.x`, `[1.0,)`) in `.csproj`/`PackageReference` entries; pipeline step for the vulnerability scan itself is Infrastructure (see security.9.1) | Missing |
| security.8.2 | Pipeline YAML contains no secret values; service connections least-privilege scoped; no unpinned external script execution; production/staging deploys require approvals | Infrastructure | Manual-only | Manual-only-comment | Periodic (suggested default: quarterly) | Requires inspecting live ADO service-connection scope and pipeline approval-gate configuration — not source-diffable beyond the YAML secret-scan already covered by security.2.1/9.1 | Missing |
| security.8.4 | SAST on every PR build (blocks on critical/high in changed code); DAST against staging before production promotion (blocks on critical); dependency scanning on every build (blocks on critical CVE) | Infrastructure | Manual-only | Manual-only-comment | Periodic (suggested default: quarterly) | Requires inspecting live pipeline YAML/ADO run configuration for SAST/DAST tool wiring and blocking-gate behavior — not source-diffable from application code alone; the pipeline YAML text itself could be scanned for the presence of the required scan steps once pipeline files are in scope | Missing |
| security.8.5 | OWASP Top 10/ASVS baseline mapping table — reference/index only | Procedural | Manual-only | Manual-only-comment | Every-Commit | Pure cross-reference table; no independent detection — points to Sections 3, 4, 6, 8.1, 8.2, 8.4 already covered above | Missing |
| security.8.6 | All app-to-app HTTP calls through `HttpClientManager`; direct `HttpClient` instantiation for in-scope calls not permitted; Azure SDK clients explicitly out of scope; specific security controls (HTTPS-only, bearer-token header-injection validation, URI scheme allowlist, protocol-relative URL rejection, response size limits, safe `Content-Disposition`) enforced inside the library | Technology | Binary | Hard-stop | Every-Commit | Roslyn-syntax: flag `new HttpClient()`/direct `HttpClientFactory.CreateClient` instantiation in application service code for calls that construct requests/supply tokens/process responses (excluding recognized Azure SDK client types) | Missing |

---

### Section 9 — Pipeline Security Gates

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| security.9.1 | Layer 1 pre-merge gate: branch protection, required approval, commit signing, incremental SAST on changed files (blocks critical/high), dependency scan with lock-file-hash caching (blocks critical/high CVE), secret scanning on changed files including pipeline YAML | Infrastructure | Manual-only | Manual-only-comment | Periodic (suggested default: quarterly) | Requires inspecting live ADO PR-validation pipeline configuration and branch-policy settings — not source-diffable; the underlying secret-pattern and SAST-rule detection itself (security.2.1) is the source-diffable portion, already captured there | Missing |
| security.9.2 | Layer 2 pre-deployment gate: re-scan compiled artifact, delta comparison against PR baseline, targeted DAST against staging scoped to changed surface, pipeline-definition change control | Infrastructure | Manual-only | Manual-only-comment | Periodic (suggested default: quarterly) | Requires inspecting live release-pipeline configuration and artifact-scan tooling — not source-diffable | Missing |
| security.9.3 | Layer 3 startup gate: every configurable security control has a synchronous startup validator; validators are fast (no network/DB calls) | Development | Heuristic | Hard-stop | Every-Commit (when startup/`Program.cs` files change) | Roslyn-syntax: cross-check that each configurable security-control flag identified elsewhere in this file (malware scanning provider, `MultitenancyOptions.IsEnabled`, etc.) has a corresponding registered startup validator; flag validators containing `await`/network-call patterns | Missing |

---

### Section 10 — Compliance Verification (Excluded — content not read in this pass; expected to restate Sections 2–9 as a checklist per established pattern)

### Section 11 — Governance (Excluded)

`security.11` (ownership/PR-approval boilerplate) is excluded — not code-checkable, procedural ownership statement only.

---

**Notes:**
1. This file has the largest concentration of **cross-file duplicate coverage** seen so far: security.4.1/4.2 duplicate `GlobalCodingStandards.md` coding.2.7 and `GlobalDatabaseStandards.md` Section 5 (inline SQL); security.6.2 duplicates detection needed in `GlobalLoggingStandards.md`, `GlobalCachingStandards.md` caching.10.1, and `GlobalCodingStandards.md` coding.7.5/7.7 (sensitive data exposure); security.6.3 duplicates `GlobalCachingStandards.md` caching.10.2 (TLS); security.7.1 duplicates `GlobalCodingStandards.md` coding.8.4 (CancellationToken forwarding); security.3.2 duplicates `GlobalCodingStandards.md` coding.2.6 (authorization policy placement); security.8.3 duplicates `GlobalAzureDevOpsPipelineStandards.md` azure-devops-pipeline.3.2 (branch protection). A shared detection-rule library is strongly recommended so these checks are implemented once and referenced by marker ID across all affected matrices, rather than reimplemented per file.
2. Section 3.4 (tenant/client isolation defense-in-depth) is the most structurally complex rule in this file — it spans a four-layer model plus a dual-gate multitenancy activation pattern (`MultitenancyOptions.IsEnabled` + nullable-to-required parameter promotion) that is explicitly a **transitional, in-progress** design (the application is currently single-tenant). Detection here should focus on the mechanical, currently-enforceable pieces (nullable leading parameters present, guard-clause pattern present) rather than asserting full multi-tenant correctness, which isn't active yet.
3. Section 8 (Engineering Environment Security) and Section 9 (Pipeline Security Gates) are overwhelmingly Infrastructure/ADO-configuration facts requiring live pipeline/ADO inspection, consistent with the same finding in `CoverageMatrix-GlobalAzureDevOpsPipelineStandards.md`. Only security.8.1 (floating version ranges), security.8.6 (`HttpClientManager` usage), and security.9.3 (startup validator presence) are meaningfully source-diffable.
4. security.9.1's SAST/secret-scanning requirements at the pipeline layer are the authoritative production-gate version of the same detection needed locally for security.2.1 — the local/pre-commit check (this repo's own validation tooling) and the ADO pipeline gate are two independent enforcement points for the same underlying rule, both listed because the source document treats them as distinct layers.

