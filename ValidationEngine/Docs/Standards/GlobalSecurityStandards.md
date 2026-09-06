# Security Standards

**Version:** 1.0.0
**Status:** Active
**Applies To:** All repositories under the GlobalStandards governance model
**Audience:** AI models and human developers
**Created:** 2026-07-11
**Last Modified:** 2026-07-13

---
<!-- STD-MARKER: security.file -->


## 1. Purpose
<!-- STD-MARKER: security.1 -->

This standard defines the security rules that must be applied across all repositories and across all phases of development — design, code, build, deploy, and run. Both human developers and AI models must apply every rule in this file when writing, reviewing, or modifying any code, configuration, or infrastructure that touches authentication, authorization, secrets, data access, input handling, or cancellation. Compliance is verified during pull request review and the pre-merge compliance process.

[`GlobalCodingStandards.md`](../standards/GlobalCodingStandards.md#27-authorization-policies)

The application suite is currently single-tenant. `tenantId` is not a database filter parameter at this time. Multi-tenancy data isolation is a planned post-release iteration. The isolation rules, authorization policy structure, and data access controls in this standard are written to be correct for the current single-tenant model. When multi-tenancy is introduced, this standard must be updated before any multi-tenant work begins — see `ProjectBacklog.md` for the tracked item.

---

## 2. Secrets Management
<!-- STD-MARKER: security.2 -->

Both human developers and AI models must verify that no secret, credential, or connection string appears in any committed file before the work slice is closed. A committed secret is a critical violation and must be treated as an immediate incident — not a deferred cleanup item.

### 2.1 What Qualifies as a Secret
<!-- STD-MARKER: security.2.1 -->

The following values are secrets and must never appear in source code, configuration files committed to source control, YAML pipeline files, or log output:

- Connection strings (database, Redis, storage, messaging)
- API keys and client secrets
- JWT signing keys and token signing certificates
- Storage account keys and SAS tokens
- Passwords and password hashes
- Managed identity client IDs when used as authentication credentials
- Any value labeled `Key`, `Secret`, `Password`, `Token`, or `ConnectionString` in any configuration section

### 2.2 Required Storage Locations
<!-- STD-MARKER: security.2.2 -->

| Context | Required Storage |
|---|---|
| Production and staging environments | Azure Key Vault, surfaced via Key Vault-backed app configuration |
| CI/CD pipelines | Azure DevOps variable groups linked to Key Vault — not inline pipeline variables |
| Local development | .NET User Secrets (`dotnet user-secrets`) — never `appsettings.Development.json` committed to source control |
| Any other context | Key Vault — no exceptions |

### 2.3 Configuration Pattern
<!-- STD-MARKER: security.2.3 -->

Secrets must be accessed through the standard .NET configuration pipeline. Services must never call Key Vault SDKs directly — Key Vault must be connected as a configuration provider at the application entry point, making secrets available as standard configuration keys.

```csharp
// Required — Key Vault connected as a configuration provider in Program.cs
// Services access secrets via IConfiguration or strongly typed options, not via Key Vault SDK directly
builder.Configuration.AddAzureKeyVault(
    new Uri(builder.Configuration["KeyVault:Uri"]!),
    new DefaultAzureCredential());
```

Connection strings and secrets must be accessed by name through `IConfiguration` or bound to a strongly typed options class. They must never be constructed inline or concatenated from parts in application code.

### 2.5 Secret Rotation
<!-- STD-MARKER: security.2.5 -->

Secrets must be rotatable without requiring an application code change or redeployment. Connection strings and API keys must be referenced by name through Key Vault — a rotation must require only a Key Vault secret update, not a code or config file change. Applications using `IOptionsMonitor<T>` or Key Vault-backed configuration with refresh enabled pick up rotated values without restart.

---

## 3. Authentication and Authorization
<!-- STD-MARKER: security.3 -->

Both human developers and AI models must verify that every endpoint and every data access operation is covered by the correct authentication and authorization policy before a PR is opened. An unauthenticated or unauthorized code path is a blocking defect.

### 3.1 Default to Authenticated
<!-- STD-MARKER: security.3.1 -->

Authentication is handled by Azure Entra ID and Azure B2B using OIDC. Users authenticate through the identity provider and receive a JWT access token. That token is presented on every API request and validated by the API before any request is processed. The application does not implement its own authentication — it validates tokens issued by the identity provider.

MFA is enforced at the identity provider level through Azure Entra ID and Azure B2B configuration. It is a platform control — not something developers implement or configure in application code. No application code may bypass or work around the MFA policy enforced by the identity provider.

Every API endpoint must require a valid access token by default. Endpoints that do not require authentication must be explicitly opted out with `[AllowAnonymous]` and the reason must be documented in a comment on the same line or directly above the attribute. Application authentication must use the built-in framework authentication features — custom authentication implementations must not be written.

```csharp
// Required — global authorization policy applied at the application level in Program.cs
// All endpoints require a valid JWT access token by default
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

// Required — opt-out must be explicit and the reason documented
[AllowAnonymous] // Public endpoint: unauthenticated health check required by load balancer
[HttpGet("/healthcheck")]
public IActionResult Health() => Ok();
```

A global fallback policy applied at the application level is the required pattern. Applying `[Authorize]` individually to every controller is not sufficient — a missing attribute on a new controller silently creates an unauthenticated endpoint.

### 3.2 Authorization Policies
<!-- STD-MARKER: security.3.2 -->

Authorization policies must be defined in dedicated policy class files, not inline in `Program.cs` or in controller attributes. Named policies must be registered at the application entry point. No authorization logic — role checks, claim checks, or resource checks — may be written inline in a controller action or service method.

```csharp
// Required — policy names defined in a dedicated class
// Pattern from DocumentManagerAPI/Services/Security/DocumentAuthorizationPolicies.cs
internal static class DocumentAuthorizationPolicies
{
    public const string Upload   = "Documents.Upload";
    public const string Update   = "Documents.Update";
    public const string Download = "Documents.Download";
    public const string Delete   = "Documents.Delete";
}

// Required — policies registered at the application entry point using role values
// sourced from configuration, not hardcoded. Pattern from DocumentManagerAPI/Program.cs.
builder.Services.AddAuthorization(options =>
{
    AddRolePolicy(options, DocumentAuthorizationPolicies.Upload,   authorizationOptions.UploadRoles);
    AddRolePolicy(options, DocumentAuthorizationPolicies.Update,   authorizationOptions.UpdateRoles);
    AddRolePolicy(options, DocumentAuthorizationPolicies.Download, authorizationOptions.DownloadRoles);
    AddRolePolicy(options, DocumentAuthorizationPolicies.Delete,   authorizationOptions.DeleteRoles);
});

// Required — role values must come from configuration, never hardcoded in the policy registration
static void AddRolePolicy(AuthorizationOptions options, string policyName, IEnumerable<string> roles)
{
    options.AddPolicy(policyName, policy =>
        policy.RequireAuthenticatedUser()
              .RequireRole(roles));
}
```

Authorization policies must be unit testable in isolation. A policy that cannot be tested independently is not compliant.

### 3.3 Least Privilege
<!-- STD-MARKER: security.3.3 -->

Both human developers and AI models must apply least privilege at every layer:

- **Application roles and claims** — assign the minimum role or claim set required for the operation; do not grant a broader role when a narrower one satisfies the requirement
- **Managed identities** — assign only the specific RBAC roles required by the application; do not use Owner or Contributor unless the application explicitly requires those permissions
- **Database accounts** — the application database account must have only the permissions required to execute the stored procedures it calls; DDL permissions must not be granted to the application account
- **Service connections in Azure DevOps** — pipeline service connections must be scoped to the specific subscription and resource group they require; subscription-wide connections are not permitted

### 3.4 Tenant and Client Isolation — Defense in Depth
<!-- STD-MARKER: security.3.4 -->

Tenant and client isolation is enforced across four independent security layers. Each layer enforces its own control independently. A failure or bypass at one layer must not be able to expose another tenant's or client's data through a lower layer. This is the defense-in-depth model for this application suite.

| Layer | Enforced By | What It Prevents |
|---|---|---|
| 1 — Identity | Entra ID / B2B OIDC | Unauthenticated access; users without a valid identity cannot reach the application |
| 2 — API access | JWT access token validation | Requests without a valid token are rejected before reaching any controller or service |
| 3 — Client boundary | `ClientAccessAuthorizer` in the service layer | A three-way match is required: the `ClientId` claim in the access token, the user's active client record in the database, and the requested `ClientId` in the request must all agree. A token with a manipulated `ClientId` claim fails the database cross-check even if it passes JWT signature validation. |
| 4 — Data access | Repository-level client and tenant scope filter; stored procedure parameters | `int? tenantId` and `int? clientId` are declared as nullable leading parameters on all new and updated endpoints and repository methods. They are nullable until data compliance is confirmed, at which point they are promoted to required `int`. No new data access path may be added without both parameters declared. Existing methods where `clientId` is already a required `int` are not changed. |

No layer may assume that an upstream layer has already enforced the isolation. The redundancy across all four layers is intentional — it is the control.

Both human developers and AI models must verify that all four layers are intact for every data access operation before a PR is opened. Adding a new data access path that bypasses any one of these layers is a security violation regardless of whether the other three layers are present.

Multi-Tenancy Infrastructure Strategy

The multi-tenancy activation model is built on two independent gates that must both be opened together as a deliberate, coordinated release step. Neither gate alone is sufficient.

| Gate | Off state | On state | How to activate |
|---|---|---|---|
| **Config flag** (`MultitenancyOptions.IsEnabled`) | `false` — compliance code path is bypassed at runtime | `true` — compliance code path is active | Change config value; no code or signature change required |
| **Nullable promotion** (`int?` → `int`) | Compiler allows nulls through; values are carried but not enforced | Compiler rejects calls that do not supply the value; enforcement is structural | Promote parameter type at declaration; update all call sites |

Flipping the flag without promoting still allows nulls through at the compiler level. Promoting without flipping the flag means the code path is not yet wired. Both gates must move together. This design means multitenancy enforcement is **structurally present but inactive** — it is turned on exactly when it is applied and off until then, with no silent partial states.

**`MultitenancyOptions` configuration class**

Each application that participates in the multi-tenancy rollout must define a `MultitenancyOptions` class bound from configuration, following the same pattern as `MalwareScanningOptions` in `DocumentManagerAPI`:

```csharp
public class MultitenancyOptions
{
    public bool IsEnabled { get; set; }
}
```

Registered in `appsettings.json`:

```json
"Multitenancy": {
  "IsEnabled": false
}
```

**Startup validation rule:** If `MultitenancyOptions.IsEnabled` is `true`, the application must fail fast at startup if tenant or client values are missing or cannot be resolved — the same principle that governs malware scanning. A misconfigured deployment must never silently run with multitenancy enabled but unenforced. Register a startup validator in `Program.cs` that checks this condition before the application begins serving traffic.

This fail-fast behavior is reinforced by the pipeline rollback policy. If the application fails to start due to a misconfigured multitenancy activation, the pipeline detects the failure and automatically rolls back to the previous known-good deployment. The application remains operational throughout with no manual intervention required. The two mechanisms work as a complementary pair: the startup validator prevents a bad deployment from serving traffic, and the pipeline rollback ensures the previous version is immediately restored. Neither gate needs to be bypassed or worked around — a failed activation is simply a non-event from a continuity perspective.

**Compliance code path**

Wrap all tenant and client filter logic in a guard on `MultitenancyOptions.IsEnabled`. When the flag is `false`, the block is skipped entirely and the method behaves as it does today. When the flag is `true`, the filter is applied and null values are rejected:

```csharp
if (_multitenancyOptions.IsEnabled)
{
    if (tenantId is null || clientId is null)
        throw new InvalidOperationException("Multitenancy is enabled but tenantId or clientId was not supplied.");

    // apply tenant and client filter to the data access call
}
```

**Parameter ordering rule:**
Use nullable parameters (`int?` with no default value), not optional parameters (`int? x = null`). Nullable parameters have no ordering restriction in C# — they can appear anywhere in the signature. Optional parameters with default values must appear before `CancellationToken`, which the coding standard requires last. Nullable parameters avoid that constraint entirely.

`tenantId` leads, followed by `clientId`, then any domain-specific parameters, with `CancellationToken` last:

```csharp
// Required parameter order — nullable tenantId and clientId lead the signature
public async Task<IEnumerable<InvoiceRecord>> GetInvoicesAsync(
    int? tenantId,               // nullable until promoted; no ordering restriction
    int? clientId,               // nullable until promoted; no ordering restriction
    CancellationToken cancellationToken)
{
    if (_multitenancyOptions.IsEnabled)
    {
        if (tenantId is null || clientId is null)
            throw new InvalidOperationException("Multitenancy is enabled but tenantId or clientId was not supplied.");

        // apply filter
    }

    // existing data access logic unchanged until promotion
}
```

**Important:** Existing methods where `clientId` is already declared as a required `int` are not changed by this rule. Those methods are already wired and enforced. This pattern applies to new methods and to existing methods being updated as part of CODE-006.

Adding these parameters to existing methods is a broad but mechanical contract update — no logic changes, only signature updates at every method declaration and call site. This work is tracked as CODE-006. All in-scope methods must be updated before any method is promoted from nullable `int?` to required `int`, so the full contract surface is consistent.
```

```csharp
// Required — three-way client access check in the service layer before any data access
// Pattern established in ClientAccessAuthorizer (DocumentManagerAPI)
public async Task<string> GetAuthorizedClientIdAsync(string targetClientId, CancellationToken cancellationToken)
{
    if (!int.TryParse(targetClientId, out var requestedClientId))
        throw new ArgumentException("The client identifier format is invalid.", nameof(targetClientId));

    // Administrators bypass the client boundary check — documented exception
    if (_tenantContextAccessor.IsInRole(_options.AdministratorRoleName))
        return requestedClientId.ToString();

    // Check 1 — extract the ClientId from the access token claim
    var tokenClientId = _tenantContextAccessor.GetRequiredClientId();
    if (!int.TryParse(tokenClientId, out var tokenClientIdValue))
        throw new TenantContextMissingException("The authenticated token does not contain a valid client identifier claim.");

    // Check 2 — verify the user's active client in the database
    var userId = _tenantContextAccessor.GetRequiredUserId();
    var activeClientId = await _clientUserReferenceRepository.GetActiveClientIdForUserAsync(userId, cancellationToken);

    // Check 3 — all three values must match; any mismatch is a denial
    if (!activeClientId.HasValue ||
        activeClientId.Value != requestedClientId ||
        tokenClientIdValue != requestedClientId)
    {
        throw new ClientAccessDeniedException(targetClientId);
    }

    return requestedClientId.ToString();
}
```

---

### 3.5 JWT Token Validation
<!-- STD-MARKER: security.3.5 -->

Every API must validate the JWT access token on every request using the `AddJwtBearer` middleware configured with the following required parameters. Both human developers and AI models must not weaken or omit any of these validation parameters.

```csharp
// Required — JWT Bearer configuration in Program.cs
// All four parameters are required — none may be omitted or set to false
static void ConfigureJwtBearer(JwtBearerOptions options, AzureAdB2COptions azureAdOptions)
{
    options.Authority = azureAdOptions.GetAuthority();    // Entra ID / B2C authority URL
    options.Audience = azureAdOptions.GetAudience();      // This API's application ID
    options.MapInboundClaims = false;                     // Preserve claim types as issued by Entra ID
    options.RequireHttpsMetadata = true;                  // Metadata endpoint must be HTTPS

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,    // Token must be issued by the configured Entra ID tenant
        ValidateAudience = true,  // Token must be issued for this specific API
        NameClaimType = azureAdOptions.NameClaimType,
        RoleClaimType = azureAdOptions.RoleClaimType
    };
}
```

- **`ValidateIssuer = true`** — rejects tokens issued by any identity provider other than the configured Entra ID tenant. A token issued by a different tenant cannot be used against this API.
- **`ValidateAudience = true`** — rejects tokens where the `aud` claim does not match this API's application ID. A valid token issued for a different API or resource cannot be used against this API. This is the primary defense against token substitution.
- **`MapInboundClaims = false`** — preserves the original claim type names from Entra ID (e.g., `extension_ClientId`, `extension_Roles`). When set to `true`, the middleware remaps claim types to WS-Federation names which breaks custom claim reads.
- **`RequireHttpsMetadata = true`** — the OIDC discovery document must be fetched over HTTPS. Must not be set to `false` in any environment including local development.

### 3.6 Token Refresh
<!-- STD-MARKER: security.3.6 -->

Access tokens issued by Entra ID have a limited lifetime. When a token expires, the application must silently renew it using the refresh token without requiring the user to re-authenticate. Silent renewal is handled by `Microsoft.Identity.Web` through `ITokenAcquisition.GetAccessTokenForUserAsync` — when the cached access token is expired, `Microsoft.Identity.Web` automatically exchanges the refresh token with Entra ID and returns a fresh access token to the caller.

`GetAccessTokenForUserAsync` must be called before every outbound API request. It must not be cached manually by the caller — token caching and renewal are owned by `Microsoft.Identity.Web` and must not be duplicated in application code. The `HttpOAuthClientService` pattern is the required implementation.

```csharp
// Required — token acquired via ITokenAcquisition before every outbound request
// Microsoft.Identity.Web handles silent renewal automatically when the cached token is expired
private async Task<string> GetAccessTokenAsync(string[] apiScopes, CancellationToken cancellationToken)
    => await _tokenAcquisition.GetAccessTokenForUserAsync(apiScopes);
