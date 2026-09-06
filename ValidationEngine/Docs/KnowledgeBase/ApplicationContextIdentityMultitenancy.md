# Application Context — Identity, Roles, and Multi-Tenancy

**Repository:** GlobalStandards  
**Created:** 2026-05-09  
**Last modified:** 2026-05-09 (full administrative hierarchy, data management scope, seat allocation model, RBAC implementation, master catalog pattern, and revocation flow captured)
**Status:** Living document — additions expected as context is provided

---

## Purpose

This document captures authoritative application context for how users, clients, tenants, and deployment scenarios are managed. It serves as the knowledge base reference for decisions made in `GlobalSecurityStandards.md` and related standards, and as a source of truth for future backlog items and architectural decisions.

---

## 1. Identity and Directory Model

- The application uses **Azure AD B2C** for authentication via OIDC.
- **One B2C directory = one tenant.** The directory defines the tenant boundary.
- In its current state, the application is **single-tenant by configuration** — one B2C directory is configured.
- For true multi-tenant operation, the application must be registered under **multiple B2C directories**. Each tenant's users authenticate through their own directory.
- Even in a multi-tenant deployment, each individual request still operates as **effectively single-tenant** — the tenant context is resolved per request from the authenticated user's claims.
- The `tenantId` is present in the **user claims** after authentication. It does not need to be passed explicitly on every API call — it is read from claims and used for data isolation.

---

## 2. Client/User Relationship

- The application manages access through a **client/user relationship**, governed by the `ClientUserReference` table in the database.
- Every user belongs to a client. Every data access check validates the user's client membership before allowing access to data.
- **New user accounts require dual approval** before access is granted:
  1. Platform approval (SingleSourceManagement / Administrator)
  2. Client Administrator approval
- **Seat allocation** is enforced at the client level. When a client reaches their allocated seat limit, the user is notified that their administrator needs to extend their allocation. SSM controls seat allocation for its own clients. In the SaaS model, SSM allocates seats at the `SAASAdministrator` level and the `SAASAdministrator` controls how those seats are distributed across their own clients.

---

## 3. SingleSourceManagement

- `SingleSourceManagement` is both the **application owner** and a **client in the database**.
- All `SingleSourceManagement` users are global administrators.
- This is one reason the **three-point check** on the `clientId` claim exists — to distinguish SingleSourceManagement users from client users and enforce the correct access level.

### SSM client access model

- Clients managed directly by SSM have **read-only access to their own data**. They do not build, manage, or modify their data — that is fully managed by SSM on their behalf.
- The only exception being considered is allowing SSM clients to **open tickets**, which would be the sole write-capable action available to them outside of read-only access.
- This model is expected to carry forward. SSM retains full management responsibility for its clients' data.

### SSM data and access responsibilities

- **SSM manages the data of its own clients** — it does not manage SaaS clients' data. SaaS clients manage their own data.
- **SSM controls access at all levels** for its own clients — users, roles, and approval flags.
- **SSM controls seat allocation** for its own clients and at the `SAASAdministrator` level for SaaS clients. It does not control how a `SAASAdministrator` distributes their allocation across their own clients — that is the `SAASAdministrator`'s responsibility.

### SAASAdministrator relationship to SSM

- A `SAASAdministrator` has **mirrored functionality to SSM** but scoped exclusively to their own clients — they cannot access or manage any client outside of their own.
- SSM retains control over the `SAASAdministrator`'s own access at all times.
- A `SAASAdministrator` **manages the data of their own clients** — SSM does not manage it.
- A `SAASAdministrator` **controls access for all users beneath them** — their own users and their clients' users.
- A `SAASAdministrator` **controls seat allocation for their own clients**, distributing the allocation granted to them by SSM as they see fit.

### Origin of the ClientAdministrator role

