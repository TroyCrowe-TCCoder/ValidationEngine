# Standards References

This file is a catalog of external references, research posts, and decision rationale that informed specific rules across the standards documents. It is not a standard itself. Each entry links back to the standard and section it supports. Each standard links forward to the entry it references.

---

## Catalog

- [REF-001 — `using` Statement Placement: Inside vs. Outside Namespace](#ref-001)
- [REF-002 — SOLID Principles: Original Source Reference (Uncle Bob)](#ref-002)
- [REF-003 — Functional Programming in C# (Microsoft Learn)](#ref-003)
- [REF-004 — Unit Testing Best Practices: Fakes, Stubs, and Mocks (Microsoft Learn)](#ref-004)

---

<a id="ref-001"></a>
## REF-001 — `using` Statement Placement: Inside vs. Outside Namespace

**Referenced by:** [`Docs/Standards/GlobalSolutionStructureStandards.md` — Section 3.4 Namespace Conventions](../standards/GlobalSolutionStructureStandards.md#34-namespace-conventions)
**Decision:** `using` statements must be placed inside the namespace block.
**Status:** Confirmed. No exception required for controllers.

### Reference Post

> Placing using statements inside the namespace does not cause functional issues in C# controllers and is actually a recommended practice by some code analysis tools like StyleCop. While it changes the scope and resolution order, it won't break controller logic in standard ASP.NET Core implementations.
>
> **Key Differences and Impacts**
>
> The choice between placing using statements inside or outside the namespace typically comes down to scope and name resolution:
>
> - **Scope Limitation:** `using` statements inside a namespace only apply to that specific block. If you have multiple namespaces in one file (which is rare in controllers), the `using` directives inside one won't "pollute" the others.
> - **Resolution Order:** When `using` is inside the namespace, the compiler searches the current namespace first, then the imported ones. If it's outside, it searches global namespaces first. This can occasionally help avoid rare naming collisions between your local classes and external libraries.
> - **Alias Behavior:** Using aliases (e.g., `using MyAlias = System.Text;`) inside a namespace requires fully qualified names, as the alias doesn't benefit from other `using` statements at the same level.
> - **Standard Practice:** Visual Studio templates place `using` statements outside the namespace by default. However, with the introduction of file-scoped namespaces in C# 10, the distinction has become less prominent, as the namespace declaration typically appears at the very top of the file.

### Decision Rationale

The rule to place `using` statements inside the namespace was initially questioned due to past controller issues. Research confirmed those issues were unrelated to `using` placement. The rule stands as written with no controller exception. The remediation steps in Section 12 of `GlobalSolutionStructureStandards.md` explicitly include controllers to ensure all existing code is corrected.

---

<a id="ref-002"></a>
## REF-002 — SOLID Principles: Original Source Reference (Uncle Bob)

**Referenced by:** [`Docs/Standards/GlobalCodingStandards.md` — Section 4 SOLID Principles](../standards/GlobalCodingStandards.md#4-solid-principles)
**Decision:** The SOLID principles as defined and described by Robert C. Martin (Uncle Bob) are the authoritative source for all SOLID-related rules in this standards library.
**Status:** Confirmed.

### Reference

Robert C. Martin — *SOLID Relevance* (2020)
[https://blog.cleancoder.com/uncle-bob/2020/10/18/Solid-Relevance.html](https://blog.cleancoder.com/uncle-bob/2020/10/18/Solid-Relevance.html)

> The SOLID principles are not dead. They are not irrelevant. They are not too hard. They remain as relevant today as they were when I wrote about them in the early 2000s.

### Decision Rationale

Uncle Bob introduced and defined the SOLID principles. His 2020 post reaffirms their relevance across modern software paradigms including functional programming and microservices. All SOLID rules in this standards library are grounded in his original definitions.

---

<a id="ref-003"></a>
## REF-003 — Functional Programming in C# (Microsoft Learn)

**Referenced by:** [`Docs/Standards/GlobalCodingStandards.md` — Section 5 Functional Programming](../standards/GlobalCodingStandards.md#5-functional-programming)
**Decision:** Microsoft Learn is the authoritative reference for functional programming patterns in C#.
**Status:** Confirmed.

### Reference

Microsoft Learn — *Functional Programming in C#*
[https://learn.microsoft.com/en-us/dotnet/csharp/functional/](https://learn.microsoft.com/en-us/dotnet/csharp/functional/)

### Decision Rationale

Functional programming patterns in C# are an approved complement to SOLID principles where they improve readability and maintainability. Microsoft Learn is the canonical reference for how these patterns apply in C#. Additional patterns and examples beyond what is documented in `GlobalCodingStandards.md` should be sourced from this reference.

---

<a id="ref-004"></a>
## REF-004 — Unit Testing Best Practices: Fakes, Stubs, and Mocks (Microsoft Learn)

**Referenced by:** [`Docs/Standards/GlobalTestingStandards.md` — Section 10 Test Doubles](../standards/GlobalTestingStandards.md#12-test-doubles-fakes-stubs-and-mocks)
**Decision:** Microsoft Learn is the authoritative reference for the definitions and correct usage of fakes, stubs, and mocks in .NET unit testing.
**Status:** Confirmed.

### Reference

Microsoft Learn — *Unit testing best practices with .NET*
[https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-best-practices)

### Decision Rationale

The terms fake, stub, and mock are frequently misused and inconsistently defined across testing literature and tooling. The Microsoft .NET testing best practices page provides clear, authoritative definitions in the context of .NET development. The key distinction is that a stub provides data, a mock verifies interactions, and a fake is a working alternative implementation that can serve as either. These definitions are the basis for the test double rules in `GlobalTestingStandards.md`.
