# Coding Standards

**Version:** 1.5.0
**Status:** Active
**Applies To:** All repositories under the GlobalStandards governance model
**Audience:** AI models and human developers
**Created:** 2025-01-01
**Last Modified:** 2026-08-15

---
<!-- STD-MARKER: coding.file -->


## 1. Purpose
<!-- STD-MARKER: coding.1 -->

This standard defines the coding rules that must be applied across all repositories. Both human developers and AI models must apply every rule in this file when writing, reviewing, or generating code. It covers application architecture, coding style, SOLID principles, dependency management, functional programming patterns, and error handling. All new code and all refactoring work must conform to this standard. A work slice that introduces a violation must not be marked complete and a PR must not be opened until the violation is resolved. Compliance is verified during pull request review and the pre-merge compliance process.

---

## 2. Application Architecture
<!-- STD-MARKER: coding.2 -->

Both human developers and AI models must verify every item in this section before closing a work slice. Any violation found during a PR review must be resolved before the PR is approved.

### 2.1 Entry Point Responsibility
<!-- STD-MARKER: coding.2.1 -->

Application composition — dependency injection registration and middleware pipeline configuration — must be kept at the application entry point only. `Program.cs` must contain only wiring. Business logic, orchestration logic, and service behavior must not accumulate in `Program.cs`.

### 2.2 Layer Separation
<!-- STD-MARKER: coding.2.2 -->

Concerns must be separated across the following layers:

| Layer | Responsibility |
|---|---|
| Controllers / Endpoints | Receive requests, delegate to services, return responses |
| Services | Orchestration and business behavior |
| Repositories | Data access |
| Options / Configuration | Strongly typed configuration binding |
| Models | DTOs and domain entities |
| Diagnostics | Health, logging, and telemetry |
| Infrastructure | External I/O — HTTP clients, storage, messaging |

No layer may reach across more than one boundary. Controllers must not contain business logic. Services must not contain infrastructure implementation details.

### 2.3 Single Responsibility
<!-- STD-MARKER: coding.2.3 -->

Every class must have exactly one clearly named responsibility. If a class requires more than one reason to change, it must be split before the current work slice is closed.

### 2.4 One Type Per File
<!-- STD-MARKER: coding.2.4 -->

Every file must declare exactly one class, interface, record, struct, or enum. A file that declares more than one type must be split so each type has its own file named after that type. This requirement does not apply to a private or nested type that is only ever consumed by its single enclosing type.

### 2.5 No Speculative Abstractions
<!-- STD-MARKER: coding.2.5 -->

Abstractions must not be added without a present, demonstrable need. Before introducing a new layer or abstraction, the developer must verify that no existing component can satisfy the need.

### 2.6 Interface Design
<!-- STD-MARKER: coding.2.6 -->

Interfaces must be kept small and purposeful. An interface that covers more than one behavior boundary must be split. An interface must not be created solely to wrap a single concrete class that is never substituted.

### 2.7 Authorization Policies
<!-- STD-MARKER: coding.2.7 -->