- The `ClientAdministrator` role came about because an SSM client wanted the ability to manage which of their own employees could access the application.
- The driving need was the ability to **immediately cut off access for an individual no longer under their employment** at the moment that decision was made, without having to go through SSM.
- `ClientAdministrator` authority is strictly limited to approving or revoking their own users' access — they have no data management capability and no ability to assign or modify roles.

---

## 4. RBAC Implementation — B2C Custom Attributes and Microsoft Graph

### Background

Azure AD B2C has historically not had native RBAC support in the way that Azure AD (Entra ID) does. This has required a custom approach to role management, which is currently in place and working. The evolution of B2C toward Entra External ID and broader RBAC support is worth monitoring, as it may influence how this is handled in future.

### Current implementation

- A **custom user attribute** has been created in the B2C directory to hold role assignments. This attribute returns as a **comma-delimited list of role names** and is surfaced on the user profile.
- The attribute is **not visible or editable by the user**. It is an administrative-only field.
- Role values are managed exclusively through the **Microsoft Graph API**, accessed from the user administration area within the application's administration section.
- In the administration UI, roles are presented as **checkboxes** against each user, allowing an administrator to assign or revoke roles without directly manipulating raw attribute values.

### How roles flow into the application

1. The user authenticates through B2C.
2. The B2C token is issued with the custom roles attribute included in the claims.
3. The application reads the comma-delimited roles claim and parses it into the user's role set for the duration of the request.
4. Role-based decisions (access control, client resolution, approval evaluation) are made against this parsed role set.

### Limitations of the current approach

- Roles are a flat comma-delimited string. There is no native hierarchy or scope enforcement in the token itself — that logic lives entirely in the application.
- Because B2C custom attributes are directory-wide, the same attribute definition is shared across all users. Role values are per-user but the attribute schema is global.
- Any change to a user's roles requires a Microsoft Graph API write and does not take effect until the user's next token issuance (next login or token refresh). Microsoft Graph exposes a `revokeSignInSessions` action that invalidates refresh tokens, however its practical effect within B2C specifically is not fully confirmed. It is also likely possible to log the user out directly via Graph, which would serve the same purpose of forcing re-authentication and ensuring a fresh token is issued reflecting the updated role state. This needs to be verified against B2C behaviour before being relied upon in the revocation flow.

> **Important:** Even with `revokeSignInSessions` available, relying on token revocation alone for access-critical decisions adds operational complexity — it requires an explicit Graph call every time access needs to be withdrawn. The master catalog check (Section 7, Scenario 4) provides a simpler and more consistent model: client-level access decisions such as `SAASClient` status, approval flags, and licensing are read from the catalog on every request, so a revocation in the catalog takes effect immediately on the next request without requiring a separate session revocation call. Both mechanisms have their place — `revokeSignInSessions` is appropriate for user-level role changes where token claims must be refreshed; the catalog is the right place for client-level access decisions that must be enforced in real time across all users of that client.

### Revocation flow

When access needs to be revoked for a user, the following two-step flow applies:

1. **Catalog revocation (synchronous)** — The catalog database is updated first, immediately marking the user's access as revoked. This takes effect on the user's very next request regardless of their current session or token state. The calling method waits for this step to complete before returning — it is the critical step that provides immediate protection.
2. **Graph session revocation (asynchronous)** — A call to `revokeSignInSessions` via Microsoft Graph is dispatched as a non-blocking operation, either as a fired event or an Azure Function invocation. The calling method does not wait for this to complete. This step forces the user's refresh tokens to be invalidated so their next authentication produces a clean token reflecting their updated role state, but immediate access protection is already in place from step 1.

This approach ensures:
- **Immediate protection** — the catalog gate blocks the user on their very next request without any dependency on the Graph call completing.
- **Clean token state** — the asynchronous Graph call eventually ensures the user must re-authenticate, at which point their new token will reflect their updated role state.
- **No blocking on external dependencies** — the Graph API call, which is an external network operation, does not hold up the revocation response returned to the administrator.

### Expansion needed for SaaS

The current role administration UI and the underlying Graph-based role management will need to be expanded in two directions:

#### SAASAdministrator scope
- A `SAASAdministrator` must be able to manage roles for their own users and their clients' users only.
- The administration UI must scope the role management view to the `SAASAdministrator`'s managed clients — they should not see or be able to modify users belonging to other `SAASAdministrator` accounts or SingleSourceManagement.
- Role assignments at or above the `SAASAdministrator`'s own level (e.g. `Administrator`, `SAASAdministrator` itself) must not be available as options in their UI.

#### ClientAdministrator scope
- A `ClientAdministrator` needs a **subset** of the role administration capability — enough to approve or revoke their own users' access to the application, but not to assign administrative roles.
- This is the `IsApproved` flag in practice — the client administrator is not assigning roles in the full sense, but they are controlling whether their users are permitted to access the application.
- The `ClientAdministrator` approval page is intentionally simple — a list of all their users with a single **Access Approved** checkbox against each one. Nothing more is needed. This maps directly to the `IsApproved` flag and gives the client administrator no ability to assign or modify roles.

> **Current status:** Not yet implemented. The `IsApproved` flag exists and is enforced in the system but there is no UI or management page provided for `ClientAdministrator` to manage it yet. It is currently handled behind the scenes.

> **Monitoring:** Microsoft is migrating Azure AD B2C toward Entra External ID, which has been gaining capabilities that B2C historically has not had. Whether native RBAC support for external user directories is part of that roadmap has not been confirmed. This should be verified against current Microsoft documentation before any decisions are made about replacing the custom attribute approach.

---

## 5. Roles

### 5.1 Current State (Internal / Single Source Management)

In the current state, Single Source Management is the sole internal operator managing clients and their users. The administrative hierarchy is:

| Level | Role | Scope |
|---|---|---|
| 1 | `Administrator` | Global user access control. Manages the data of its own clients. Controls seat allocation for its own clients and at the `SAASAdministrator` level for SaaS. |
| 2 | `ClientAdministrator` | Client-scoped — manages their own users' access via `IsApproved` only. No data management capability. Exists so clients can immediately revoke access for departing employees without going through SSM. |

#### Approval flags (current)

Both flags must be `true` for a user to gain access to the application.

| Flag | Set by | Purpose |
|---|---|---|
| `IsAdminApproved` | Administrator (SingleSourceManagement) | Global-level approval. Controls user access at the platform level. |
| `IsApproved` | ClientAdministrator | Client-level approval. Allows the client to manage their own users' access. |

The combination of both flags ensures that neither the platform nor the client can unilaterally grant access — both must independently approve each user.

---

### 5.2 Future State (SaaS / Multi-Tenant)

When SaaS multi-tenancy is in place, a third administrative tier is introduced between `Administrator` and `ClientAdministrator`. The hierarchy becomes:

| Level | Role | Scope |
|---|---|---|
| 1 | `Administrator` | Global user access control. Manages the data of its own clients. Controls seat allocation for its own clients and at the `SAASAdministrator` level for SaaS. SSM does not manage SaaS clients' data. |
| 2 | `SAASAdministrator` | Global user access control of its clients and their users. Manages the data of its own clients. Controls how its SSM-allocated seats are distributed across its own clients. Scoped strictly to their own clients — SSM retains control over their access at all times. |
| 3 | `ClientAdministrator` | Client-scoped — unchanged from current. Manages their own users' access via `IsApproved` only. No data management capability. |

#### SAASClient classification

`SAASClient` is not an administrative role or a tier in the hierarchy. It is a **classification on the client account** stored in the master catalog that signifies the client is operating under a `SAASAdministrator` rather than being directly managed by SSM. It determines which approval chain applies for that client's users.

#### Access evaluation order (SaaS)

1. **`SAASClient` classification check** — Is this client classified as a SaaS client? This determines whether the SaaS approval chain applies. If not classified as a `SAASClient`, the standard approval chain applies instead.
2. **`IsAdminApproved`** — Has the global Administrator approved this user?
3. **`IsSAASApproved`** — Has the SAASAdministrator approved this user?
4. **`IsApproved`** — Has the ClientAdministrator approved this user?