```

**Token cache — distributed cache required:**

The default `AddInMemoryTokenCaches()` registration must be replaced with `AddDistributedTokenCaches()` backed by Redis. The in-memory cache does not survive application restarts or scale-out to multiple instances — a user whose request lands on a different instance will not find their cached token and will be forced to re-authenticate.

```csharp
// VIOLATION — in-memory token cache does not survive restarts or multi-instance deployments
.AddInMemoryTokenCaches();

// Required — distributed token cache backed by Redis
// Redis must already be registered via AddStackExchangeRedisCache per GlobalCachingStandards.md
.AddDistributedTokenCaches();
```

**Silent renewal failure handling:**

When silent renewal fails — because the refresh token has expired, been revoked, or the Entra ID session has ended — `Microsoft.Identity.Web` throws a `MicrosoftIdentityWebChallengeUserException`. This exception must be caught at the HTTP client service layer and the user must be redirected to the login page to re-authenticate. It must not be allowed to surface as an unhandled 500 error.

```csharp
// Required — catch silent renewal failure and redirect to login
try
{
    _accessToken = await GetAccessTokenAsync(apiScopes, cancellationToken);
}
catch (MicrosoftIdentityWebChallengeUserException)
{
    // Refresh token expired or revoked — user must re-authenticate
    // Redirect to login; do not surface as a 500 error
    throw;
}
```

---

## 4. Inline SQL Prohibition
<!-- STD-MARKER: security.4 -->

Both human developers and AI models must not write inline SQL in any application code. Inline SQL is prohibited for security and maintainability reasons — it is the primary vector for SQL injection and it bypasses the stored procedure contract that defines the data access layer boundary.

### 4.1 Rule
<!-- STD-MARKER: security.4.1 -->

All database operations must be executed through stored procedures. Inline SQL strings — including parameterized inline SQL — must not appear in any service, repository, or infrastructure class. String-concatenated SQL is a critical violation. Parameterized inline SQL is also a violation — the prohibition is on inline SQL itself, not only on unparameterized SQL.

```csharp
// VIOLATION — inline SQL, even when parameterized
var sql = "SELECT * FROM Invoices WHERE ClientId = @clientId";
var results = await connection.QueryAsync(sql, new { clientId });

