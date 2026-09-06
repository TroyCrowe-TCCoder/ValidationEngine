# React Project Standards

**Version:** 1.0.0
**Status:** Draft
**Applies To:** All repositories under the GlobalStandards governance model that contain React applications
**Audience:** AI models and human developers
**Created:** 2026-07-12
**Last Modified:** 2026-07-13

---
<!-- STD-MARKER: react-project.file -->


## Activation Notes

This document is drafted in preparation for an upcoming UI rebuild using React. It has not yet been formally reviewed or promoted to Active status.

**Before starting any React work, complete the following steps:**

1. Review this document in full and confirm the technology stack choices are still current.
2. Update the `**Status:**` field from `Draft` to `Active` once the stack is confirmed.
3. Evaluate whether VS Code's scoped `.instructions.md` files (placed in `.github/instructions/`) would provide enough value to be worth creating. A scoped `react.instructions.md` and `typescript.instructions.md` can reference these standards documents as focused Copilot context without duplicating their content. This is only relevant when working in VS Code — defer the decision until that point.
4. Confirm the STD-004 pipeline setup guide covers the Vite build step and any static hosting deployment stage needed for the React application.

---

## 1. Purpose
<!-- STD-MARKER: react-project.1 -->

This document defines the React-specific rules for project setup, component design, state management, data fetching, routing, styling, and testing. It supplements `GlobalTypeScriptStandards.md` and `GlobalSolutionStructureStandards.md`. Rules in those documents take precedence where they conflict with anything here.

---

## 2. Technology Baseline
<!-- STD-MARKER: react-project.2 -->

The approved stack for all React projects is:

| Concern | Approved Technology |
|---|---|
| Framework | React 18+ |
| Language | TypeScript (see `GlobalTypeScriptStandards.md`) |
| Build tool | Vite |
| Routing | React Router v6+ |
| Server state | TanStack Query (React Query) |
| Client state | React Context or Zustand for global client-only state |
| Styling | CSS Modules or Tailwind CSS |
| Testing | Vitest + React Testing Library |
| Linting | ESLint + `@typescript-eslint` + `eslint-plugin-react-hooks` |
| Formatting | Prettier |

Do not introduce new libraries for concerns already covered by this stack without documenting the reason in the repository addendum. Adding a competing state management library (e.g., Redux) alongside TanStack Query and Zustand increases cognitive overhead with no benefit.

---

## 3. Project Structure
<!-- STD-MARKER: react-project.3 -->

```
src/
  assets/              ← static assets (images, fonts)
  components/          ← reusable UI components not tied to a feature
    Button/
      Button.tsx
      Button.module.css
      Button.test.tsx
      index.ts         ← named re-export only
  features/            ← feature-scoped code
    documents/
      components/      ← components used only within this feature
      hooks/           ← custom hooks for this feature
      services/        ← API call functions for this feature
      types.ts         ← TypeScript types local to this feature
      index.ts         ← public API of the feature
  hooks/               ← application-wide custom hooks
  lib/                 ← thin wrappers over third-party libraries
  pages/               ← route-level components
  router/              ← routing configuration
  store/               ← global client state (Zustand stores)
  types/               ← shared application-wide TypeScript types
  App.tsx
  main.tsx
```

- Do not place feature-specific code in `components/`. Feature code lives in `features/{featureName}/`.
- Each component in `components/` lives in its own folder with the component file, styles, test, and a single `index.ts` re-export.

---

## 4. Component Rules
<!-- STD-MARKER: react-project.4 -->

### 4.1 — Function Components Only
<!-- STD-MARKER: react-project.4.1 -->

Use function components exclusively. Class components are prohibited. There are no use cases where a class component is required in React 18+.

### 4.2 — Component Size
<!-- STD-MARKER: react-project.4.2 -->

A component that renders more than approximately 150 lines of JSX is a candidate for decomposition. Split large render trees into smaller named sub-components. Name sub-components that are only meaningful in the context of their parent with the parent name as a prefix: `DocumentList`, `DocumentListItem`, `DocumentListEmptyState`.