All four conditions must be satisfied. The `SAASClient` gate is always evaluated first so that deprovisioned or unenrolled clients are rejected before the approval flags are ever checked. No single tier can unilaterally grant access.

---

## 6. Client Resolution and the Three-Point Check

### What the check actually governs

The three-point check controls **which client's data is in scope for a given request**. Its primary purpose is not simply to validate a claim — it determines how the `clientId` used to filter data is resolved, based on who the user is.

### Current state

#### Regular client users
- A client user has no client-selection capability.
- Their `clientId` is read from their identity and **automatically applied as a filter** on every request.
- They can only ever see data that belongs to their own client. There is no mechanism for them to request another client's data.

#### Administrator (SingleSourceManagement)
- Administrators have access to a **client selector dropdown** in the UI — a capability that is exclusive to SingleSourceManagement users.
- This allows an Administrator to choose which client they are currently managing.
- The three-point check recognises the `Administrator` role and **permits the selected `clientId` to differ from the user's own client**, allowing access to any client's data.
- Without this rule, the check would reject the request because the Administrator's own `clientId` (SingleSourceManagement) would not match the client whose data they are viewing.

### Future state (SaaS)

The following behaviour is not yet implemented and is planned for the SaaS release.

#### SAASAdministrator
- A `SAASAdministrator` will have the same client-selector capability as an Administrator, but **scoped to only the clients they manage**.
- The three-point check will recognise the `SAASAdministrator` role and permit the selected `clientId` to differ from the user's own client, **only if that client is one they are authorised to manage**.
- A `SAASAdministrator` will not be able to select or access a client that belongs to another `SAASAdministrator` or to SingleSourceManagement's direct client list.

### Summary of resolution rules

| User type | clientId source | Can select other clients? | Scope | State |
|---|---|---|---|---|
| Regular client user | Resolved from identity automatically | No | Own client only | Current |
| `Administrator` | Selected via dropdown | Yes | All clients | Current |
| `SAASAdministrator` | Selected via dropdown | Yes | Their managed clients only | Future |

### Proposed expansion to include tenant validation (future)

As multi-tenancy is introduced, the check should additionally validate that the resolved `clientId` belongs to the correct tenant:

1. Resolve `tenantId` from user claims
2. Verify the resolved `clientId` belongs to the resolved `tenantId`
3. Verify the user belongs to that client (`ClientUserReference`)
4. Verify the data being accessed belongs to that client

This requires adding a `tenantId` column to the `ClientUserReference` table so tenant-to-client membership can be validated server-side on every request.

---

## 7. Multi-Tenancy Deployment Scenarios

Three deployment scenarios are in scope. Additional scenarios may be identified and added as the product evolves.

### Scenario 1 — Shared application, shared data
- Multiple tenants share the same application instance and the same database.
- Data isolation is enforced entirely through the three/four-point security check in application code.
- This is the standard multi-tenant SaaS model and the primary scenario being designed for.

### Scenario 2 — Shared application, dedicated (secluded) data
- Multiple tenants share the same application instance, but each tenant has its own isolated database.
- The client record in the database includes configuration indicating a dedicated data source.
- On each request, the application calls **Azure Key Vault** to retrieve the connection string for that client's dedicated database, then connects to the isolated data source.
- Data isolation is enforced at the infrastructure level (separate database) in addition to application-level checks.

> **Security concern:** In this scenario, the shared database holds client records that include the configuration pointing to each client's dedicated data source. This makes the shared database a high-value security target — if it is compromised, an attacker could enumerate or redirect dedicated database connections. This concern is what motivates Scenario 4 below.

### Scenario 3 — Dedicated application, dedicated data
- The tenant has a fully dedicated application instance and a dedicated database.
- This is effectively a **single-tenant deployment** and requires no multi-tenancy accommodation in the application code.
- Treated as a standard single-tenant installation.