// REQUIRED — stored procedure executed through the repository pattern
var results = await _repository.GetInvoicesByClientAsync(clientId, cancellationToken);
```

The stored procedure is the contract between the application and the database. The application must not construct or alter queries at runtime — all query logic lives in the stored procedure. This makes all database operations auditable, reviewable, and independently testable at the database layer. Full data access rules are defined in [`GlobalDatabaseStandards.md`](../standards/GlobalDatabaseStandards.md#5-repository-pattern).

---

## 5. Input Validation and File Handling
<!-- STD-MARKER: security.5 -->

Both human developers and AI models must validate all input at the entry point before it reaches any service or data access layer. Deferred validation — checking input deep inside a service or repository — is not compliant. The general guard-clause requirement for entry-point validation is defined in [`GlobalCodingStandards.md`](../standards/GlobalCodingStandards.md#72-input-validation) and is not restated here. The rules below cover the security-specific validation categories and file-handling requirements.

### 5.1 Validation Categories
<!-- STD-MARKER: security.5.1 -->

Entry-point guard-clause validation must cover the following categories for every input:

- Null and empty checks on required fields
- Range and format checks on typed values (dates, numeric ranges, string length limits)
- Allowlist validation on any value used to construct a file path, query parameter, or external call

```csharp
// Required — validate at the entry point before passing to the service layer
[HttpPost("clients/{clientId}/invoices")]
public async Task<IActionResult> UploadInvoice(
    int clientId, IFormFile file, CancellationToken cancellationToken)
{
    if (clientId <= 0)
        return BadRequest("ClientId must be a positive integer.");

    if (file is null || file.Length == 0)
        return BadRequest("A non-empty file is required.");

    if (file.Length > _options.MaxUploadSizeBytes)
        return BadRequest($"File size exceeds the maximum allowed size of {_options.MaxUploadSizeBytes} bytes.");

    // Validated — pass to service layer
    await _invoiceService.ProcessUploadAsync(clientId, file, cancellationToken);
    return Accepted();
}
```

### 5.2 File Upload Validation
<!-- STD-MARKER: security.5.2 -->

All uploaded files must be validated on three dimensions before being written to any storage location. Passing any one check while failing another is not sufficient — all three must pass.

| Dimension | Rule |
|---|---|
| Content type | Validate the declared `ContentType` against an allowlist of permitted MIME types; reject anything not on the allowlist |
| File size | Enforce a maximum size limit sourced from configuration; hardcoded size limits are not permitted |
| File signature (magic bytes) | Read the first bytes of the file and verify they match the expected signature for the declared content type; the declared content type alone is not sufficient |

```csharp
// Required — all three dimensions validated before writing to storage
public async Task ValidateUploadAsync(IFormFile file, CancellationToken cancellationToken)
{
    var allowedTypes = _options.AllowedContentTypes;  // from configuration
    if (!allowedTypes.Contains(file.ContentType))
        throw new ValidationException($"Content type '{file.ContentType}' is not permitted.");

    if (file.Length > _options.MaxUploadSizeBytes)
        throw new ValidationException("File exceeds maximum allowed size.");

    using var stream = file.OpenReadStream();
    var header = new byte[4];
    await stream.ReadAsync(header, cancellationToken);

    if (!IsValidFileSignature(header, file.ContentType))
        throw new ValidationException("File signature does not match the declared content type.");
}
```

### 5.3 Malware Scanning
<!-- STD-MARKER: security.5.3 -->

Malware scanning is a mandatory security control on all uploaded files. It is not optional and it must not be disabled in any environment including development, staging, or production. The only client-configurable aspect is which approved scanning service is used — the control itself is non-negotiable.

**Rules:**

- Every uploaded file must be scanned before it is written to any storage location.
- A scan failure must block the upload. Files that cannot be confirmed clean must be rejected.
- The active scanning provider must be resolved from configuration. An unrecognized or missing provider value must cause a startup exception — it must not silently fall back to a pass-through implementation.
- `NoOpMalwareScanner` and any equivalent pass-through implementation must never be the active `IMalwareScanner` in a non-test environment. Its use is restricted to automated tests only.
- The scanning provider is client-configurable in the same pattern as document storage — clients may select from a set of approved scanning services. Approved providers are defined in application configuration. A client may not configure a provider that is not on the approved list.

**Startup validation — required:**

```csharp
// Required — throw at startup if the configured provider is not recognized
// A misconfigured or missing provider must never silently disable scanning
var provider = malwareScanningOptions.Provider;
if (!KnownMalwareScanningProviders.IsSupported(provider))
    throw new InvalidOperationException(
        $"Malware scanning provider '{provider}' is not a recognized provider. " +
        "Malware scanning is mandatory. Configure a supported provider before starting the application.");
