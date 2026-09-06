# Testing Standards

**Version:** 2.0.0
**Status:** Active
**Applies To:** All repositories under the GlobalStandards governance model
**Audience:** AI models and human developers
**Created:** 2025-01-01
**Last Modified:** 2026-07-22

---
<!-- STD-MARKER: testing.file -->


## 1. Purpose
<!-- STD-MARKER: testing.1 -->

This standard defines the testing rules that must be applied across all repositories. Both human developers and AI models must apply every rule in this file when writing, reviewing, or modifying test code. It covers test project structure, naming conventions, test categories, test structure, unit tests, workflow tests, and test doubles. All new code and all refactoring work must include tests that conform to this standard. A work slice that introduces untested code must not be marked complete and a PR must not be opened until tests exist and pass. Compliance is verified during pull request review and the pre-merge compliance process. The testing requirement is stated in [`GlobalCodingStandards.md`](../standards/GlobalCodingStandards.md#6-testing-requirement).

Testing is implemented in two defined categories of enforcement. Phase 1 requirements in this file are enforced now. Phase 2 requirements in this file apply only to repositories that have the required dedicated automated environment and an explicit repository-local enforcement record that activates them.

Automated test execution belongs to the `feature/* → dev` validation path. Production delivery pipelines may perform deployment validation or smoke checks needed to confirm successful release behavior, but they must not become the primary place where unit-test correctness is verified.

---

## 2. Test Project Structure
<!-- STD-MARKER: testing.2 -->

Both human developers and AI models must verify that all test code is placed in a conforming test project before a PR is opened.

### 2.1 Framework
<!-- STD-MARKER: testing.2.1 -->

xUnit must be used as the test framework across all repositories. No other test framework may be introduced.

### 2.2 Test Project Placement
<!-- STD-MARKER: testing.2.2 -->

All test code must be placed in a dedicated test project named `[ProjectName].Tests`. Test files must never be added to a production project. Test projects must follow the placement rules defined in [`GlobalSolutionStructureStandards.md`](../standards/GlobalSolutionStructureStandards.md#4-test-projects).

```
CaptiveExpensesAPI/
├── CaptiveExpensesAPI/
│   └── CaptiveExpensesAPI.csproj
└── CaptiveExpensesAPI.Tests/
    └── CaptiveExpensesAPI.Tests.csproj
```

### 2.3 Test Project Folder Layout
<!-- STD-MARKER: testing.2.3 -->

Test files must mirror the folder structure of the production project. A test file for a class in `Services/` must live in a `Services/` folder within the test project.

```
CaptiveExpensesAPI.Tests/
├── Controllers/
├── Services/
├── Repositories/
└── Fakes/
```

The `Fakes/` folder contains all fake implementations used as test doubles. Fakes are reusable across the test project and must not be embedded inside individual test files.

---

## 3. Naming Conventions
<!-- STD-MARKER: testing.3 -->

Both human developers and AI models must apply every naming rule in this section to all test classes, test methods, and fake implementations. A naming violation found during a PR review must be resolved before the PR is approved.

### 3.1 Test Class and File Naming
<!-- STD-MARKER: testing.3.1 -->

Test classes and their files must be named after the subject under test using the pattern `[SubjectName]Tests`.

```
ClientService        → ClientServiceTests.cs
InvoiceRepository    → InvoiceRepositoryTests.cs
ClientsController    → ClientsControllerTests.cs
```

### 3.2 Test Method Naming
<!-- STD-MARKER: testing.3.2 -->

Test method names must follow the pattern `[MethodUnderTest]_[Scenario]_[ExpectedResult]`. The name must identify the method under test, the scenario being tested, and the expected result. A test name that does not communicate its full purpose without opening the method body is not acceptable. The test name is the first thing a developer sees when a test fails in CI — it must tell them immediately what broke and why.

```csharp
// Not acceptable — tells you nothing
public void TestGetById()
public void ClientTest1()
public void AddClientFails()

// Required — purpose is immediately clear
public void GetById_WhenClientExists_ReturnsClient()
public void GetById_WhenIdIsZero_ThrowsArgumentOutOfRangeException()
public void Add_WhenClientIsNull_ThrowsArgumentNullException()
public void Add_WhenClientNameExceedsMaxLength_ThrowsValidationException()
public void CalculateTotal_WhenAllLineItemsAreZero_ReturnsTotalOfZero()
```

### 3.3 Fake Naming
<!-- STD-MARKER: testing.3.3 -->

Fake implementations must be named using the pattern `Fake[InterfaceName]` with the `I` prefix removed.

```
IClientRepository    → FakeClientRepository
IInvoiceService      → FakeInvoiceService
```

### 3.4 Observability Test Naming
<!-- STD-MARKER: testing.3.4 -->

Both human developers and AI models must apply this naming pattern to any test class or method whose sole purpose is to verify observability behavior — that an exception is caught, rethrown, and logged with structured context — rather than to verify a method's primary business behavior.

Observability test classes must be named `[TypeName]ObservabilityTests` instead of `[SubjectName]Tests`. Observability test methods must be named `[MethodOrOperation]When[Dependency]ThrowsThenRethrowsAndLogsError` instead of `[MethodUnderTest]_[Scenario]_[ExpectedResult]`.

```
AccountService              → AccountServiceObservabilityTests.cs
AddClient_WhenRepositoryThrows_RethrowsAndLogsError
    → AddWhenRepositoryThrowsThenRethrowsAndLogsError
```

This is a distinct naming category, not a replacement for [Section 3.1](#31-test-class-and-file-naming) and [Section 3.2](#32-test-method-naming). A production type that has both ordinary behavior tests and observability tests must have two separate test classes: `[SubjectName]Tests` for behavior and `[TypeName]ObservabilityTests` for the cross-cutting observability concern. Non-observability tests must continue to follow the naming rules in Section 3.1 and Section 3.2 without exception.

### 3.5 Service Guard Test Naming
<!-- STD-MARKER: testing.3.5 -->

Both human developers and AI models must apply this naming pattern to any test that verifies a service's precondition/guard-clause behavior (constructor argument checks, method argument checks) rather than its primary business behavior.

Guard-focused test methods must be named `[Service][Method]When[Condition]Then[ExpectedGuardException]` instead of `[MethodUnderTest]_[Scenario]_[ExpectedResult]`.

```
ClientServiceAddWhenClientIsNullThenThrowsArgumentNullException
```

This is a distinct naming category, not a replacement for [Section 3.2](#32-test-method-naming). Non-guard tests must continue to follow the naming rule in Section 3.2 without exception. Guard tests for a given service may be grouped into a shared guard-test class rather than colocated with that service's behavior tests, provided the guard-test class name and location are consistent within the repository.

---

## 4. Test Categories and Traits
<!-- STD-MARKER: testing.4 -->

Both human developers and AI models must apply the correct `Category` trait to every test method. A test with no trait is not permitted and must be flagged during PR review.

xUnit traits must be used to categorize every test.

```csharp
[Fact]
[Trait("Category", "Unit")]
[Trait("Category", "Regression")]
public void CalculateTotal_WithTwoLineItems_ReturnsCorrectSum()
```

The following category values are the defined standard. No other category values may be used without an approved addition to this standard.

| Trait Value | Applies To | Phase |
|---|---|---|
| `Unit` | Tests that verify a single class or method in isolation | **Phase 1 — required now** |
| `Contract` | Tests that verify API endpoint contracts — correct status codes, response shapes, and rejection behavior for invalid input | **Phase 1 — required now** |
| `Workflow` | Tests that verify one complete business operation through all application layers against real infrastructure | Phase 2 |
| `Regression` | Tests that verify a previously broken behavior is fixed and stays fixed | Phase 2 |
| `Performance` | Tests that verify critical operations meet defined response time and memory thresholds | Phase 2 |

Every test must carry at least one `Category` trait. A test with no trait is not permitted.

In CI, the `Unit` category must be run on every PR build. The `Workflow`, `Regression`, and `Performance` categories must run only in the repository-specific pipeline stages that explicitly activate them.

The `dev → main` promotion and delivery path must rely on the test results established during the `feature/* → dev` validation flow unless an approved repository-specific deviation adds an additional release-stage test requirement.

---

## 5. Arrange-Act-Assert
<!-- STD-MARKER: testing.5 -->

Both human developers and AI models must structure every test method using Arrange-Act-Assert. A test that does not follow this structure must be refactored before the PR is approved.

Every test method must follow the Arrange-Act-Assert (AAA) structure.

```csharp
[Fact]
[Trait("Category", "Unit")]
public void GetAll_WhenClientsExist_ReturnsAllClients()
{
    // Arrange
    var repository = new FakeClientRepository();
    repository.Add(new Client { Id = 1, Name = "Acme Corp" });
    repository.Add(new Client { Id = 2, Name = "Globex" });
    var service = new ClientService(repository);

    // Act
    var result = service.GetAll();

    // Assert
    Assert.Equal(2, result.Count());
}
```

A test method that contains more than one Act or more than one Assert must be split into separate test methods. Each test method must verify exactly one behavior.

---

## 6. Test Independence and Workflow Tests
<!-- STD-MARKER: testing.6 -->

Both human developers and AI models must verify that all unit tests are fully independent before a PR is opened. Any test that relies on shared state or execution order must be refactored before the PR is approved.

### 6.1 Unit Test Independence
<!-- STD-MARKER: testing.6.1 -->

Unit tests must be fully independent. No unit test may rely on state set by another test. No unit test may depend on execution order, the system clock, random values, or external services. Each unit test must set up everything it needs in its own Arrange block.

### 6.2 Workflow Tests
<!-- STD-MARKER: testing.6.2 -->

Workflow tests verify a sequence of steps across multiple components and by their nature carry ordered state within the test. This is intentional and acceptable. A workflow test may depend on the outcome of a prior step within the same test method. Workflow tests must not depend on state from a separate test method or test class.

Workflow tests require abstractions to be in place so that each step can be controlled and substituted. This is one of the primary reasons functional programming structure and small, purposeful interfaces are required by [`GlobalCodingStandards.md`](../standards/GlobalCodingStandards.md#5-functional-programming) — they make workflow steps independently composable and testable.

```csharp
[Fact]
[Trait("Category", "Workflow")]
public void SubmitInvoice_WhenApproved_CompletesFullWorkflow()
{
    // Arrange
    var repository = new FakeInvoiceRepository();
    var approvalService = new FakeApprovalService();
    var workflow = new InvoiceWorkflow(repository, approvalService);

    // Act — steps carry ordered state within this single test
    var invoice = workflow.Create(new InvoiceRequest { Amount = 500 });
    var submitted = workflow.Submit(invoice.Id);
    var approved = workflow.Approve(submitted.Id);

    // Assert
    Assert.Equal(InvoiceStatus.Approved, approved.Status);
}
```

---

## 7. Unit Tests
<!-- STD-MARKER: testing.7 -->

Both human developers and AI models must write unit tests for every new or changed public method or behavior. No behavior may be left untested on the grounds that it is simple or obvious. Unit tests must be written for every changed or new public method or behavior.

### 7.1 Required Coverage Scenarios
<!-- STD-MARKER: testing.7.1 -->

For every public method, the following scenarios must each have a dedicated test. Each scenario is one test method. The method naming convention makes it immediately visible when a scenario is missing.

| Scenario | Description |
|---|---|
| Happy path | Valid input produces the expected result |
| Sad path | Invalid input is handled correctly — rejected, throws, or returns the expected error |
| Boundary conditions | The edges of valid input — null, empty string, empty collection, zero, minimum value, maximum value |
| Edge cases | Technically valid but unusual inputs — all whitespace, exactly one item, a value at the exact maximum allowed length, a date on a boundary such as midnight or leap day |
| Each guard clause | Every precondition check at the top of a method must have a test that triggers it |
| Each distinct outcome | If a method can return different results based on state, each outcome must have its own test |
| Exception scenarios | Where the method is expected to throw, the correct exception type must be verified |

### 7.2 Exception Testing
<!-- STD-MARKER: testing.7.2 -->

When a method is expected to throw an exception, `Assert.Throws<T>` must be used to verify the correct exception type is thrown. The exception type must be as specific as possible — `ArgumentNullException`, `ArgumentOutOfRangeException`, `InvalidOperationException` — not the base `Exception` type.

```csharp
[Fact]
[Trait("Category", "Unit")]
public void GetById_WhenIdIsZero_ThrowsArgumentOutOfRangeException()
{
    // Arrange
    var repository = new FakeClientRepository();
    var service = new ClientService(repository);

    // Act & Assert
    Assert.Throws<ArgumentOutOfRangeException>(() => service.GetById(0));
}
```

### 7.3 Async Tests
<!-- STD-MARKER: testing.7.3 -->

All async methods must be tested with async test methods. xUnit supports `async Task` test methods natively. A test method for an async subject must be declared `async Task` and must `await` the Act. Synchronously blocking on async code with `.Result` or `.Wait()` is not permitted in test methods.

```csharp
[Fact]
[Trait("Category", "Unit")]
public async Task GetByIdAsync_WhenClientExists_ReturnsClient()
{
    // Arrange
    var repository = new FakeClientRepository();
    repository.Add(new Client { Id = 1, Name = "Acme Corp" });
    var service = new ClientService(repository);

    // Act
    var result = await service.GetByIdAsync(1);

    // Assert
    Assert.Equal("Acme Corp", result.Name);
}
```

---

## 8. Workflow Tests — Phase 2
<!-- STD-MARKER: testing.8 -->

Both human developers and AI models must tag workflow tests with the `Workflow` trait and must not run them on PR builds unless a repository-specific enforcement record has activated them.

A workflow test verifies one complete logical business operation through every layer of the application.

Workflow tests are distinct from true end-to-end tests in that they test one logical operation at a time (e.g., Create Invoice), not a chain of multiple business operations. They are distinct from unit tests in that nothing is faked — the full stack is exercised.

### 8.1 Environment Requirements
<!-- STD-MARKER: testing.8.1 -->

Workflow tests require a dedicated environment with real infrastructure. That environment must be provisioned, tested against, and deprovisioned automatically within the pipeline. No persistent manually-managed test environment is permitted. The environment must be defined as infrastructure-as-code so that it is always consistent and always matches the production configuration.

The required pipeline process is:

1. Pipeline provisions a fresh environment from infrastructure-as-code
2. Application is deployed to that environment
3. Workflow tests run against it
4. Environment is deprovisioned regardless of pass or fail
5. Test results gate promotion to the next pipeline stage

Infrastructure-as-code definitions and the workflow test pipeline stage configuration must be defined in the applicable pipeline and rollout artifacts governed by [`GlobalAzureDevOpsPipelineStandards.md`](../standards/GlobalAzureDevOpsPipelineStandards.md#2-pipeline-behavior-by-environment).

### 8.2 Test Data
<!-- STD-MARKER: testing.8.2 -->

Workflow tests must set up all required test data as part of the test and must clean up after themselves. No workflow test may depend on pre-existing data in the environment. Teardown must occur regardless of whether the test passes or fails.

---

## 9. Contract Tests
<!-- STD-MARKER: testing.9 -->

Contract tests verify the observable API contract of an endpoint — the HTTP status codes it returns, the shape of its response body, and how it rejects invalid or unauthorized input. They do not test business logic in isolation the way unit tests do, and they do not exercise real infrastructure the way workflow tests do. They sit at the application boundary and verify that the API behaves as its consumers expect.

Contract tests must be tagged with the `Contract` trait and are Phase 1 — required now.

```csharp
[Fact]
[Trait("Category", "Contract")]
public async Task UploadDocument_WhenUnauthenticated_Returns401()
{
    // Arrange
    var client = _factory.CreateClient(); // no auth token applied

    // Act
    var response = await client.PostAsync("/Documents/v1/upload", new MultipartFormDataContent());

    // Assert
    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
}
```

### Contract Test Scope

Contract tests must cover:

- Correct status code for a valid authenticated request (happy path)
- `401 Unauthorized` when no token is supplied
- `403 Forbidden` when a valid token lacks the required role or client access
- `400 Bad Request` for structurally invalid input
- `404 Not Found` when the requested resource does not exist
- Any other documented contract behavior specific to the endpoint

Contract tests must use `WebApplicationFactory<T>` to host the application in-process. They must not call a deployed environment. Dependencies that require real infrastructure — databases, blob storage, external scanners — must be replaced with fakes or stubs registered via `WebApplicationFactory` configuration so the test is fully self-contained.

```csharp
// Required — replace real infrastructure dependencies for contract tests
internal class ContractTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace real repository with a fake for contract tests
            services.RemoveAll<IClientRepository>();
            services.AddSingleton<IClientRepository, FakeClientRepository>();
        });
    }
}
```

Contract test files must be placed in a `Contracts/` folder within the test project, mirroring the controller structure of the production project.

---

## 10. Regression Tests
<!-- STD-MARKER: testing.10 -->

Regression tests verify that a previously identified defect remains fixed.

### 10.1 Regression Test Requirement
<!-- STD-MARKER: testing.10.1 -->

When a defect reaches the point of code correction, a regression test must be added if the defect can be reproduced in an automated test.

The regression test must:

- reproduce the defect scenario
- verify the corrected behavior
- be tagged with `Regression`
- be placed with the test type that best matches the corrected behavior boundary

### 10.2 Regression Test Placement
<!-- STD-MARKER: testing.10.2 -->

Regression is a cross-cutting category, not a separate test style.

- A regression test for a single class or method must also be a `Unit` test.
- A regression test for an API contract defect must also be a `Contract` test.
- A regression test for an end-to-end business operation must also be a `Workflow` test when the repository has activated Phase 2 workflow enforcement.

The `Regression` trait must be added in addition to the primary test category trait.

### 10.3 Regression Test Scope
<!-- STD-MARKER: testing.10.3 -->

Regression tests must verify the exact defect boundary that failed previously. A broad happy-path test that does not isolate the prior failure condition is not sufficient as the only regression guard.

---

## 11. Performance Tests
<!-- STD-MARKER: testing.11 -->

Both human developers and AI models must tag performance tests with the `Performance` trait. Performance tests must not run on PR builds unless a repository-specific enforcement record has activated them.

Performance tests verify that critical operations meet defined response time and memory thresholds.

The following operations must have performance tests:

- High-frequency service calls — any operation called on every request or at high volume
- Data transformation operations — any extension method or mapping operation that processes large collections
- Repository operations — any stored procedure call that operates on large data sets

Performance tests must be written using BenchmarkDotNet. A raw `Stopwatch` in a single-run test is not acceptable — it is affected by JIT warm-up, GC pressure, and OS scheduling and produces unreliable results. BenchmarkDotNet handles all of that automatically through multiple iterations and warm-up runs.

Performance tests must be tagged with the `Performance` trait and must run as part of the Phase 2 pipeline stage. They must not run on every PR build as they are too slow for that feedback loop.

```csharp
// Required — BenchmarkDotNet benchmark; thresholds from GlobalPerformanceStandards.md Section 4
[MemoryDiagnoser]
public class GetAllClientsBenchmark
{
    private ClientService _service = null!;

    [GlobalSetup]
    public void Setup()
    {
        var repository = new FakeClientRepository();
        // seed representative production-scale data
        for (var i = 0; i < 1000; i++)
            repository.Add(new Client { Id = i, Name = $"Client {i}" });
        _service = new ClientService(repository);
    }

    [Benchmark]
    public async Task GetAllAsync() => await _service.GetAllAsync();
}
```

Thresholds used in benchmark assertions must match the values defined in [`GlobalPerformanceStandards.md`](../standards/GlobalPerformanceStandards.md#4-thresholds-reference). A benchmark that defines its own threshold without referencing that table is non-compliant.

---

## 12. Test Doubles — Fakes, Stubs, and Mocks
<!-- STD-MARKER: testing.12 -->

Both human developers and AI models must use fakes by default and must use mocks only when the test specifically needs to verify an interaction. A test that uses a mock when a fake would suffice must be refactored to use a fake before the PR is approved.

A test double is any object that stands in for a real dependency during a test.

| Type | Definition | Used For |
|---|---|---|
| **Stub** | A controllable replacement that returns predetermined data | Supplying predictable inputs to the subject under test |
| **Mock** | A fake that is asserted against — verifies that a call was made | Verifying interactions and behavior |
| **Fake** | A working alternative implementation used as either a stub or a mock depending on context | General-purpose test double |

### 12.1 Fake Implementation
<!-- STD-MARKER: testing.12.1 -->

A fake implements only the interface members the tests require. It does not replicate the full functionality of the real implementation. A `FakeClientRepository` does not contain ADO.NET, stored procedure calls, or connection management — it stores and retrieves items from an in-memory `List<T>`. That is its entire implementation.

```csharp
// The fake — implements the interface with just enough behavior for tests
public class FakeClientRepository : IClientRepository
{
    private readonly List<Client> _clients = new();

    public IEnumerable<Client> GetAll() => _clients;

    public Client GetById(int id)
        => _clients.FirstOrDefault(c => c.Id == id);

    public void Add(Client client) => _clients.Add(client);
}
```

### 12.2 Test Data Belongs in the Test
<!-- STD-MARKER: testing.12.2 -->

The fake object is the reusable piece — it lives in the `Fakes/` folder and is shared across the test project. Test data is test-specific — it is created in the Arrange block of each individual test and describes exactly what that test needs. No fake may have data baked into it.

```csharp
[Fact]
[Trait("Category", "Unit")]
public void GetById_WhenClientExists_ReturnsClient()
{
    // Arrange — test owns and supplies its own data
    var repository = new FakeClientRepository();
    repository.Add(new Client { Id = 1, Name = "Acme Corp" });
    var service = new ClientService(repository);

    // Act
    var result = service.GetById(1);

    // Assert
    Assert.Equal("Acme Corp", result.Name);
}
```

### 12.3 When to Use a Mock
<!-- STD-MARKER: testing.12.3 -->

A mock is appropriate when the test must verify that a specific interaction occurred — for example, that a method was called a specific number of times, or that it was called with specific arguments. NSubstitute is the approved mocking library. A mock must not be used to test internal implementation details. If a test breaks because internal code was restructured without changing observable behavior, the test was asserting the wrong thing.

```csharp
[Fact]
[Trait("Category", "Unit")]
public void Add_WhenClientIsValid_CallsRepositoryOnce()
{
    // Arrange
    var mockRepository = Substitute.For<IClientRepository>();
    var service = new ClientService(mockRepository);
    var client = new Client { Id = 1, Name = "Acme Corp" };

    // Act
    service.Add(client);

    // Assert — verifying the interaction, not the implementation
    mockRepository.Received(1).Add(client);
}
```

### 12.4 Fakes Are Preferred Over Mocks
<!-- STD-MARKER: testing.12.4 -->

Fakes must be used by default. Mocks must only be used when the test specifically needs to verify an interaction. A test that uses a mock when a fake would suffice must be refactored to use a fake before the PR is approved.

---

## 13. CI Gate
<!-- STD-MARKER: testing.13 -->

Both human developers and AI models must verify that all unit tests pass before marking a work slice complete or opening a PR. A PR with failing or suppressed tests must not be approved.

All unit tests must pass in CI before any PR may be merged.

Workflow, regression, and performance tests must run only in repository-specific activated stages outside the PR build when the repository has the required environment and an explicit local enforcement record.

---

## 14. Compliance Verification
<!-- STD-MARKER: testing.14 -->

**Phase 1 — enforced now:**
- [ ] xUnit is used as the test framework.
- [ ] All test code is in a `[ProjectName].Tests` project separate from the production project.
- [ ] Test files mirror the production project folder structure.
- [ ] All fake implementations are in the `Fakes/` folder.
- [ ] Contract test files are in a `Contracts/` folder mirroring the controller structure.
- [ ] Test classes and files are named `[SubjectName]Tests`.
- [ ] Test methods follow the `[MethodUnderTest]_[Scenario]_[ExpectedResult]` naming pattern.
- [ ] Fake implementations are named `Fake[InterfaceName]`.
- [ ] Observability-only test classes are named `[TypeName]ObservabilityTests` and their methods follow `[MethodOrOperation]When[Dependency]ThrowsThenRethrowsAndLogsError`.
- [ ] Guard-clause test methods follow `[Service][Method]When[Condition]Then[ExpectedGuardException]`.
- [ ] Every test carries at least one `Category` trait using the defined values.
- [ ] Every test method follows Arrange-Act-Assert with each block appearing exactly once.
- [ ] AAA blocks are separated by blank lines with comments marking each block.
- [ ] Unit tests are fully independent — no shared state, no ordering dependency.
- [ ] Unit tests cover all new and changed public behavior.
- [ ] Async subjects are tested with `async Task` test methods — `.Result` and `.Wait()` are not used in test code.
- [ ] Fakes are used by default; mocks are used only to verify interactions.
- [ ] No fake has test data baked into it — all test data is in the Arrange block.
- [ ] Contract tests exist for every API endpoint covering the required status code scenarios.
- [ ] Contract tests use `WebApplicationFactory<T>` and replace real infrastructure with fakes — no deployed environment is called.
- [ ] Defects that can be reproduced through automation have a regression test that verifies the corrected behavior.
- [ ] All unit tests pass in CI before the PR is opened.

**Phase 2 — active only when the repository has the required environment and an explicit local enforcement record:**
- [ ] Workflow tests exist for all key business operations.
- [ ] Workflow test environment is provisioned and deprovisioned automatically within the pipeline.
- [ ] Workflow test environment is defined as infrastructure-as-code.
- [ ] Workflow tests set up and clean up all test data within the test.
- [ ] Workflow, regression, and performance tests gate promotion in the repository-specific activated pipeline stages.

---

## 15. Governance
<!-- STD-MARKER: testing.15 -->

This standard is owned by Troy Crowe. No changes to this file may be merged without Troy Crowe's explicit approval. Changes must be submitted as a pull request that includes a rationale comment explaining the reason for the update or deviation. Direct commits to `dev` or `main` are not permitted. Branch, PR, and approval rules are defined in [`GlobalGovernanceStandards.md`](../standards/GlobalGovernanceStandards.md).
