# Coverage Matrix — GlobalTypeScriptStandards.md

**Status:** Paused — technology not yet in use, see backlog
**Sign Off:**
**Sign Off Date:**
**Revision Date:**
**Columns**

| Column | Meaning |
|---|---|
| STD-MARKER | Rule id from the standards file. |
| Rule Summary | One-line paraphrase of the rule. |
| Category | Technology / Development / Naming. |
| Confidence | Binary (deterministic pass/fail) / Heuristic (risk-flag, needs human confirmation) / Manual-only (no automation path). |
| Severity | Hard-stop / Warning / Manual-only-comment. |
| Frequency | Every-Commit (change-set scoped, `.ts`/`.tsx` file changes). |
| Technique | Short detection approach. |
| Status | Missing / Existing / Acknowledged-future-work. |

**File status note:** This standard's header declares `**Status:** Draft`. Per `GlobalFileSpecificationStandards.md` file-specification.2.8, a Draft standard "must not be enforced until promoted to Active." **All rows in this matrix are therefore classified `Acknowledged-future-work`**, consistent with the CaptiveExpensesApi repository's exclusion of `typescript.file` ("This solution contains no TypeScript code"). Unlike `GlobalReactProjectStandards.md`, this file has no explicit "Activation Notes" section describing a promotion trigger — it applies to any TypeScript in the GlobalStandards governance model (React front-ends, Node.js utilities, build tooling), not solely the pending React rebuild.

---

### Section 2 — Compiler Configuration

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| typescript.2.1 | Baseline `tsconfig.json`: `target: ES2022`, `module: ESNext`, `moduleResolution: bundler`, `strict: true` (non-negotiable), `noUncheckedIndexedAccess: true`, `noImplicitReturns: true`, `noFallthroughCasesInSwitch: true`, `exactOptionalPropertyTypes: true`, `forceConsistentCasingInFileNames: true`, `skipLibCheck: false` (must not be set to `true` to suppress dependency type errors), `esModuleInterop: true` | Technology | Binary | Hard-stop | Every-Commit (on `tsconfig.json` changes) | Config-scan: parse `tsconfig.json` `compilerOptions` and verify each listed flag matches the required value exactly | Acknowledged-future-work |
| typescript.2.2 | `any` prohibited except for generated code, documented third-party interop with no type definitions (comment required explaining why), or test-stub partial mocks where `unknown` is insufficient; `unknown` used instead when type is genuinely unknown, narrowed with type guards before use | Development | Heuristic | Hard-stop | Every-Commit | ESLint rule `@typescript-eslint/no-explicit-any` set to error; flag `any` usage not accompanied by an adjacent justification comment or not within an allowlisted generated-code path; verifying "narrowed with type guards before use" for `unknown` requires flow analysis — a Heuristic layered on top of the Binary ESLint rule | Acknowledged-future-work |

---

### Section 3 — Type System Usage

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| typescript.3.1 | `interface` for object shapes that may be implemented/extended; `type` for unions, intersections, mapped types | Development | Heuristic | Warning | Every-Commit | ESLint rule `@typescript-eslint/consistent-type-definitions` set to `interface`; the distinction between "shape that may be extended" (interface) vs. genuinely needing `type` (union/intersection) is partially enforceable by the ESLint rule but the semantic judgment for edge cases keeps this Heuristic | Acknowledged-future-work |
| typescript.3.2 | Discriminated unions used for mutually-exclusive-shape states; optional fields must not substitute for a discriminated union when field presence depends on another field's value | Development | Manual-only | Manual-only-comment | Periodic (code-review) | Detecting "should have been a discriminated union but used optional fields instead" requires semantic understanding of the domain model's state machine — not mechanically detectable from syntax alone | Acknowledged-future-work |
| typescript.3.3 | Data that must not be mutated after construction marked `readonly`; `Readonly<T>`, `ReadonlyArray<T>`, `ReadonlyMap<K,V>` used for collections/objects passed through function boundaries where mutation would be a bug | Development | Heuristic | Warning | Every-Commit | ESLint rule `functional/prefer-readonly-type` or `@typescript-eslint` immutability rules can flag missing `readonly` on interface properties/parameters that are never reassigned; "would be a bug if mutated" requires judging intent, keeping the overall determination Heuristic | Acknowledged-future-work |
| typescript.3.4 | Type assertions (`as SomeType`) avoided; used only when justified with information the compiler cannot infer; type guards (`value is T` predicate functions) preferred for narrowing | Development | Heuristic | Warning | Every-Commit | ESLint rule `@typescript-eslint/no-unnecessary-type-assertion` catches redundant assertions (Binary); flag `as` usage not immediately preceded by a type-guard-based narrowing check as a candidate for review — justification adequacy is a Heuristic/manual judgment | Acknowledged-future-work |