```

**Current violation — tracked as CODE-005:**
`DocumentManagerAPI` currently falls back to `NoOpMalwareScanner` when the configured provider is not `"Http"`. This is a standards violation. See `ProjectBacklog.md` CODE-005 for the required remediation.

---

### 5.4 Query String Handling
<!-- STD-MARKER: security.5.4 -->

Query string values must be treated as untrusted on the way in and safely encoded on the way out. Both directions are enforced at the library boundary wherever possible so compliance is automatic and callers cannot accidentally bypass it.

**Inbound — validate at the action boundary**

ASP.NET Core model binding decodes query string values automatically when parameters are declared on controller actions. Decoding is not the application's responsibility — validation is. Two complementary layers enforce this:

- **Global coarse-pass action filter** — runs on every request before any action executes. Rejects values that are obviously malformed: null bytes, values exceeding the maximum permitted length. This is a blunt first pass and does not replace per-endpoint validation.
- **`[ValidateQueryString]` attribute** — applied per-action or per-controller. Enforces endpoint-specific rules: allowlists, format checks, range checks. This is the required validation layer for any action that accepts query string parameters. Both layers are provided by the shared infrastructure library (tracked as CODE-008).

Any value read directly from `HttpContext.Request.Query` or `HttpContext.Request.QueryString` — bypassing model binding — must be validated manually using the same rules before it is passed to any downstream operation.

```csharp
// Violation — raw query string value used without validation
var filter = HttpContext.Request.Query["filter"].ToString();
var results = await _repository.SearchAsync(filter, cancellationToken);

// Required — attribute handles validation at the action boundary;
// direct Query access still requires explicit validation
[ValidateQueryString]
[HttpGet("invoices")]
public async Task<IActionResult> GetInvoices(
    [FromQuery] string filter, CancellationToken cancellationToken)
{
    // filter has been validated by the attribute before reaching here
    var results = await _invoiceService.GetInvoicesAsync(filter, cancellationToken);
    return Ok(results);
}
```

**Outbound — encode inside `HttpClientManager`**

When making outbound HTTP calls through `HttpClientManager`, query parameter values must never be appended to the URL by string concatenation or interpolation. `RequestManager` provides parameterised overloads that accept query parameters as `IDictionary<string, string?>` and encode them internally using `QueryHelpers.AddQueryString`. The caller supplies raw values; encoding is handled at the library boundary.

```csharp
// Violation — string interpolation with an unencoded value
var request = _requestManager.GetRequest($"invoices?clientId={clientId}&filter={filter}");