### Scenario 4 — Dedicated administrative database / master catalog (under consideration)

The core question this scenario addresses: **why would administrative configuration for a client with a dedicated data source ever live in a shared database alongside other clients' data?** It should not. This scenario separates that concern entirely.

#### The idea

A single **master catalog database** is introduced. It is lightweight, always accessed first on every request, and contains only administrative and routing data. It holds no client operational or business data whatsoever.

**What lives in the master catalog:**
- Client administrative data — routing configuration, which database instance the client uses, and any flags or configuration needed to resolve and validate a request. The full client record (profile, settings, business data) lives in the client's own data context, not here.
- **Client-level roles and classifications** — roles that describe the client account itself rather than individual users, for example `SAASClient`. These do not belong on the user profile because they are not a property of the user; they are a property of the client the user belongs to. Storing them here keeps them in the administrative layer where they are evaluated at resolution time.
- **Hierarchy relationships** — which `SAASAdministrator` is responsible for which clients. This is administrative structure data that belongs at the catalog level, not on user records.
- `ClientUserReference` — user-to-client membership and access control
- User-level approval flags (`IsAdminApproved`, `IsSAASApproved`, `IsApproved`)
- Licensing tables — seat allocations, entitlements, and enforcement

> **Design principle:** If a role or flag describes the client account rather than an individual user, it belongs in the catalog against the client. If it describes the user's standing within a client (approved, active, etc.), it belongs on the user record or `ClientUserReference`. This keeps the catalog as the authoritative source for all client-level administrative decisions and prevents administrative concerns from leaking into user profiles.

**What each request does:**
1. Authenticate the user (claims resolved from B2C token)
2. Hit the master catalog — resolve the client, validate access (role, approval flags, licensing), and retrieve the target database connection reference
3. Execute the request against the correct database instance for that client

The master catalog lookup is intentionally **lightweight** — it is a small, well-indexed set of tables with a narrow, well-defined purpose. It is not a general-purpose database.

#### Why this matters

- A client with a dedicated data source should have **no presence** in a shared operational database. Their configuration, routing, and access control all live in the master catalog, and their business data lives exclusively in their dedicated database.
- Compromising any single client database yields **no information** about other clients, their database locations, or any administrative configuration.
- Licensing enforcement happens at the master catalog level on every request — it is not scattered across individual client databases.
- The master catalog becomes the single authoritative source for access decisions across all deployment scenarios.

#### Relationship to other scenarios

| Scenario | Application database | Client data | Admin/routing data |
|---|---|---|---|
| 1 — Shared app, shared data | Shared | Shared database | Master catalog |
| 2 — Shared app, dedicated data | Shared | Per-client dedicated database | Master catalog |
| 3 — Dedicated app, dedicated data | Dedicated | Dedicated database | Master catalog (or local) |
| 4 — Master catalog | N/A | Unchanged per scenario | Master catalog (this is the catalog itself) |

> **Current status:** Under consideration. No architectural decision has been made. Needs evaluation of operational overhead, failover behaviour of the master catalog, and connection management strategy before being adopted as a standard.

---

## 8. Backlog Items Identified from This Context

The following items are tracked in `Working/ProjectBacklog.md` and are referenced here for traceability.