### 4.3 — Props Interface
<!-- STD-MARKER: react-project.4.3 -->

Define a TypeScript `interface` for every component's props. Do not use inline object types in the function signature for anything beyond a one-prop trivial component.

```typescript
interface DocumentCardProps {
  document: DocumentMetadata;
  onSelect: (id: string) => void;
  isSelected?: boolean;
}

function DocumentCard({ document, onSelect, isSelected = false }: DocumentCardProps) {
  ...
}
```

### 4.4 — Default Exports for Components
<!-- STD-MARKER: react-project.4.4 -->

React components use default exports to align with dynamic import (`React.lazy`) and the conventions of the ecosystem. All other TypeScript modules use named exports per `GlobalTypeScriptStandards.md`.

```typescript
export default function DocumentCard(...) { ... }
```

### 4.5 — No Index Components
<!-- STD-MARKER: react-project.4.5 -->

Do not name a component `index.tsx` inside a folder. Name the file after the component: `DocumentCard.tsx`. Use `index.ts` (not `.tsx`) as the re-export barrel only.

---

## 5. Hooks
<!-- STD-MARKER: react-project.5 -->

### 5.1 — Rules of Hooks
<!-- STD-MARKER: react-project.5.1 -->

Follow the React rules of hooks at all times. ESLint's `eslint-plugin-react-hooks` must be enabled in the lint configuration and must not be suppressed.

### 5.2 — Custom Hook Naming and Scope
<!-- STD-MARKER: react-project.5.2 -->

Custom hooks must be named with the `use` prefix. A custom hook that encapsulates logic for a specific feature belongs in `features/{featureName}/hooks/`. A hook that is used across multiple features belongs in `src/hooks/`.

### 5.3 — useMemo and useCallback
<!-- STD-MARKER: react-project.5.3 -->

Use `useMemo` and `useCallback` only when there is a measured performance reason to do so (a re-render is verifiably expensive or a reference is used as a dependency in another hook). Do not add them speculatively — they add cognitive overhead and their cost can exceed their benefit when applied without measurement.

---

## 6. Server State and Data Fetching
<!-- STD-MARKER: react-project.6 -->

### 6.1 — TanStack Query for All Server State
<!-- STD-MARKER: react-project.6.1 -->

All data that originates from the server is server state. Server state must be managed with TanStack Query (`useQuery`, `useMutation`). Do not use `useEffect` + `useState` to fetch and hold server data.

```typescript
// CORRECT
function useDocuments(clientId: number) {
  return useQuery({
    queryKey: ['documents', clientId],
    queryFn: () => documentsService.getByClient(clientId),
  });
}

// WRONG
function useDocuments(clientId: number) {
  const [documents, setDocuments] = useState<DocumentMetadata[]>([]);
  useEffect(() => {
    documentsService.getByClient(clientId).then(setDocuments);
  }, [clientId]);
  return documents;
}
```

### 6.2 — Query Key Conventions
<!-- STD-MARKER: react-project.6.2 -->

Query keys must be arrays. The first element is the entity name. Additional elements narrow the scope:

```typescript
['documents']                    // all documents
['documents', clientId]          // documents for a client
['documents', clientId, docId]   // a specific document
['documents', 'upload-status']   // a domain-specific query variant
```

Consistent query key structure makes invalidation predictable.

### 6.3 — API Call Functions
<!-- STD-MARKER: react-project.6.3 -->

Define API call functions in a `services/` file within the relevant feature folder. Do not inline `fetch` or `axios` calls inside `useQuery` options. Keep the query hook thin — it must declare only the key and the call to the service function.

```typescript
// features/documents/services/documentsService.ts
export const documentsService = {
  getByClient: (clientId: number): Promise<DocumentMetadata[]> =>
    apiClient.get(`/documents?clientId=${clientId}`).then(r => r.data),
};
```

---

## 7. Client State
<!-- STD-MARKER: react-project.7 -->

Use React Context for simple client-only state shared across a small number of components. Use Zustand for global client state that is accessed across many components or features.

Do not put server state into Zustand or Context. TanStack Query already provides a global cache for server state.