// Required — raw values supplied; RequestManager encodes them
var request = _requestManager.GetRequest("invoices", new Dictionary<string, string?>
{
    ["clientId"] = clientId.ToString(),
    ["filter"] = filter
});
```

The existing `GetRequest(string url)` overload is retained for cases where the caller constructs a fully formed URL with no dynamic query parameters. It must not be used when any query parameter value is caller-supplied or user-derived. The parameterised overloads are the required path for all dynamic query string construction. This pattern is enforced across `DocumentManagerAPI` and `CaptiveExpensesApi` as part of CODE-007.

`Uri.EscapeDataString` may be used when constructing URLs outside of the `HttpClientManager` context. `Uri.EscapeUriString` must not be used — it does not encode reserved characters and provides a false sense of safety.

---

## 6. Sensitive Data
<!-- STD-MARKER: security.6 -->

Both human developers and AI models must ensure that sensitive data is never written to any location that is not explicitly designed to hold it. The categories below define what qualifies as sensitive and where it must never appear.

### 6.1 Sensitive Data Categories
<!-- STD-MARKER: security.6.1 -->

| Category | Examples |
|---|---|
| Credentials | Passwords, password hashes, API keys, tokens, signing keys |
| Personal identifiable information (PII) | Full name combined with contact details, government IDs, date of birth combined with name, payment card data |
| Financial data | Invoice amounts, billing records, payment transactions, account balances |
| Health information | Any data that qualifies as PHI under HIPAA if applicable to the application domain |

### 6.2 Where Sensitive Data Must Not Appear
<!-- STD-MARKER: security.6.2 -->

Sensitive data must never appear in log output, cache entries, error responses, or source control. These prohibitions are each owned by a single authoritative standard and are not restated here:

- **Log output** — see [`GlobalLoggingStandards.md`](../standards/GlobalLoggingStandards.md).
- **Cache entries** — see [`GlobalCachingStandards.md`](../standards/GlobalCachingStandards.md#23-what-must-not-be-cached).
- **Error responses** — see [`GlobalCodingStandards.md`](../standards/GlobalCodingStandards.md#75-no-raw-stack-traces) Sections 7.5 and 7.7.
- **Source control** — see Section 2 of this file.

### 6.3 Data in Transit
<!-- STD-MARKER: security.6.3 -->

All data in transit must use TLS. Non-TLS connections are not permitted in any environment, including local development when connecting to shared or cloud-hosted services. Azure services enforce TLS by default — this must not be disabled.

### 6.4 Data at Rest
<!-- STD-MARKER: security.6.4 -->

Sensitive data stored in any persistent location must be encrypted at rest. Both human developers and AI models must verify the following before a PR is opened:

- **Azure SQL** — Transparent Data Encryption (TDE) must be enabled. TDE is on by default for Azure SQL — it must not be disabled.
- **Azure Blob Storage** — Storage Service Encryption must be enabled. This is on by default for Azure Storage — it must not be disabled.
- **Azure Redis Cache** — data at rest encryption must be enabled on the Redis instance. Non-encrypted Redis instances are not permitted for any data category.
- **Encryption keys** — encryption keys must be managed through Azure Key Vault. Customer-managed keys (CMK) must be used when data residency or compliance requirements mandate it. Microsoft-managed keys are permitted where no such requirement applies.
- **Application-level encryption** — fields classified as PII or financial data that are stored in the database must use column-level encryption when the data residency or compliance profile of the application requires it. The need for column-level encryption must be assessed at design time — it must not be retrofitted.

---

## 7. Cancellation Tokens
<!-- STD-MARKER: security.7 -->

Both human developers and AI models must pass a `CancellationToken` through every async call chain. Cancellation token forwarding is both a performance requirement — operations that ignore cancellation hold threads and resources after the caller has abandoned the request — and a security and reliability requirement, because unbounded long-running operations that cannot be cancelled are a denial-of-service risk under load. The general forwarding rule is defined in [`GlobalCodingStandards.md`](../standards/GlobalCodingStandards.md#84-cancellationtoken-forwarding) coding.8.4 and is not restated here. The rules below cover the top-level entry point token source and long-running operation requirements specific to security and reliability.

### 7.2 Top-Level Entry Point Exception
<!-- STD-MARKER: security.7.2 -->

The only permitted exception to the forwarding rule is at a true top-level entry point where the hosting framework is the originator of the operation. At these points the framework provides the token — the developer must use it and forward it into all downstream calls. `CancellationToken.None` must not be passed at any of these points.

| Entry Point Type | Required Token Source |
|---|---|
| ASP.NET Core controller action or minimal API handler | `HttpContext.RequestAborted` |
| Background service `ExecuteAsync` override | `stoppingToken` provided by the host |
| Azure Function — HTTP trigger | `CancellationToken` parameter passed by the Functions runtime to the function method |
| Azure Function — non-HTTP trigger (Queue, Service Bus, Timer, Blob) | `CancellationToken` parameter passed by the Functions runtime to the function method |
| Queue-triggered message processing | `CancellationToken` provided by the message processing host per message — must be forwarded into all downstream calls |

Scheduled jobs are not a recognized entry point in this application suite. Time-based work must be implemented using Azure Functions (Timer trigger) or Logic Apps, both of which provide a runtime-supplied token. If a scheduled job host is introduced in the future, this section must be updated to define the correct token source before any scheduled job is implemented.

```csharp
// Required — controller action uses HttpContext.RequestAborted
[HttpGet("clients/{clientId}/invoices")]
public async Task<IActionResult> GetInvoices(int clientId)
    => Ok(await _invoiceService.GetInvoicesAsync(clientId, HttpContext.RequestAborted));

// Required — background service uses stoppingToken from the host
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
        await _processor.ProcessNextAsync(stoppingToken);
}