| Item | Description |
|---|---|
| Add `tenantId` to `ClientUserReference` | Required for tenant-to-client validation in the expanded four-point check |
| Define `SAASAdministrator` role scope | Defined in Section 4.2 — ready for standards reference and implementation planning |
| Implement `SAASAdministrator` role and `IsSAASApproved` flag | New for SaaS release — three-flag approval chain |
| Expand role administration UI for `SAASAdministrator` scope | SAASAdministrator must only see and manage users within their own clients; roles above their tier must not be assignable |
| Implement `ClientAdministrator` role administration subset | ClientAdministrator gets a scoped UI to approve/revoke their users' access without being able to assign administrative roles |
| Verify Entra External ID RBAC capability for external user directories | Microsoft is migrating B2C to Entra External ID but it is unconfirmed whether native RBAC for external users is part of that roadmap — verify against current Microsoft documentation before making any decisions about replacing the custom attribute approach |
| Implement two-step user revocation flow | Step 1: synchronous catalog revocation for immediate protection. Step 2: asynchronous Azure Function or event to call `revokeSignInSessions` via Microsoft Graph without blocking the calling method |
| Implement `SAASClient` classification and access evaluation routing | Client account classification stored in the master catalog that determines whether the SaaS three-flag approval chain or the standard approval chain applies |
| Implement `ClientAdministrator` role administration UI | Simple user list with a single Access Approved checkbox per user mapping to `IsApproved` — flag exists and is enforced but no management page has been built yet |
| Implement dual-approval user onboarding flow (current) | New user accounts require `IsAdminApproved` + `IsApproved` before access |
| Implement three-flag approval flow (SaaS) | Extend onboarding to require `IsAdminApproved` + `IsSAASApproved` + `IsApproved` |
| Implement seat/license enforcement with messaging | Notify user and client administrator when seat limit is reached |
| Key Vault connection string resolution for Scenario 2 | Retrieve per-client database connection strings at request time for dedicated data tenants |
| Evaluate master catalog database (Scenario 4) | Assess feasibility of a lightweight master catalog database for routing, access control, and licensing — completely separate from all client operational data |

---

## 9. Open Questions

| Question | Owner | Status |
|---|---|---|
| Can a single user belong to more than one client? | Troy | Open |
| Is `IsSAASApproved` stored on the user record or on a licensing/tenant relationship table? | Troy | Open |
| When a `SAASAdministrator`'s own `IsSAASApproved` flag is revoked by `Administrator`, are all of their clients' users immediately blocked? | Troy | Open |
| What is the seat/license unit — per client, per `SAASAdministrator` account, or both? | Troy | Open |
| Does `SAASAdministrator` have visibility into other `SAASAdministrator` accounts, or is each fully isolated? | Troy | Open |
| Does the `SASSAdministrator` spelling in early requirements represent the same role as `SAASAdministrator`? | Troy | Resolved — canonical spelling is `SAASAdministrator`. |
| Is `SAASClient` a role stored on the client record, a claim on the user token, or both? | Troy | Resolved — stored in the master catalog as a client-level classification, not on the user profile. |
| When a `SAASClient` is deprovisioned, should in-flight sessions be terminated immediately or at next token refresh? | Troy | Resolved — two complementary mechanisms apply. For client-level access decisions (`SAASClient` status, approval flags, licensing), the master catalog is checked on every request so revocation is immediate. For user-level role changes that live in the B2C token, `revokeSignInSessions` via Microsoft Graph can force immediate re-authentication so updated token claims are issued without waiting for natural token expiry. |
| Does `revokeSignInSessions` via Microsoft Graph have the expected effect in B2C specifically, or does it behave differently from Entra ID? Can a user be logged out directly via Graph as an alternative — verify both against B2C behaviour before relying on either in the revocation flow. | Troy | Open |
| Should `tenantId` validation be enforced as middleware or as part of the existing authorization policy? | Troy | Open |
| Should administrative and configuration data be moved to a dedicated administrative database (Scenario 4)? What is the operational and complexity cost? | Troy | Open |
| What is the failover and high-availability strategy for the master catalog — if it is unavailable, the entire application is unavailable? | Troy | Open |
| Should connection strings for client databases be stored directly in the master catalog or always retrieved from Key Vault at resolution time? | Troy | Open |
| In Scenario 3 (dedicated app + data), does the master catalog still apply or does the dedicated instance manage its own administrative data locally? | Troy | Open |
| Where does the canonical client record (profile, settings) live — in the client's own operational database, a shared application database, or somewhere else? | Troy | Open |
| If a dedicated administrative database is adopted, which application paths are considered privileged enough to access it, and how is that boundary enforced? | Troy | Open |