Authorization policies must be defined in dedicated class files separate from `Program.cs` and from controller classes. Named policies must be registered at the application entry point. No authorization logic may be defined inline in a controller or action method. Security enforcement rules governing what those policies must contain, testability requirements, and least-privilege constraints are defined in [`GlobalSecurityStandards.md`](../standards/GlobalSecurityStandards.md#32-authorization-policies).

### 2.8 Data Access
<!-- STD-MARKER: coding.2.8 -->

All data access must be implemented using ADO.NET through the repository pattern. Object-relational mapping tools and frameworks must not be used. All database operations must be executed through stored procedures. Inline SQL must not be used. Full detail on data access rules is defined in [`GlobalDatabaseStandards.md`](../standards/GlobalDatabaseStandards.md#5-repository-pattern). Inline SQL prohibition and its security implications are defined in [`GlobalSecurityStandards.md`](../standards/GlobalSecurityStandards.md#4-inline-sql-prohibition).

---

## 3. Coding Style
<!-- STD-MARKER: coding.3 -->

Both human developers and AI models must apply every rule in this section to all new and changed code. A rule violation found during a PR review must be resolved before the PR is approved.

### 3.1 Repository Conventions First
<!-- STD-MARKER: coding.3.1 -->

Repository-specific conventions must be followed before falling back to standard .NET conventions. Standard .NET conventions apply only where the repository has no existing pattern.

### 3.2 Descriptive Naming
<!-- STD-MARKER: coding.3.2 -->

All classes, methods, parameters, variables, and files must use descriptive names that communicate intent without requiring a comment to explain them. Abstract or abbreviated names are not permitted.

### 3.3 Method Size and Focus
<!-- STD-MARKER: coding.3.3 -->

Methods must be short with one clear purpose. A method that requires scrolling to read in full must be split.

### 3.4 Guard Clauses
<!-- STD-MARKER: coding.3.4 -->

Guard clauses must appear at the top of every method that has preconditions. A guard clause validates a single precondition and immediately throws or returns before any business logic executes. This is the fail-fast principle — invalid input must be rejected at the boundary so the happy path runs clean without defensive nesting.

```csharp
public void ProcessInvoice(Invoice invoice, int clientId)
{
    if (invoice == null) throw new ArgumentNullException(nameof(invoice));
    if (clientId <= 0) throw new ArgumentOutOfRangeException(nameof(clientId));

    // happy path begins here
}
```

Precondition validation must not be deferred to deep implementation code. Nested conditional structures that exist only to validate inputs are not permitted when a guard clause achieves the same result.

### 3.5 Immutable DTOs
<!-- STD-MARKER: coding.3.5 -->

Data transfer objects that are not modified after construction must be implemented as immutable types using `init`-only properties or C# records.

### 3.6 Object Mapping
<!-- STD-MARKER: coding.3.6 -->

Object mapping must be performed exclusively in the constructor of the target type. The constructor of the target type must accept the source object as a parameter and map its own properties. This applies universally — business class to DTO, DTO to business class, and any other object-to-object mapping regardless of layer.

```csharp
// Mapping from business class to DTO — DTO constructor accepts the business object
public class InvoiceDto
{
    public int Id { get; }
    public decimal Amount { get; }

    public InvoiceDto(Invoice invoice)
    {
        Id = invoice.Id;
        Amount = invoice.Amount;
    }
}

// Mapping from DTO to business class — business class constructor accepts the DTO
public class Invoice
{
    public int Id { get; }
    public decimal Amount { get; }

    public Invoice(InvoiceDto dto)
    {
        Id = dto.Id;
        Amount = dto.Amount;
    }
}
```

Third-party mapping tools such as AutoMapper must not be used. Mapping logic must not be placed in services, controllers, or standalone mapping classes outside of the target type's constructor.

### 3.7 Visibility
<!-- STD-MARKER: coding.3.7 -->

Every member must have the minimum visibility required. Methods, properties, and fields must be private unless a concrete, present reason requires wider visibility. Visibility must not be widened without that reason being identifiable.

### 3.8 Comments
<!-- STD-MARKER: coding.3.8 -->

Comments must explain why, not what. A comment that restates what the code already says must not be added.

### 3.9 Using Statements
<!-- STD-MARKER: coding.3.9 -->

All `using` statements must be sorted and all unused `using` statements must be removed before any code is committed. Visual Studio provides **Remove and Sort Usings** via the right-click context menu as a convenient way to satisfy this requirement. Configuring **Run Code Cleanup on Save** under **Tools → Options → Text Editor → C# → Advanced** is a recommended approach to automate this. The mechanism used to satisfy this requirement is left to the developer; the outcome is mandatory.

### 3.10 Third-Party Library Approval
<!-- STD-MARKER: coding.3.10 -->

No third-party library may be introduced into any project — NuGet package, npm package, or any other external dependency — without prior approval. Approval must be obtained during the architecture planning process and recorded in the architectural design document for the repository before any development work using that library begins. A library that is not listed in the architectural design document as approved must not be used regardless of how common or convenient it may be.

The default position is that third-party libraries are not used. Libraries introduce maintenance risk, security surface area, and bloat. A library must only be approved when no reasonable first-party or platform-native alternative exists and the benefit clearly outweighs the long-term cost.

### 3.11 NuGet and npm Package Duplication
<!-- STD-MARKER: coding.3.11 -->

A new NuGet or npm package must not be introduced without first verifying it is not already available in the solution or project. Duplicate packages that provide the same capability are not permitted.

### 3.12 Centralized Package Feed
<!-- STD-MARKER: coding.3.12 -->

All approved NuGet and npm packages must be sourced from the organization's centralized Azure Artifacts feed. Projects must not pull packages directly from public feeds such as nuget.org or npmjs.com. This ensures all packages in use across all repositories are vetted, versioned, and consistent. Feed configuration and setup are defined in [`GlobalAzureDevOpsPipelineStandards.md`](../standards/GlobalAzureDevOpsPipelineStandards.md).

### 3.13 Purpose-Built Data Models
<!-- STD-MARKER: coding.3.13 -->

A purpose-built model must be used whenever the data being passed has a clear, bounded shape — meaning only the properties relevant to that use case are present, nothing more. A full domain entity must not be passed to a use case that only requires a subset of its properties.

The select list is a canonical example. Data retrieved from the database is mapped into a lean model (e.g., `ClientListItem`) containing only what a list option needs.

```csharp
// Lean model — only the properties this use case needs
public class ClientListItem
{
    public int    Id       { get; init; }
    public string Name     { get; init; }
    public bool   Selected { get; init; }
}
```

Purpose-built models must be named to reflect their type and role (e.g., `ClientListItem`, `InvoiceListItem`). Purpose-built models must not be duplicated across projects; if needed in more than one project they must move to a shared class library.

### 3.13.1 Reusable Extension Methods
<!-- STD-MARKER: coding.3.13.1 -->

Extension methods must be used whenever a transformation or data-shaping operation is consumed in more than one place. This does not violate single responsibility: the class has exactly one responsibility, which is extending the behavior of that domain type.

Extension methods on a purpose-built model (see [Section 3.13](#313-purpose-built-data-models)) produce whatever output format the caller requires — a `SelectListItem` collection for server-rendered dropdowns, or an HTML option string for JavaScript-populated dropdowns. Both live in `ClientExtensions` alongside any other Client-related extension methods.

```csharp
// ClientExtensions is the home for all Client-related extension methods
public static class ClientExtensions
{
    // Server-rendered dropdown
    public static IEnumerable<SelectListItem> ToSelectList(this IEnumerable<ClientListItem> items)
        => items.Select(i => new SelectListItem
        {
            Value    = i.Id.ToString(),
            Text     = i.Name,
            Selected = i.Selected
        });

    // JavaScript-populated dropdown
    public static string ToOptionString(this IEnumerable<ClientListItem> items)
    {
        var options = new StringBuilder();
        options.Append($"<option value='{string.Empty}'>-- SELECT A CLIENT --</option>");

        foreach (var item in items)
            options.Append($"<option value='{item.Id}'>{item.Name}</option>");

        return options.ToString();
    }

    // Any other reusable Client-related transformation belongs here too
    public static IEnumerable<Client> ActiveOnly(this IEnumerable<Client> clients)
        => clients.Where(c => c.IsActive);
}

// Same source data — different extension method per output format
var items = _clientRepository.GetClientListItems();

IEnumerable<SelectListItem> clientDropdown = items.ToSelectList();
string                      clientOptions  = items.ToOptionString();
```

Grouping rules:

- Each domain type gets exactly one extension class named after it (e.g., `ClientExtensions`, `InvoiceExtensions`)
- All extension methods related to that domain type must live in that class — no exceptions
- All extension classes must be placed in the `Extensions/` folder within the project where they apply
- Extension classes must not be duplicated across projects; if needed in more than one project they must move to a shared class library
- A duplicate transformation found during a PR review must be consolidated before the PR is approved

This pattern is a direct application of the functional programming rules in [Section 5](#5-functional-programming), the object mapping rules in [Section 3.6](#36-object-mapping), and the single-responsibility principle in [Section 4.1](#41-single-responsibility).

### 3.14 Constants and Magic Values
<!-- STD-MARKER: coding.3.14 -->

All string literals and numeric values that appear in more than one place, are used as identifiers (route fragments, claim names, header names, policy names, role names, configuration keys), or represent a named concept must be defined as named constants. A value that appears only once in a single method and has no named meaning may be used inline.

Constants must be defined in a dedicated `Constants/` folder within the project. Each constants class must be named after the group it represents (e.g., `ClaimNames`, `PolicyNames`, `RouteSegments`, `ConfigurationKeys`). Constants must be `public static class` types with `public const` members.

```csharp
// Constants/PolicyNames.cs
public static class PolicyNames
{
    public const string RequireClientAccess = "RequireClientAccess";
    public const string RequireAdministrator = "RequireAdministrator";
}

// Usage
[Authorize(Policy = PolicyNames.RequireClientAccess)]
```

A repeated string literal that must be a constant is a code smell. See [Section 9](#9-code-smell-watchlist).

---

## 4. SOLID Principles
<!-- STD-MARKER: coding.4 -->

The authoritative source is Robert C. Martin (Uncle Bob) — see [REF-002](../references/StandardsReferences.md#ref-002--solid-principles-original-source-reference-uncle-bob) for the original reference.

### 4.1 Single Responsibility
<!-- STD-MARKER: coding.4.1 -->

Every class, service, and module must have exactly one reason to change.

### 4.2 Open/Closed
<!-- STD-MARKER: coding.4.2 -->

New behavior must be introduced by adding new types, not by modifying stable existing types.

### 4.3 Interface Segregation
<!-- STD-MARKER: coding.4.3 -->

Code must depend only on the interface members it actually uses. Large interfaces that serve multiple consumers must be split.

### 4.4 Dependency Inversion
<!-- STD-MARKER: coding.4.4 -->

Dependencies must be on abstractions only where external dependencies, testability seams, or substitutability justify an abstraction. Existing BCL or framework abstractions must not be wrapped without a demonstrated reason.

### 4.5 Dependency Injection
<!-- STD-MARKER: coding.4.5 -->

Dependencies must be registered in the DI container at the application entry point. Service locator patterns must not be used inside services. Dependencies must be injected via constructor.

---

## 5. Functional Programming
<!-- STD-MARKER: coding.5 -->

Functional programming patterns must be applied where they fit naturally alongside SOLID principles. Both human developers and AI models must evaluate whether a transformation or calculation can be expressed as a pure function before introducing a stateful alternative. Functional patterns must not be forced into areas where they conflict with existing SOLID structure.

The most common practical use case is a pure transformation function — a method with no side effects that takes an input and returns a transformed output. This pattern is well suited to service-layer data preparation and calculation logic.

```csharp
// Pure function — no side effects, same input always produces same output
public static class InvoiceCalculator
{
    public static decimal CalculateTotal(IEnumerable<LineItem> lineItems)
        => lineItems.Sum(item => item.Quantity * item.UnitPrice);

    public static InvoiceDto ToSummary(Invoice invoice)
        => new InvoiceDto(invoice);
}
```

Applicable patterns include:

- Immutable data structures for values that do not change after construction
- Pure functions — methods with no side effects — for transformation and calculation logic
- Function composition over inheritance for behavior that can be expressed as a pipeline of transformations
- Avoiding shared mutable state where a functional alternative is equally clear

Functional patterns must not replace dependency injection, constructor initialization, or repository patterns where those are the established structure of the codebase. For additional patterns and examples see [REF-003](../references/StandardsReferences.md#ref-003--functional-programming-in-c-microsoft-learn).

---

## 6. Testing Requirement
<!-- STD-MARKER: coding.6 -->

Automated tests must be written for all new and changed code. Testing is not optional. Both human developers and AI models must verify that test coverage exists for all new and changed public behavior before marking a work slice complete. No work slice may be considered complete and no PR may be opened without that coverage in place. Full testing rules, naming conventions, project structure, and coverage expectations are defined in [`GlobalTestingStandards.md`](../standards/GlobalTestingStandards.md).

---

## 7. Error Handling
<!-- STD-MARKER: coding.7 -->

Every application must have a global error handler registered as the outermost layer of the request pipeline. Both human developers and AI models must verify that every new or changed method complies with the rules in this section before closing a work slice. The global error handler requirement and its placement are defined in [`GlobalSolutionStructureStandards.md`](../standards/GlobalSolutionStructureStandards.md#12-global-error-handling). The rules in this section govern how all code within the application handles, throws, and propagates exceptions.

### 7.1 Exception Types
<!-- STD-MARKER: coding.7.1 -->

The most precise exception type available must be used. `Exception` and `ApplicationException` must not be thrown directly.

### 7.2 Input Validation
<!-- STD-MARKER: coding.7.2 -->

All inputs must be validated at entry points — controller actions, service method parameters, and job entry methods — using guard clauses. Validation must not be deferred to deep implementation code.

### 7.3 No Swallowed Exceptions
<!-- STD-MARKER: coding.7.3 -->

Exceptions must not be caught and discarded silently. Every caught exception must be logged or rethrown.

### 7.4 Centralized Error Formatting
<!-- STD-MARKER: coding.7.4 -->

API error responses must be formatted using `ProblemDetails` middleware or a global exception handler. Error responses must not be formatted inline in individual controllers.

### 7.5 No Raw Stack Traces
<!-- STD-MARKER: coding.7.5 -->

Unhandled exceptions must not surface raw stack traces to clients.

### 7.6 Rethrowing
<!-- STD-MARKER: coding.7.6 -->

When rethrowing an exception, `throw;` must be used to preserve the original stack. `throw ex;` must not be used.

### 7.7 User-Facing Error Messages
<!-- STD-MARKER: coding.7.7 -->

Errors must bubble up to the UI in a form that gives the user enough information to understand what went wrong and what they can do next. Messages must be clear and actionable. They must not expose internal details, exception messages, stack traces, or system implementation specifics.

The distinction is between useful and harmful information:

| Harmful — must not be shown | Useful — must be shown |
|---|---|
| `NullReferenceException in InvoiceService.cs line 42` | `The invoice could not be saved. Please try again.` |
| `SQL timeout on stored procedure usp_GetClients` | `Client data is currently unavailable. Please try again shortly.` |
| `Object reference not set to an instance of an object` | `An unexpected error occurred. If this continues, contact support.` |

The global error handler is responsible for catching unhandled exceptions and producing a user-facing message. Individual features and service calls that can anticipate failure conditions must provide specific, context-aware messages rather than relying solely on the global fallback.

---

## 8. Async/Await Rules
<!-- STD-MARKER: coding.8 -->

All new asynchronous code must use `async`/`await` throughout. Both human developers and AI models must verify every item in this section before closing a work slice. A violation found during a PR review must be resolved before the PR is approved.

### 8.1 Method Naming
<!-- STD-MARKER: coding.8.1 -->

Every `async` method must end with the `Async` suffix. A method that returns `Task` or `Task<T>` without the `Async` suffix is a violation.

```csharp
// Correct
public async Task<Invoice> GetInvoiceAsync(int id, CancellationToken cancellationToken)

// Violation
public async Task<Invoice> GetInvoice(int id)
```

### 8.2 Prohibited Blocking Calls
<!-- STD-MARKER: coding.8.2 -->

Blocking calls on asynchronous code must not be used. The following are prohibited in all application code:

| Prohibited pattern | Reason |
|---|---|
| `.Result` on a `Task` or `Task<T>` | Blocks the calling thread; risks deadlock on ASP.NET synchronization contexts |
| `.Wait()` on a `Task` | Same risk as `.Result` |
| `.GetAwaiter().GetResult()` | Equivalent to `.Result`; same deadlock risk |
| `Thread.Sleep` in async code | Blocks a thread; use `await Task.Delay` instead |

### 8.3 Async Void
<!-- STD-MARKER: coding.8.3 -->

`async void` methods must not be used except for event handlers where the framework requires it. `async void` prevents exception propagation and cannot be awaited. Event handlers that must use `async void` must wrap their body in a `try`/`catch` that logs all exceptions.

### 8.4 CancellationToken Forwarding
<!-- STD-MARKER: coding.8.4 -->

Every `async` method must accept a `CancellationToken` parameter and forward it to every awaited call that accepts one. A `CancellationToken` must never be ignored or substituted with `CancellationToken.None` inside an async call chain except at the top-level entry point (controller action or background job entry) where the token is first received from the framework. Full cancellation token rules are defined in [`GlobalSecurityStandards.md`](../standards/GlobalSecurityStandards.md).

### 8.5 ConfigureAwait
<!-- STD-MARKER: coding.8.5 -->

Library code (NuGet packages, shared class libraries) must use `ConfigureAwait(false)` on all `await` calls to avoid capturing the synchronization context. Application code (API projects, web projects) does not require `ConfigureAwait(false)` because ASP.NET Core does not use a synchronization context.

---

## 9. Code Smell Watchlist
<!-- STD-MARKER: coding.9 -->

Both human developers and AI models must run this checklist against every work slice before it is closed. The presence of any item on this list in new or changed code is a violation that must be resolved before the PR is opened.

- `Program.cs` accumulating orchestration or business logic beyond DI wiring — see [Section 2.1](#21-entry-point-responsibility)
- Infrastructure classes combining transport, retry, telemetry, parsing, and policy logic without separation — see [Section 2.2](#22-layer-separation)
- Configuration values scattered without grouping or strongly typed options
- Repeated string literals for keys, claim names, route fragments, or header names that must be constants — see [Section 3.14](#314-constants-and-magic-values)
- New abstractions introduced without a present, documented need — see [Section 2.5](#25-no-speculative-abstractions)
- Methods or classes that grew beyond one responsibility during the current work slice — see [Section 2.3](#23-single-responsibility)
- Members with wider visibility than required — see [Section 3.7](#37-visibility)
- Blocking calls (`.Result`, `.Wait()`, `.GetAwaiter().GetResult()`) on async code — see [Section 8.2](#82-prohibited-blocking-calls)
- `async void` outside of a required event handler — see [Section 8.3](#83-async-void)
- Async methods missing the `Async` suffix — see [Section 8.1](#81-method-naming)
- `CancellationToken` not forwarded through an async call chain — see [Section 8.4](#84-cancellationtoken-forwarding)

---

## 10. Compliance Verification
<!-- STD-MARKER: coding.10 -->

- [ ] `Program.cs` contains only DI wiring and middleware configuration; no business or orchestration logic is present.
- [ ] Each class has a single, named responsibility.
- [ ] No new abstractions were added without a present, demonstrable need.
- [ ] No existing component was duplicated when a reusable one existed.
- [ ] All new names are descriptive and communicate intent without requiring a comment.
- [ ] No method requires more than one clear purpose.
- [ ] Guard clauses appear at the top of all methods with preconditions.
- [ ] All DTOs that are not modified after construction are immutable.
- [ ] All object mapping is performed in the constructor of the target type; no third-party mapping tools are used.
- [ ] Purpose-built lean models are used for data with a bounded shape; no full domain entity is passed to a use case that only needs a subset of its properties.
- [ ] Extension methods are used for transformations consumed in more than one place, grouped one class per domain type under `Extensions/`.
- [ ] No member visibility was widened without a specific present reason.
- [ ] All members are private unless a concrete reason requires wider visibility.
- [ ] Comments added explain why, not what.
- [ ] All `using` statements are sorted and all unused `using` statements have been removed.
- [ ] Every third-party library used is listed as approved in the architectural design document; no unapproved library has been introduced.
- [ ] No new NuGet or npm package was added without confirming it is not already available in the solution or project.
- [ ] All NuGet and npm packages are sourced from the organization's centralized Azure Artifacts feed; no direct references to public feeds exist.
- [ ] No interface was created purely to wrap a single, never-substituted class.
- [ ] DI registration occurs at the entry point; no service locator patterns exist inside services.
- [ ] Dependencies are injected via constructor.
- [ ] Authorization policies are defined in dedicated class files, registered at the entry point, and no authorization logic exists inline in controllers or action methods.
- [ ] All data access is implemented using ADO.NET through the repository pattern; no ORM frameworks are used.
- [ ] All database operations execute through stored procedures; no inline SQL exists anywhere in the codebase.
- [ ] Functional patterns are applied only where they fit naturally alongside SOLID; they do not replace DI or repository patterns.
- [ ] Automated tests have been written for all new and changed code; no PR has been opened without test coverage.
- [ ] All exceptions thrown use specific types, not base `Exception`.
- [ ] Input validation exists at all entry points using guard clauses.
- [ ] No exception is caught and discarded silently.
- [ ] API error responses flow through centralized `ProblemDetails` formatting.
- [ ] No raw stack trace can reach a client response.
- [ ] All rethrows use `throw;` to preserve the stack.
- [ ] All string and numeric identifiers used in more than one place or representing a named concept are defined as named constants in the `Constants/` folder.
- [ ] Every `async` method name ends with the `Async` suffix.
- [ ] No blocking calls (`.Result`, `.Wait()`, `.GetAwaiter().GetResult()`) exist on any `Task` or `Task<T>`.
- [ ] No `async void` methods exist outside of required event handlers.
- [ ] Every `async` method accepts a `CancellationToken` parameter and forwards it to all awaitable calls that accept one.
- [ ] Library code uses `ConfigureAwait(false)` on all `await` calls.
- [ ] Code smell watchlist has been checked and all findings resolved before closing the work slice.

---

## 11. Governance
<!-- STD-MARKER: coding.11 -->

This standard is owned by Troy Crowe. No changes to this file may be merged without Troy Crowe's explicit approval. Changes must be submitted as a pull request that includes a rationale comment explaining the reason for the update or deviation. Direct commits to `dev` or `main` are not permitted.