Keep Zustand stores small and focused. One store per domain concern. Do not create a single global store that accumulates all application state.

---

## 8. Routing
<!-- STD-MARKER: react-project.8 -->

- Define all routes in `src/router/`. Do not scatter route definitions across components.
- Use lazy-loaded routes for all page-level components to enable code splitting:

```typescript
const DocumentsPage = lazy(() => import('../pages/DocumentsPage'));
```

- Protect authenticated routes with a guard component that checks the authentication state and redirects to the login page if unauthenticated.
- Do not use route-level authentication decisions to replace server-side authorization. Server endpoints must always enforce authorization independently.

---

## 9. Styling
<!-- STD-MARKER: react-project.9 -->

- Use CSS Modules for component-scoped styles or Tailwind CSS for utility-first styling. Do not mix both approaches within the same project.
- Do not use inline `style` props for layout or visual styling. Use class names.
- Do not use global CSS resets or style rules that affect component internals from outside the component's own CSS module.

---

## 10. Error Handling
<!-- STD-MARKER: react-project.10 -->

### 10.1 — Error Boundaries
<!-- STD-MARKER: react-project.10.1 -->

Wrap every route-level component in an error boundary. An unhandled rendering error in a deeply nested component must not crash the entire application.

```typescript
<ErrorBoundary fallback={<ErrorPage />}>
  <Suspense fallback={<LoadingSpinner />}>
    <DocumentsPage />
  </Suspense>
</ErrorBoundary>
```

### 10.2 — TanStack Query Error Handling
<!-- STD-MARKER: react-project.10.2 -->

Every `useQuery` and `useMutation` must handle the `isError` / `error` state. Do not leave query error states unrendered.

---

## 11. Testing
<!-- STD-MARKER: react-project.11 -->

### 11.1 — Testing Library
<!-- STD-MARKER: react-project.11.1 -->

Use Vitest as the test runner and React Testing Library for component tests. Do not use Enzyme. Do not test implementation details (internal state, method calls on component instances).

### 11.2 — What to Test
<!-- STD-MARKER: react-project.11.2 -->

- User interactions: clicks, form submissions, keyboard navigation.
- Rendered output: what the user sees given specific props and state.
- Integration between components and custom hooks.
- TanStack Query: use `renderHook` with a `QueryClientProvider` wrapper and mock the API layer.

Do not test CSS class names, internal state values, or the existence of specific DOM elements that are not meaningful to the user.

### 11.3 — Test Setup
<!-- STD-MARKER: react-project.11.3 -->

Configure a global test setup file that provides:

- A default `QueryClient` wrapper for all component tests that use TanStack Query.
- MSW (Mock Service Worker) for API mocking in integration-level component tests.

### 11.4 — Naming
<!-- STD-MARKER: react-project.11.4 -->

Test files must be named `{ComponentName}.test.tsx` and placed alongside the component file they test.

---

## 12. Compliance Verification
<!-- STD-MARKER: react-project.12 -->

- [ ] Project uses the approved technology stack (React 18+, Vite, TanStack Query, etc.).
- [ ] Project structure follows the `src/features/` and `src/components/` layout.
- [ ] All components are function components.
- [ ] All component props are typed with a named `interface`.
- [ ] No `useEffect` + `useState` pattern used for server data fetching.
- [ ] TanStack Query manages all server state.
- [ ] API call functions are defined in feature `services/` files.
- [ ] Routes are defined in `src/router/` and use lazy loading.
- [ ] Every route-level component is wrapped in an error boundary.
- [ ] ESLint `react-hooks` plugin is enabled and not suppressed.
- [ ] Tests use Vitest and React Testing Library.
- [ ] Tests cover user interactions and rendered output, not implementation details.

---

## 13. Governance
<!-- STD-MARKER: react-project.13 -->

This standard is owned by Troy Crowe. No changes to this file may be merged without Troy Crowe's explicit approval. Changes must be submitted as a pull request that includes a rationale comment explaining the reason for the update or deviation. Direct commits to `dev` or `main` are not permitted.
