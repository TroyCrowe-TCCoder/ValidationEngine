# TypeScript Standards

**Version:** 1.0.0
**Status:** Draft
**Applies To:** All repositories under the GlobalStandards governance model that contain TypeScript code
**Audience:** AI models and human developers
**Created:** 2026-07-12
**Last Modified:** 2026-07-12

---
<!-- STD-MARKER: typescript.file -->


## 1. Purpose
<!-- STD-MARKER: typescript.1 -->

This document defines the TypeScript-specific rules that supplement the general patterns in `GlobalCodingStandards.md`. Rules in `GlobalCodingStandards.md` take precedence. This file defines TypeScript-specific additions, overrides, and clarifications that are not covered by the general standard.

This document applies to all TypeScript written for any purpose in repositories under the GlobalStandards governance model, including React front-end code, Node.js utilities, and build tooling. React-specific project and component rules are in `GlobalReactProjectStandards.md`.

---

## 2. Compiler Configuration
<!-- STD-MARKER: typescript.2 -->

### 2.1 — tsconfig.json Baseline
<!-- STD-MARKER: typescript.2.1 -->

Every TypeScript project must use the following baseline `tsconfig.json` settings:

```json
{
  "compilerOptions": {
    "target": "ES2022",
    "module": "ESNext",
    "moduleResolution": "bundler",
    "strict": true,
    "noUncheckedIndexedAccess": true,
    "noImplicitReturns": true,
    "noFallthroughCasesInSwitch": true,
    "exactOptionalPropertyTypes": true,
    "forceConsistentCasingInFileNames": true,
    "skipLibCheck": false,
    "esModuleInterop": true
  }
}
```

- `strict: true` is non-negotiable. It enables `strictNullChecks`, `strictFunctionTypes`, `noImplicitAny`, and other checks that prevent entire categories of runtime errors.
- `noUncheckedIndexedAccess: true` forces explicit null checks when accessing array indices or object properties by key, preventing silent `undefined` bugs.
- Do not add `"skipLibCheck": true` to suppress type errors from dependencies. Fix the underlying issue or add a proper type override.

### 2.2 — No `any`
<!-- STD-MARKER: typescript.2.2 -->

The `any` type disables TypeScript's type system for the annotated value and everything it touches. It is prohibited except in the following narrow cases:

- Generated code that cannot be modified.
- Third-party library interop where no type definitions exist and creating them is not feasible. In this case, add a comment explaining why `any` was unavoidable.
- Test stubs where a partial mock is required and `unknown` cast is insufficient.

Use `unknown` instead of `any` when the type is genuinely not known at compile time. Narrow `unknown` with type guards before use.

---

## 3. Type System Usage
<!-- STD-MARKER: typescript.3 -->

### 3.1 — Prefer Interfaces for Object Shapes
<!-- STD-MARKER: typescript.3.1 -->

Use `interface` for object shape declarations that may be implemented by a class or extended:

```typescript
// CORRECT
interface DocumentMetadata {
  id: string;
  clientId: number;
  fileName: string;
  uploadedOn: Date;
}

// Use type for union types, intersections, or mapped types
type DocumentStatus = 'Pending' | 'Scanned' | 'Approved' | 'Rejected';
```

### 3.2 — Discriminated Unions
<!-- STD-MARKER: typescript.3.2 -->

Use discriminated unions for modeling states that have mutually exclusive shapes:

```typescript
type ApiResult<T> =
  | { success: true; data: T }
  | { success: false; error: string };
```

Do not use optional fields as a substitute for discriminated unions when the presence of a field depends on another field's value.

### 3.3 — Readonly
<!-- STD-MARKER: typescript.3.3 -->

Mark data that must not be mutated after construction as `readonly`:

```typescript
interface AppConfig {
  readonly apiBaseUrl: string;
  readonly clientId: string;
}
```

Use `Readonly<T>`, `ReadonlyArray<T>`, and `ReadonlyMap<K, V>` for collections and objects passed through function boundaries where mutation would be a bug.

### 3.4 — Avoid Type Assertions
<!-- STD-MARKER: typescript.3.4 -->

Type assertions (`as SomeType`) bypass the compiler and can mask real type errors. Use them only when you have information that the compiler cannot infer and you can justify the safety of the assertion.

```typescript
// CORRECT — narrowing with a type guard
function isDocumentMetadata(value: unknown): value is DocumentMetadata {
  return (
    typeof value === 'object' &&
    value !== null &&
    'id' in value &&
    'clientId' in value
  );
}

// AVOID — blind assertion
const doc = response.data as DocumentMetadata;
```

---

## 4. Functional Patterns
<!-- STD-MARKER: typescript.4 -->

### 4.1 — Pure Functions
<!-- STD-MARKER: typescript.4.1 -->

Functions must not produce side effects unless their purpose is explicitly to cause a side effect (e.g., a logging function, a network call, a DOM mutation). Business logic functions must be pure: given the same inputs, they return the same output with no external state changes.

### 4.2 — Immutable Data
<!-- STD-MARKER: typescript.4.2 -->

Do not mutate objects or arrays passed as function arguments. Return new values instead:

```typescript
// CORRECT
function addTag(tags: readonly string[], newTag: string): string[] {
  return [...tags, newTag];
}

// WRONG — mutates the caller's array
function addTag(tags: string[], newTag: string): void {
  tags.push(newTag);
}
```

### 4.3 — Array Transformations
<!-- STD-MARKER: typescript.4.3 -->

Use `map`, `filter`, `reduce`, and `flatMap` over imperative loops for data transformation. Imperative loops are acceptable when the transformation logic is complex enough that the functional form is less readable.