// Required — Azure Function uses the runtime-provided CancellationToken parameter
[Function("ProcessBillingQueue")]
public async Task RunAsync(
    [QueueTrigger("billing-queue")] BillingMessage message,
    CancellationToken cancellationToken)  // Runtime-provided — must be forwarded
{
    await _billingService.ProcessAsync(message, cancellationToken);
}
```

### 7.3 Long-Running Operations
<!-- STD-MARKER: security.7.3 -->

Any operation that may run for more than a few seconds — batch processing, billing runs, bulk imports, external HTTP calls — must respect cancellation and exit cleanly when the token is signalled. Polling loops must check `cancellationToken.IsCancellationRequested` at the top of every iteration. Operations that call `ThrowIfCancellationRequested()` explicitly are also compliant.

```csharp
// Required — polling loop checks cancellation at the top of every iteration
while (!cancellationToken.IsCancellationRequested)
{
    await ProcessNextBatchAsync(cancellationToken);
    await Task.Delay(_options.PollingIntervalMs, cancellationToken);
}
```

---

## 8. Engineering Environment Security
<!-- STD-MARKER: security.8 -->

Both human developers and AI models must ensure that the engineering environment itself does not become an attack surface. The practices in this section follow SDL Practice 6 — Secure the Engineering Environment.

### 8.1 Dependency Management
<!-- STD-MARKER: security.8.1 -->

- All NuGet packages must be sourced from the official NuGet.org feed or a private Azure Artifacts feed that mirrors approved packages. Packages from unknown or unverified feeds must not be used.
- Package versions must be pinned. Floating version ranges (e.g., `*`, `1.x`) are not permitted in production project files.
- Dependencies must be reviewed for known vulnerabilities before being introduced. Running `dotnet list package --vulnerable` before opening a PR is required when any package is added or updated.

```shell
# Required — run before opening a PR when any package is added or updated
dotnet list package --vulnerable --include-transitive
```

### 8.2 Pipeline Security
<!-- STD-MARKER: security.8.2 -->

- Pipeline YAML files must not contain secret values — all secrets must be sourced from variable groups linked to Key Vault.
- Service connections used in pipelines must follow the least-privilege rule in Section 3.3.
- No pipeline step may execute arbitrary code retrieved from an external URL without a pinned version reference and explicit review.
- Pipeline approvals must be required for any deployment to production or staging environments.

### 8.4 Static and Dynamic Security Testing (SAST/DAST)
<!-- STD-MARKER: security.8.4 -->

Security testing must be integrated into the CI/CD pipeline — it must not be a manual step or a post-release activity.

- **SAST (Static Analysis):** A static analysis security scanner must run as part of every PR build. The build must not complete successfully if the scanner reports a critical or high severity finding in new or changed code. GitHub Advanced Security (GHAS) or an equivalent Azure DevOps-integrated scanner is the required tool.
- **DAST (Dynamic Analysis):** Dynamic analysis must be run against a deployed instance in the staging environment as part of the release pipeline before any promotion to production. DAST findings at critical or high severity must block promotion.
- **Dependency scanning:** `dotnet list package --vulnerable --include-transitive` must be executed as a pipeline step on every build. A build with known critical vulnerabilities in any dependency must not be promoted to production.

```shell
# Required pipeline step — fails build if critical or high vulnerabilities are found in dependencies
dotnet list package --vulnerable --include-transitive
```

### 8.5 Security Baseline — OWASP
<!-- STD-MARKER: security.8.5 -->

The OWASP Top 10 and OWASP Application Security Verification Standard (ASVS) are the baseline security verification frameworks for this application suite. Both human developers and AI models must be familiar with the current OWASP Top 10 risk categories. The rules in this standards file address the most critical of those categories directly:

| OWASP Top 10 Category | Where Addressed in This Standard |
|---|---|
| A01 Broken Access Control | Section 3 — Authentication and Authorization; Section 3.4 — Tenant and Client Isolation |
| A02 Cryptographic Failures | Section 6.3 — Data in Transit; Section 6.4 — Data at Rest |
| A03 Injection | Section 4 — Inline SQL Prohibition |
| A04 Insecure Design | Section 3.1 — Default to Authenticated; defense-in-depth throughout |
| A05 Security Misconfiguration | Section 2 — Secrets Management; Section 8.2 — Pipeline Security |
| A06 Vulnerable and Outdated Components | Section 8.1 — Dependency Management; Section 8.4 — SAST/DAST |
| A07 Identification and Authentication Failures | Section 3.1 — MFA requirement; Section 3.2 — Authorization Policies |
| A08 Software and Data Integrity Failures | Section 8.1 — Dependency pinning; Section 8.4 — Pipeline scanning |
| A09 Security Logging and Monitoring Failures | Section 6.2 — Sensitive data excluded from logs; observability rules in `GlobalLoggingStandards.md` Domain 7 |
| A10 Server-Side Request Forgery | Section 5.1 — Input validation and allowlist enforcement; Section 8.6 — SSRF responsibility boundary |

### 8.6 HTTP Client Security — HttpClientManager
<!-- STD-MARKER: security.8.6 -->

All application-to-application HTTP calls must be made through the `HttpClientManager` library. This covers any call where application code constructs the request, supplies the token, and processes the response — service-to-service calls, downstream API calls, and outbound webhook or scanner calls all fall in scope.

**Azure SDK clients are explicitly out of scope.** `Azure.Storage.Blobs`, `Azure.KeyVault`, `Azure.ServiceBus`, `Azure.Cosmos`, and all other Azure SDK clients manage their own HTTP transport internally. Do not attempt to route Azure SDK operations through `HttpClientManager` — the SDK owns authentication, retry, connection pooling, and transport security for those calls.

Direct instantiation of `HttpClient` in application code for in-scope calls is not permitted. `HttpClientManager` enforces the following security controls that must not be bypassed or duplicated in application code:

| Control | Where enforced | What it prevents |
|---|---|---|
| HTTPS-only base path | `IHttpClientBuilder.CreateOAuthClient`, `CreateOAuthClientWithFile` | Bearer tokens transmitted in plaintext over HTTP |
| Bearer token header injection validation | `IHttpClientBuilder.CreateOAuthClient`, `CreateOAuthClientWithFile` | Header injection via space, tab, CR, LF, or null in the token string; raw token value in logs on `FormatException` |
| URI scheme allowlist (`http`, `https` only) | `IRequestManager` — all methods | SSRF via `file://`, `ftp://`, or other non-HTTP schemes in request URLs |
| Protocol-relative URL rejection | `IRequestManager` — all methods | SSRF bypass where `//host/path` is misclassified as a relative URI, silently passing scheme validation |
| Response body size limit — declared (10 MB) | `IRequestProcessor` — all methods | OOM via oversized `Content-Length`; check fires before body is buffered (`ResponseHeadersRead` used on all requests) |
| Response body size limit — chunked (10 MB) | `IRequestProcessor` — all methods | OOM via chunked-encoding responses with no `Content-Length` header; body streamed with an in-library cap |
| `BaseAddress` trailing-slash normalisation | `IHttpClientBuilder` — both URI helpers | Silent RFC 3986 path-segment loss when combining base URI with relative request URI |
| Safe `Content-Disposition` filename | `IRequestManager.PostWithFileRequest` | Header injection via ASCII control characters; path traversal via `/`; NTFS Alternate Data Streams via `:` |

**SSRF responsibility boundary:** `HttpClientManager` does not block requests to private IP ranges (`10.x`, `172.16–31.x`, `192.168.x`, `169.254.x` link-local, cloud metadata endpoints). If any consuming application proxies user-supplied URLs to downstream services, an IP-address allowlist or denylist must be implemented at the `HttpMessageHandler` or network layer in that application — it is not the library's responsibility.

**Token field storage is a violation.** The access token must never be stored in an instance field and reused across requests. A stored token can expire between calls, does not benefit from `Microsoft.Identity.Web` silent renewal, and is a concurrency hazard on shared service instances. `GetAccessTokenForUserAsync` must be called on every outbound request — the call is cheap when the cached token is still valid.

```csharp
// VIOLATION — token stored in a field and reused across requests
private string? _accessToken;

public async Task<Tuple<HttpStatusCode, string>> SubmitGetRequest(...)
{
    _accessToken = await GetAccessToken(basePath);  // stale on next call if token expired
    var client = _httpClientBuilder.CreateOAuthClient(clientType, basePath, _accessToken);
    ...
}

// Required — token acquired per request via ITokenAcquisition; Microsoft.Identity.Web handles renewal
public async Task<Tuple<HttpStatusCode, string>> SubmitGetRequestAsync(
    string basePath, string clientType, string requestPath, CancellationToken cancellationToken)
{
    var token = await _tokenAcquisition.GetAccessTokenForUserAsync(_apiScopes);
    var client = _httpClientBuilder.CreateOAuthClient(clientType, basePath, token);
    var request = _requestManager.GetRequest(requestPath);
    return await _requestProcessor.GetAsync(client, request, cancellationToken);
}
```

**Auto-redirect warning:** `HttpClient` follows 301/302 redirects by default. A redirect from `https://` to `http://` silently transmits the Bearer token in plaintext. OAuth named clients must be registered with `AllowAutoRedirect = false` on the `HttpClientHandler`:

```csharp
// Required — disable auto-redirect on all OAuth named clients to prevent HTTPS→HTTP token downgrade
builder.Services.AddHttpClient("OAuthApi")
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AllowAutoRedirect = false
    });
```

---

## 9. Pipeline Security Gates
<!-- STD-MARKER: security.9 -->

Security enforcement does not begin at deployment — it begins the moment a code change is proposed. The three-layer gate model below is designed to catch unauthorized or dangerous code changes at the earliest possible point and prevent them from reaching production. This includes not only honest mistakes but also deliberate supply chain attacks: a tampered commit, a hijacked PR, or a malicious code block injected into the pipeline must be detected and stopped before it can affect a running system.