---

### Section 4 — Functional Patterns

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| typescript.4.1 | Business-logic functions must be pure (no side effects) unless the function's explicit purpose is a side effect (logging, network call, DOM mutation); given the same inputs, return the same output with no external state changes | Development | Manual-only | Manual-only-comment | Periodic (code-review) | Purity is a whole-program semantic property (no external state mutation, no I/O) that static analysis can only partially approximate (e.g., ESLint `functional/no-expression-statements` plugins flag some side-effect patterns) — the "is this function's purpose explicitly a side effect" classification requires human judgment, keeping detection Manual-only overall | Acknowledged-future-work |
| typescript.4.2 | Function arguments (objects/arrays) not mutated; new values returned instead | Development | Heuristic | Hard-stop | Every-Commit | ESLint rule `no-param-reassign` (with `props: true`) flags direct parameter mutation and property mutation on parameters; array-mutating-method calls on parameters (`.push`, `.splice`, `.sort` without a copy) can be flagged via a custom rule or `functional/immutable-data` — reasonably reliable Heuristic, some false positives on intentionally local copies possible | Acknowledged-future-work |
| typescript.4.3 | `map`/`filter`/`reduce`/`flatMap` preferred over imperative loops for data transformation; imperative loops acceptable when the transformation is complex enough that functional form is less readable | Development | Manual-only | Manual-only-comment | Periodic (code-review) | "Complex enough that imperative form is more readable" is an explicit readability judgment call the rule itself defers to humans; a Heuristic linter could flag `for`/`while` loops that build up an array/object as a candidate for functional refactor, but the exception clause makes any hard-stop enforcement inappropriate | Acknowledged-future-work |
| typescript.4.4 | Optional chaining (`?.`) and nullish coalescing (`??`) preferred over explicit null checks where they improve readability; `\|\|` must not be used as null-coalescing when the value may be a valid falsy value (`0`, `''`, `false`) — use `??` instead | Development | Binary | Hard-stop (for the `\|\|` vs `??` sub-rule) | Every-Commit | ESLint rule `@typescript-eslint/prefer-nullish-coalescing` reliably flags `\|\|` usage that should be `??` based on the inferred type of the left-hand operand (catches the falsy-value-safety issue precisely); the broader "improves readability" guidance for `?.` adoption itself is a softer, non-hard-stop preference | Acknowledged-future-work |

---

### Section 5 — Async Code

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| typescript.5.1 | `async`/`await` used for all async operations; raw `.then().catch()` chains prohibited except when `Promise.all`/`Promise.allSettled`/`Promise.race` requires chaining at the call site | Development | Heuristic | Hard-stop | Every-Commit | ESLint rule `@typescript-eslint/promise-function-async` plus a custom/regex scan flagging `.then(` / `.catch(` call chains not immediately wrapping a `Promise.all`/`allSettled`/`race` call | Acknowledged-future-work |
| typescript.5.2 | Errors always handled in async functions; unhandled `Promise` rejections must not occur (silently swallowed in some browser contexts, crashes the process in Node.js) | Development | Heuristic | Hard-stop | Every-Commit | ESLint rule `@typescript-eslint/no-floating-promises` set to error catches the most common unhandled-rejection pattern (a Promise-returning call with no `await`/`.catch`/return); confirming every async function's `try/catch` block meaningfully handles (not just re-throws blindly without logging) is a softer Heuristic layer | Acknowledged-future-work |
| typescript.5.3 | `AbortController`/`AbortSignal` used for cancellable async operations, matching the `CancellationToken` pattern in C#; `AbortSignal` propagated through all layers of async call chains | Development | Heuristic | Warning | Every-Commit | AST scan: for exported async functions performing `fetch`/long-running work, flag absence of an `AbortSignal`-typed parameter; verifying full propagation through multi-layer call chains requires call-graph analysis — a more involved Heuristic check, directly parallel to the C#/.NET `CancellationToken` propagation rule already captured in `GlobalCodingStandards.md`/`GlobalPerformanceStandards.md` | Acknowledged-future-work |

---

### Section 6 — Naming Conventions

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| typescript.6 | Naming table: variables/functions `camelCase`; classes/interfaces/type aliases/enum members `PascalCase`; module-level constants `SCREAMING_SNAKE_CASE`, local constants `camelCase`; files `kebab-case`; test files `{name}.test.ts` | Naming | Binary | Hard-stop | Every-Commit | ESLint rule `@typescript-eslint/naming-convention` configured per the table (covers variables, functions, classes, interfaces, type aliases, enum members programmatically); file-naming casing (`kebab-case`) and test-file suffix pattern verified via a regex file-path scan since ESLint naming-convention does not cover filenames | Acknowledged-future-work |