### 4.4 — Optional Chaining and Nullish Coalescing
<!-- STD-MARKER: typescript.4.4 -->

Use optional chaining (`?.`) and nullish coalescing (`??`) instead of explicit null checks where they improve readability without obscuring intent:

```typescript
// CORRECT
const name = user?.profile?.displayName ?? 'Unknown';

// Acceptable when the check is complex
const name = user !== null && user.profile !== null
  ? user.profile.displayName
  : 'Unknown';
```

Do not use `||` as a null-coalescing operator when the value may be a valid falsy value (`0`, `''`, `false`). Use `??` instead.

---

## 5. Async Code
<!-- STD-MARKER: typescript.5 -->

### 5.1 — Async/Await
<!-- STD-MARKER: typescript.5.1 -->

Use `async`/`await` for all asynchronous operations. Do not use raw `.then().catch()` chains except in the rare case where `Promise.all`, `Promise.allSettled`, or `Promise.race` requires chaining at the call site.

```typescript
// CORRECT
async function fetchDocument(id: string): Promise<DocumentMetadata> {
  const response = await apiClient.get<DocumentMetadata>(`/documents/${id}`);
  return response.data;
}

// WRONG — raw promise chains
function fetchDocument(id: string): Promise<DocumentMetadata> {
  return apiClient.get<DocumentMetadata>(`/documents/${id}`)
    .then(response => response.data);
}
```

### 5.2 — Error Handling in Async Functions
<!-- STD-MARKER: typescript.5.2 -->

Always handle errors in async functions. An unhandled `Promise` rejection in browser code is swallowed silently in some environments. An unhandled rejection in Node.js will crash the process.

```typescript
async function uploadDocument(file: File): Promise<void> {
  try {
    await documentService.upload(file);
  } catch (error) {
    logger.error('Document upload failed', { error });
    throw error; // re-throw so the caller can decide how to surface the failure
  }
}
```

### 5.3 — Cancellation
<!-- STD-MARKER: typescript.5.3 -->

Use `AbortController` / `AbortSignal` for cancellable async operations in TypeScript, matching the pattern that `CancellationToken` provides in C#:

```typescript
async function fetchWithCancellation(url: string, signal: AbortSignal): Promise<Response> {
  return fetch(url, { signal });
}
```

Pass `AbortSignal` through all layers of async call chains just as `CancellationToken` is propagated in C#.

---

## 6. Naming Conventions
<!-- STD-MARKER: typescript.6 -->

| Construct | Convention | Example |
|---|---|---|
| Variables | `camelCase` | `documentId`, `isLoading` |
| Functions | `camelCase` | `fetchDocument`, `parseResponse` |
| Classes | `PascalCase` | `DocumentService`, `ApiClient` |
| Interfaces | `PascalCase` | `DocumentMetadata`, `ApiConfig` |
| Type aliases | `PascalCase` | `DocumentStatus`, `ApiResult` |
| Enum members | `PascalCase` | `DocumentStatus.Pending` |
| Constants | `SCREAMING_SNAKE_CASE` for module-level constants; `camelCase` for local constants | `API_BASE_URL`, `defaultTimeout` |
| Files | `kebab-case` | `document-service.ts`, `use-documents.ts` |
| Test files | `{name}.test.ts` | `document-service.test.ts` |

---

## 7. Module Imports and Exports
<!-- STD-MARKER: typescript.7 -->

- Use named exports. Avoid default exports except for React components, which follow the React convention.
- Group imports: external packages first, then internal modules, separated by a blank line.
- Do not use barrel `index.ts` files that re-export everything from a folder unless the folder represents a public API boundary. Deep barrel re-exports create circular dependency risks and slow build tooling.

```typescript
// CORRECT — named export
export function parseDocumentMetadata(raw: unknown): DocumentMetadata { ... }

// WRONG — default export for a utility function
export default function parseDocumentMetadata(raw: unknown): DocumentMetadata { ... }
```

---

## 8. Linting and Formatting
<!-- STD-MARKER: typescript.8 -->

- **Linter:** ESLint with `@typescript-eslint` plugin. The `.eslintrc` configuration must extend the recommended TypeScript rules.
- **Formatter:** Prettier. The `.prettierrc` configuration must be committed to the repository. Do not rely on editor-specific settings for formatting.
- The CI validation pipeline must run `eslint` and `prettier --check` and fail the build on any error.
- Do not disable ESLint rules inline (`// eslint-disable-next-line`) unless the rule is incorrect for the specific case and the suppression is accompanied by a comment explaining why.

---

## 9. Compliance Verification
<!-- STD-MARKER: typescript.9 -->

- [ ] `tsconfig.json` includes `strict: true` and `noUncheckedIndexedAccess: true`.
- [ ] `any` is not used except in documented exceptions.
- [ ] `unknown` is narrowed with type guards before use.
- [ ] Object shapes use `interface`; union/intersection types use `type`.
- [ ] Functions do not mutate their arguments.
- [ ] All async operations use `async`/`await`.
- [ ] Async errors are handled — no unhandled promise rejections.
- [ ] `AbortSignal` is propagated through cancellable async call chains.
- [ ] Named exports are used; default exports limited to React components.
- [ ] ESLint and Prettier run in CI and fail the build on errors.

---

## 10. Governance
<!-- STD-MARKER: typescript.10 -->

This standard is owned by Troy Crowe. No changes to this file may be merged without Troy Crowe's explicit approval. Changes must be submitted as a pull request that includes a rationale comment explaining the reason for the update or deviation. Direct commits to `dev` or `main` are not permitted.