The pipeline rollback policy reinforces every layer. If any gate triggers a failure, the pipeline halts and the previous known-good deployment is automatically restored. The application remains operational throughout. A caught violation is a non-event from a continuity standpoint.

**Performance principle:** Security gates must be designed so that their cost is paid at the right time — in the pipeline or at startup — and never on the request hot path. Layers 1 and 2 run entirely in the pipeline and have zero runtime impact on the deployed application. Layer 3 runs once at startup and exits before the application serves any traffic. Runtime security controls that do touch every request must be kept as lightweight as possible, as documented in their respective sections. If a security control introduces measurable per-request overhead it must be reviewed for caching, short-circuiting, or deferral to a background process before it is accepted.

---

### 9.1 Layer 1 — Pre-Merge Gate (PR Validation Pipeline)
<!-- STD-MARKER: security.9.1 -->

This layer runs on every pull request before any code is merged. It is the first line of defence against introduced vulnerabilities, including supply chain tampering. All scans run in parallel where the toolchain supports it to minimise the impact on PR turnaround time.

**Branch and commit integrity**
- Direct commits to `dev` or `main` are blocked by branch protection policy — all changes must arrive via a pull request.
- Every PR requires at least one explicit approval from an authorized reviewer. Approval from the author is not valid.
- Commit signing is required. An unsigned commit cannot be merged. This ensures that a hijacked or forged commit is detectable before it enters the repository history.

**Static Application Security Testing (SAST)**
- A SAST scanner (Roslyn security analyzers at minimum) runs on every PR build.
- SAST runs as an incremental, file-scoped analysis against changed files only where the toolchain supports it, to keep pipeline duration proportional to change size rather than total codebase size.
- Critical and high findings in new or changed code block the merge. The PR cannot be completed until findings are resolved or formally accepted via a documented deviation.
- Architecture-specific rules are enforced alongside generic rules. Examples include detection of `NoOpMalwareScanner` registered as `IMalwareScanner` outside of test projects, detection of inline SQL, and detection of hardcoded credential patterns.

**Dependency vulnerability scanning**
- `dotnet list package --vulnerable --include-transitive` runs as a required pipeline step.
- Results are cached by lock file hash — if `packages.lock.json` has not changed, the cached result is used and the scan is skipped, eliminating redundant network calls on PRs that do not touch dependencies.
- Any critical or high CVE in a new or updated package blocks the merge.
- Floating version ranges (`*`, `[1.0,)`) are rejected — pinned versions are required so dependency substitution attacks cannot silently introduce a compromised version.

**Secret scanning**
- Secret scanning runs on every PR against changed files only. Any detected credential, connection string, or API key committed to source blocks the merge regardless of the file type.
- Pipeline YAML files are included in the scan scope. Secret values must never appear in pipeline definitions.

---

### 9.2 Layer 2 — Pre-Deployment Gate (Release Pipeline)
<!-- STD-MARKER: security.9.2 -->

This layer runs after merge and before any artifact is deployed to any environment. It operates against the actual build artifact, not the source at PR time, which means it catches anything introduced between validation and release.

**Re-scan on artifact**
- SAST and dependency vulnerability scans run again against the compiled output. A clean PR scan does not exempt the artifact from release-time scanning.
- Artifact scan results are compared to the PR scan baseline. If any scan produces a new critical or high finding that was not present at PR time, the release pipeline halts and the artifact is rejected. Comparing only the delta keeps this step fast — it does not re-evaluate findings that were already accepted at the PR gate.
- This catches tampering that occurs after the PR gate closes.

**DAST against staging**
- Dynamic Application Security Testing runs against the staging environment before every production promotion.
- DAST is scoped to the application surface that changed in the release. A full crawl is not required on every deployment — targeted scans against affected endpoints keep the gate proportional to the change.
- DAST results are reviewed as a required gate step. Critical findings block the promotion to production.

**Pipeline integrity**
- Pipeline definitions are stored in source control and subject to the same PR and branch protection rules as application code. A pipeline YAML file cannot be changed without going through the same review and approval process.
- Pipeline steps may not be modified at runtime. No step may fetch and execute an externally hosted script without an explicit pinned reference and a documented approval — this prevents a compromised external resource from injecting malicious steps into a trusted pipeline.

---

### 9.3 Layer 3 — Startup Gate (Runtime Validation)
<!-- STD-MARKER: security.9.3 -->

This layer runs inside the application itself, immediately at startup, before the application serves any traffic. It is the last line of defence and ensures that even a deployment that passes all pipeline checks cannot silently run in a degraded or misconfigured security posture.

Startup validators have no impact on request performance — they execute once during the application host startup sequence and are never invoked again after the application begins serving traffic. Validators must be synchronous or complete before the first request is accepted; they must not defer validation to a background thread where a window could exist between startup and the first request.

**Startup validators**
- Every security control that has a configurable state must have a corresponding startup validator that verifies the control is correctly configured before the application starts.
- Validators must be fast. They verify configuration state only — they do not make network calls, query databases, or perform any work that would extend startup time beyond what is necessary to confirm the security posture.
- If any validator fails, the application throws during startup, the health check fails, the pipeline detects the failure, and rollback is triggered automatically.
- Current required validators:

| Control | Validator behaviour |
|---|---|
| Malware scanning | Fails startup if `MalwareScanningOptions.Enabled` is `true` and no real `IMalwareScanner` implementation is resolved — `NoOpMalwareScanner` is not accepted |
| Multitenancy | Fails startup if `MultitenancyOptions.IsEnabled` is `true` and tenant or client values cannot be resolved |

As new security controls are introduced, a startup validator must be added for each one before the control is considered production-ready. This requirement is part of the definition of done for any security feature.

**Rollback as a safety net**
- The pipeline rollback policy and the startup validator pattern work as a deliberate pair. The startup validator prevents a misconfigured deployment from serving traffic. The pipeline rollback ensures the previous known-good version is immediately restored without manual intervention.
- This means a failed security activation — whether caused by a misconfiguration, a tampered artifact, or a partially applied change — results in automatic recovery with no downtime and no manual action required.
- The combination of these two mechanisms means that introducing security controls progressively (as with the multitenancy nullable-to-required promotion) carries no deployment risk. A failed activation is a non-event from a production continuity standpoint.

---

## 10. Compliance Verification
<!-- STD-MARKER: security.10 -->

Both human developers and AI models must run this checklist

**Secrets management:**
- [ ] No secret, credential, connection string, or API key appears in any committed file.
- [ ] All secrets are stored in Key Vault and accessed via the configuration pipeline — not via Key Vault SDK calls in services.
- [ ] Local development secrets use .NET User Secrets — not `appsettings.Development.json`.
- [ ] No secret value appears in any pipeline YAML file — variable groups linked to Key Vault are used.