---

### Section 7 — Module Imports and Exports

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| typescript.7 | Named exports used; default exports avoided except for React components (per React convention); imports grouped external-then-internal with a blank-line separator; barrel `index.ts` re-export-everything files avoided unless the folder is a genuine public-API boundary (deep barrel re-exports create circular-dependency risk and slow builds) | Development | Heuristic | Warning | Every-Commit | ESLint rules `import/no-default-export` (scoped to exclude component files, cross-referencing `GlobalReactProjectStandards.md` react-project.4.4's default-export-for-components carve-out) and `import/order` (with a groups configuration for external-vs-internal + newlines-between); "public API boundary" judgment for barrel-file exceptions requires human review, keeping that clause Heuristic | Acknowledged-future-work |

---

### Section 8 — Linting and Formatting

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| typescript.8 | ESLint with `@typescript-eslint` plugin extending recommended TypeScript rules; Prettier with a committed `.prettierrc` (no reliance on editor-specific settings); CI pipeline runs `eslint` and `prettier --check`, failing the build on any error; inline ESLint disables (`// eslint-disable-next-line`) not permitted unless the rule is genuinely incorrect for the case, accompanied by an explanatory comment | Technology | Binary | Hard-stop | Every-Commit | Config-scan: verify `.eslintrc`/`eslint.config.js` extends `@typescript-eslint/recommended` (or stricter) and `.prettierrc` exists at repo root; pipeline-YAML scan: verify `eslint` and `prettier --check` steps exist and are configured to fail the build on non-zero exit; regex scan: flag `eslint-disable` comments with no adjacent justification comment — this rule pair closely parallels `GlobalCodingStandards.md`'s C#/Roslyn-analyzer suppression-comment requirement | Acknowledged-future-work |

---

### Section 9 — Compliance Verification (Excluded as an independent detection target)

The Section 9 checklist restates Sections 2–8 as a pre-commit checklist (note: as with `GlobalReactProjectStandards.md`, these checklist bullets lack individual rule-item `STD-MARKER` comments — a `file-specification.2.10`/`2.14` compliance gap in this Draft file worth flagging to the standards owner before promotion to Active). No independent detection techniques are introduced beyond what is captured above.

### Section 10 — Governance (Excluded)

`typescript.10` (ownership/approval boilerplate) is excluded — not code-checkable, procedural ownership statement only.

---

**Notes:**
1. **This entire file is Acknowledged-future-work** pending promotion from `Draft` to `Active` status per `GlobalFileSpecificationStandards.md` file-specification.2.8, and the CaptiveExpensesApi repository's `typescript.file` exclusion remains appropriate until TypeScript code is introduced (most likely alongside the React rebuild covered by `GlobalReactProjectStandards.md`).
2. Section 9's compliance checklist lacks individual rule-item markers, the same structural gap identified in the `GlobalReactProjectStandards.md` matrix — both Draft files share this omission, suggesting it may be a pattern from when these two files were authored together and worth a single combined fix pass when both are promoted to Active.
3. Detection here is dominated by **ESLint rule configuration** (`@typescript-eslint/*`, `import/*`, `functional/*` plugin rules) rather than custom AST tooling — a notably higher proportion of Binary/near-Binary rules than `GlobalReactProjectStandards.md` achieved, because TypeScript/JS tooling has a mature, well-established linting ecosystem that directly maps to many of these rules (naming conventions, `no-explicit-any`, `no-floating-promises`, `prefer-nullish-coalescing`, `consistent-type-definitions`). A single well-configured `.eslintrc` extending `@typescript-eslint/recommended-type-checked` plus a handful of targeted custom rules would cover the majority of this file's Hard-stop/Warning rows more cheaply than any bespoke tooling investment.
4. typescript.5.3 (`AbortSignal`/`AbortController` cancellation propagation) is the direct TypeScript-ecosystem analog of the C#/.NET `CancellationToken` propagation rule already captured in the `GlobalCodingStandards.md` and `GlobalPerformanceStandards.md` matrices — the standard itself explicitly draws this parallel ("matching the pattern that `CancellationToken` provides in C#"). Worth noting as a cross-language rule-pair when reviewing detection-tooling investment priorities, since the underlying architectural principle (cancellation propagation through async call chains) is identical across both ecosystems even though the concrete mechanism differs.
5. typescript.7's default-vs-named-export carve-out for React components directly depends on `GlobalReactProjectStandards.md` react-project.4.4 being in effect — these two Draft standards are mutually referential and should be reviewed/promoted together rather than independently, since a TypeScript-only (non-React) repository would need a stricter no-default-exports rule with no carve-out.