**Authentication and authorization:**
- [ ] A global fallback authorization policy requiring authentication is registered in `Program.cs`.
- [ ] Every unauthenticated endpoint is explicitly decorated with `[AllowAnonymous]` and the reason is documented.
- [ ] All authorization policies are defined in dedicated policy class files — no inline role or claim checks in controllers or services.
- [ ] All authorization policies are unit testable in isolation.
- [ ] Least privilege is applied to all managed identities, database accounts, and pipeline service connections.
- [ ] No application code bypasses or works around the MFA policy enforced by Entra ID.
- [ ] JWT Bearer is configured with `ValidateIssuer = true`, `ValidateAudience = true`, `MapInboundClaims = false`, and `RequireHttpsMetadata = true`.
- [ ] `AddDistributedTokenCaches()` backed by Redis is used — not `AddInMemoryTokenCaches()`.
- [ ] `GetAccessTokenForUserAsync` is called on every outbound API request — tokens are not cached in fields or static variables.
- [ ] `MicrosoftIdentityWebChallengeUserException` is caught and results in a login redirect — not a 500 error.
- [ ] All application-to-application HTTP calls use `HttpClientManager` — no direct `HttpClient` instantiation in application code for in-scope calls. Azure SDK clients (`Azure.Storage.Blobs`, `Azure.KeyVault`, etc.) are not in scope and must not be routed through `HttpClientManager`.
- [ ] Access tokens are acquired via `GetAccessTokenForUserAsync` per request — not stored in instance fields or static variables.
- [ ] All OAuth named clients are registered with `AllowAutoRedirect = false` to prevent HTTPS→HTTP token downgrade on redirect.
- [ ] Any application that proxies user-supplied URLs implements an IP-address allowlist or denylist at the handler or network layer — private IP ranges are not blocked by `HttpClientManager`.

**Tenant and client isolation (all four layers):**
- [ ] Layer 3 — `ClientAccessAuthorizer` pattern is used: all three values must agree — the `ClientId` token claim, the user's active client record in the database, and the requested `ClientId`.
- [ ] Layer 4 — every new and updated endpoint and repository method declares `int? tenantId` and `int? clientId` as nullable leading parameters in that order.
- [ ] Existing methods where `clientId` is already a required `int` are not regressed — they remain as-is.
- [ ] No new data access path is added without both parameters declared, even while nullable.
- [ ] No repository method returns data across client boundaries.
- [ ] `MultitenancyOptions` is defined and bound from configuration in every participating application; `IsEnabled` defaults to `false`.
- [ ] All tenant and client compliance code is wrapped in a `MultitenancyOptions.IsEnabled` guard.
- [ ] A startup validator is registered that fails fast if `IsEnabled` is `true` and tenant or client values cannot be resolved.
- [ ] Promotion from nullable `int?` to required `int` is a call-site update only — no structural signature changes at promotion time.
- [ ] The config flag and nullable promotion are changed together as a single coordinated release step.
- [ ] No new data access path bypasses any of the four isolation layers.

**Inline SQL:**
- [ ] No inline SQL appears in any service, repository, or infrastructure class — parameterized or otherwise.
- [ ] All database operations execute through stored procedures via the repository pattern.

**Input validation and file handling:**
- [ ] All input is validated at the entry point before reaching the service layer.
- [ ] The global coarse-pass action filter is registered in every ASP.NET Core API application — rejects null bytes and overlength query string values on every request.
- [ ] `[ValidateQueryString]` is applied to every controller action that accepts query string parameters.
- [ ] Any value read directly from `HttpContext.Request.Query` or `HttpContext.Request.QueryString` is validated manually before use — null checks, length limits, format and allowlist checks as applicable.
- [ ] All outbound HTTP calls with dynamic query parameters use the parameterised `RequestManager` overloads — no string concatenation or interpolation with unencoded values.
- [ ] `Uri.EscapeUriString` is not used anywhere in the codebase.
- [ ] All uploaded files are validated on content type, file size, and file signature (magic bytes).
- [ ] All uploaded files are scanned for malware before being written to any storage location.
- [ ] Malware scanning provider is resolved from configuration — an unrecognized or missing provider causes a startup exception, not a silent pass-through.
- [ ] `NoOpMalwareScanner` or any equivalent pass-through implementation is not registered as the active `IMalwareScanner` in any non-test environment.
- [ ] Maximum upload size is sourced from configuration — not hardcoded.

**Sensitive data:**
- [ ] No sensitive data (credentials, PII, financial records) appears in any log property or message.
- [ ] No sensitive data is written to any cache entry.
- [ ] API error responses do not expose stack traces or internal exception messages — error messages to clients are generic; detail is in secure logs only.
- [ ] All data in transit uses TLS.
- [ ] Azure SQL Transparent Data Encryption (TDE) is enabled and has not been disabled.
- [ ] Azure Blob Storage encryption is enabled and has not been disabled.
- [ ] Azure Redis Cache data-at-rest encryption is enabled.
- [ ] Encryption keys are managed through Azure Key Vault.

**Cancellation tokens:**
- [ ] Every async method declares a `CancellationToken` as the last parameter.
- [ ] Every async call within those methods forwards the token — `CancellationToken.None` is not passed at any point.
- [ ] Long-running polling loops check `cancellationToken.IsCancellationRequested` at the top of every iteration.
- [ ] Controller actions use `HttpContext.RequestAborted` as the token source.
- [ ] Background services use the host-provided `stoppingToken`.
- [ ] Azure Function methods use the `CancellationToken` parameter provided by the Functions runtime.
- [ ] Queue-triggered message handlers forward the host-provided token into all downstream calls.

**Engineering environment and pipeline security gates:**
- [ ] Branch protection is enforced — no direct commits to `dev` or `main`; all changes arrive via an approved PR.
- [ ] Commit signing is enforced — unsigned commits cannot be merged.
- [ ] SAST scanner runs on every PR build against changed files only; no unresolved critical or high findings in new or changed code.
- [ ] Architecture-specific SAST rules are active — `NoOpMalwareScanner` as active `IMalwareScanner`, inline SQL, and hardcoded credentials are flagged as violations.
- [ ] `dotnet list package --vulnerable --include-transitive` runs as a required PR pipeline step; results are cached by lock file hash so the scan is skipped when dependencies have not changed.
- [ ] No NuGet package uses a floating version range.
- [ ] Secret scanning runs on every PR against changed files only; no credentials, connection strings, or API keys are committed to source.
- [ ] No pipeline YAML file contains secret values.
- [ ] SAST and dependency scans re-run against the release artifact before any deployment; only delta findings relative to the PR baseline are evaluated; a new finding blocks the release.
- [ ] DAST runs against the staging environment before every production promotion, scoped to the changed application surface; critical findings block promotion.
- [ ] Pipeline definitions are version-controlled and subject to the same PR and branch protection rules as application code.
- [ ] No pipeline step fetches and executes an externally hosted script without a pinned reference and documented approval.
- [ ] A startup validator is registered for every security control that has a configurable state; validators verify configuration state only and make no network calls or database queries.
- [ ] The pipeline rollback policy is active; a startup failure triggers automatic rollback to the previous known-good deployment.
- [ ] No security control introduces measurable per-request overhead without a documented review confirming caching, short-circuiting, or background deferral has been considered.

---

## 11. Governance
<!-- STD-MARKER: security.11 -->

This standard is owned by Troy Crowe. No changes to this file may be merged without Troy Crowe's explicit approval. Changes must be submitted as a pull request that includes a rationale comment explaining the reason for the update or deviation. Direct commits to `dev` or `main` are not permitted.
