# Coverage Matrix — GlobalReactProjectStandards.md

**Status:** Paused — technology not yet in use, see backlog
**Sign Off:**
**Sign Off Date:**
**Revision Date:**
**Columns**

| Column | Meaning |
|---|---|
| STD-MARKER | Rule id from the standards file. |
| Rule Summary | One-line paraphrase of the rule. |
| Category | Technology / Structure / Development / Testing. |
| Confidence | Binary (deterministic pass/fail) / Heuristic (risk-flag, needs human confirmation) / Manual-only (no automation path). |
| Severity | Hard-stop / Warning / Manual-only-comment. |
| Frequency | Every-Commit (change-set scoped, TS/TSX file changes). |
| Technique | Short detection approach. |
| Status | Missing / Existing / Acknowledged-future-work. |

**File status note:** This standard's own header declares `**Status:** Draft`, and its "Activation Notes" section explicitly states it has not been formally reviewed or promoted to Active status, pending confirmation of the technology stack before any React work begins. Per `GlobalFileSpecificationStandards.md` Section 2.8, a `Draft` standard "must not be enforced until promoted to Active." **All rows in this matrix are therefore classified `Acknowledged-future-work` regardless of their individual Confidence/Severity/Technique rating**, consistent with the CaptiveExpensesApi repository's own marker-exclusion table which excludes `react-project.file` entirely ("This solution contains no React application"). This matrix is drafted now for completeness of the GlobalStandards corpus and to be ready the moment a React project is initiated and the standard is promoted to Active.

---

### Section 2 — Technology Baseline

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| react-project.2 | Approved stack: React 18+, TypeScript, Vite, React Router v6+, TanStack Query (server state), React Context/Zustand (client state), CSS Modules or Tailwind, Vitest + React Testing Library, ESLint (+`@typescript-eslint`+`eslint-plugin-react-hooks`), Prettier; no new library introduced for an already-covered concern without a documented repository-addendum reason (e.g., no competing state library like Redux alongside TanStack Query/Zustand) | Technology | Heuristic | Warning | Every-Commit (on `package.json` changes) | Config-scan: parse `package.json` dependencies against the approved-stack allowlist; flag any dependency matching a known competing-library denylist (e.g., `redux`, `mobx`, `enzyme`) unless a matching repository addendum entry exists; version-range check for `react` ≥ 18 | Acknowledged-future-work |

---

### Section 3 — Project Structure

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| react-project.3 | Fixed `src/` layout (`assets/`, `components/`, `features/`, `hooks/`, `lib/`, `pages/`, `router/`, `store/`, `types/`, `App.tsx`, `main.tsx`); feature-specific code must not live in `components/` (belongs in `features/{featureName}/`); each `components/` entry lives in its own folder with component file, styles, test, and a single `index.ts` named re-export | Structure | Binary | Hard-stop | Every-Commit | File-path scan: verify top-level `src/` folder set matches the expected structure; verify each folder under `components/` contains exactly one `.tsx` component file, an optional style file, a `.test.tsx`, and an `index.ts`; flag any component-like file (PascalCase `.tsx` exporting a function returning JSX) placed directly under `components/` outside its own subfolder — this overlaps with and refines `GlobalSolutionStructureStandards.md` solution-structure.11.1's more general feature-first layout rule | Acknowledged-future-work |

---

### Section 4 — Component Rules

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| react-project.4.1 | Function components only; class components prohibited | Development | Binary | Hard-stop | Every-Commit | AST/regex scan (TS-ESLint rule or custom): flag `class ... extends React.Component`/`extends Component` declarations in `.tsx` files | Acknowledged-future-work |
| react-project.4.2 | A component rendering more than ~150 lines of JSX is a decomposition candidate; sub-components meaningful only in their parent's context are prefixed with the parent name (e.g., `DocumentList`, `DocumentListItem`) | Development | Heuristic | Warning | Every-Commit | AST line-count scan: measure JSX return-block line count per component and flag components exceeding ~150 lines as a decomposition candidate; the "150 lines" and naming-prefix conventions are guidance requiring human judgment on when decomposition is actually warranted, keeping this Heuristic | Acknowledged-future-work |
| react-project.4.3 | Every component's props typed via a named TypeScript `interface`; inline object types only permitted for a trivial one-prop component | Development | Binary | Hard-stop | Every-Commit | TS-ESLint rule (e.g., a custom rule or `@typescript-eslint` config) requiring a named `interface`/`type` for function component parameter destructuring beyond a single primitive prop | Acknowledged-future-work |
| react-project.4.4 | React components use default exports; all other TypeScript modules use named exports (per `GlobalTypeScriptStandards.md`) | Development | Binary | Hard-stop | Every-Commit | ESLint rule (`import/prefer-default-export` scoped to component files, `import/no-default-export` scoped to non-component files) or custom AST check distinguishing component files (return JSX) from other modules | Acknowledged-future-work |
| react-project.4.5 | No component file named `index.tsx`; component files named after the component (`DocumentCard.tsx`); `index.ts` (not `.tsx`) reserved for the re-export barrel only | Development | Binary | Hard-stop | Every-Commit | File-path scan: flag any `index.tsx` file in the repository; verify `index.ts` (non-`.tsx`) files contain only re-export statements, not component definitions | Acknowledged-future-work |

---

### Section 5 — Hooks

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| react-project.5.1 | `eslint-plugin-react-hooks` enabled in lint config and must not be suppressed | Development | Binary | Hard-stop | Every-Commit (on ESLint config changes) | Config-scan: verify `eslint-plugin-react-hooks` is present in `.eslintrc`/`eslint.config.js` and its rules are not overridden to `off`; regex scan across `.tsx` files for `eslint-disable` comments targeting `react-hooks/*` rules | Acknowledged-future-work |
| react-project.5.2 | Custom hooks named with `use` prefix; feature-specific hooks live in `features/{featureName}/hooks/`; cross-feature hooks live in `src/hooks/` | Development | Binary | Hard-stop | Every-Commit | Regex: flag any exported function starting with `use` that is not placed under a `hooks/` folder, and any function under a `hooks/` folder not prefixed with `use` (this is the standard `eslint-plugin-react-hooks` naming convention check, extended with the folder-placement rule) | Acknowledged-future-work |
| react-project.5.3 | `useMemo`/`useCallback` used only with a measured performance justification, not speculatively | Development | Manual-only | Manual-only-comment | Periodic (code-review) | Requires a performance measurement (profiler data) to justify each usage — not mechanically verifiable from source alone; a Heuristic flag could count `useMemo`/`useCallback` call sites and prompt a reviewer to confirm justification exists, but true verification is Manual-only | Acknowledged-future-work |

---

### Section 6 — Server State and Data Fetching

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| react-project.6.1 | All server-originated data managed via TanStack Query (`useQuery`/`useMutation`); `useEffect` + `useState` must not be used to fetch/hold server data | Development | Heuristic | Hard-stop | Every-Commit | AST scan: flag `useEffect` bodies containing an async fetch call (`fetch(`, `axios.`, or a call to a known `*Service` function) paired with a `useState` setter in the same component — a reasonably reliable pattern match, though edge cases (non-server-data `useEffect`+`useState` usage) require judgment, keeping this Heuristic | Acknowledged-future-work |
| react-project.6.2 | Query keys are arrays with entity name as the first element and progressively narrowing subsequent elements | Development | Heuristic | Warning | Every-Commit | AST scan: verify the `queryKey` option passed to `useQuery`/`useMutation` calls is an array literal (not a string or template literal); the "consistent progressive narrowing" semantic convention is not mechanically verifiable beyond the array-literal structural check | Acknowledged-future-work |
| react-project.6.3 | API call functions defined in a feature's `services/` file; `fetch`/`axios` calls not inlined inside `useQuery` options; query hooks stay thin (key + service call only) | Structure | Heuristic | Warning | Every-Commit | File-path scan: verify `fetch(`/`axios.` calls occur only within files under a `services/` folder; AST scan flagging `fetch`/`axios` calls directly inside a `useQuery`/`useMutation` `queryFn`/`mutationFn` inline arrow function body (rather than delegating to a service import) | Acknowledged-future-work |

---

### Section 7 — Client State

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| react-project.7 | React Context for small shared client-only state; Zustand for global client state accessed widely; server state must not be placed in Zustand/Context; Zustand stores kept small and focused, one store per domain concern (no single accumulating global store) | Development | Heuristic | Warning | Every-Commit | AST scan: flag Zustand `create()` calls or Context provider values whose shape includes obvious server-entity fields (heuristic name-matching against known server-data field names) as a possible server-state-in-client-state violation; "one store per domain concern" size/scope is a judgment call requiring a reviewer, though a simple line-count/field-count threshold on `create()` calls could serve as a coarse Heuristic proxy | Acknowledged-future-work |

---

### Section 8 — Routing

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| react-project.8 | All routes defined in `src/router/`, not scattered across components; page-level components lazy-loaded via `React.lazy`/`import()`; authenticated routes protected by a guard component; route-level auth decisions must not replace server-side authorization (server endpoints always enforce authorization independently) | Structure | Heuristic | Hard-stop | Every-Commit | File-path scan: flag `<Route` JSX elements or router-config objects defined outside `src/router/`; AST scan: verify page-level component imports for routes use `lazy(() => import(...))` rather than static imports; "server always enforces authorization independently" duplicates `GlobalSecurityStandards.md` security.3.x authorization rules and is verified server-side, not in the React codebase | Acknowledged-future-work |

---

### Section 9 — Styling

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| react-project.9 | CSS Modules or Tailwind CSS, not both in the same project; no inline `style` props for layout/visual styling; no global CSS resets/rules affecting component internals from outside the component's own CSS module | Development | Heuristic | Warning | Every-Commit | Config-scan: detect presence of both `tailwind.config.*` and widespread `*.module.css` usage in the same project as a mixed-approach flag; regex/AST scan: flag `style={{` inline-style JSX attributes; flag global (non-scoped) `.css` files containing selectors matching component class names outside their own module | Acknowledged-future-work |

---

### Section 10 — Error Handling

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| react-project.10.1 | Every route-level component wrapped in an `ErrorBoundary` (paired with `Suspense` per the shown pattern) | Development | Binary | Hard-stop | Every-Commit | AST scan: for each lazy-loaded route component identified via react-project.8's detection, verify its JSX usage in the router config is wrapped by an `<ErrorBoundary>` element | Acknowledged-future-work |
| react-project.10.2 | Every `useQuery`/`useMutation` call site handles the `isError`/`error` state; error states must not be left unrendered | Development | Heuristic | Hard-stop | Every-Commit | AST scan: for each `useQuery`/`useMutation` call, verify the destructured/returned result's `isError` or `error` field is referenced somewhere in the component body (e.g., in a conditional render) — a reasonably reliable structural check, though confirming the error state is meaningfully "rendered" (not just referenced in a no-op) requires judgment, keeping this Heuristic | Acknowledged-future-work |

---

### Section 11 — Testing

| STD-MARKER | Rule Summary | Category | Confidence | Severity | Frequency | Technique | Status |
|---|---|---|---|---|---|---|---|
| react-project.11.1 | Vitest as test runner, React Testing Library for component tests; Enzyme prohibited; no testing of implementation details (internal state, method calls on component instances) | Testing | Heuristic | Hard-stop | Every-Commit | Config-scan: verify `vitest` and `@testing-library/react` are dependencies; flag any `enzyme` dependency outright (Binary); "no implementation-detail testing" requires inspecting test file content for anti-patterns (e.g., accessing component instance internals), which is Heuristic | Acknowledged-future-work |
| react-project.11.2 | Tests cover user interactions, rendered output, and hook/component integration; TanStack Query hooks tested via `renderHook` + `QueryClientProvider` wrapper with mocked API layer; must not test CSS class names, internal state, or non-user-meaningful DOM element existence | Testing | Heuristic | Warning | Every-Commit | AST scan of test files: flag assertions on `className` string equality or `toHaveClass` used for non-semantic checks; flag direct access to component instance state; verify `renderHook` usage for custom-hook tests wraps with a `QueryClientProvider` — largely Heuristic since "user-meaningful" is a judgment call | Acknowledged-future-work |
| react-project.11.3 | Global test setup file provides a default `QueryClient` wrapper for TanStack Query component tests and MSW (Mock Service Worker) for API mocking in integration-level tests | Testing | Binary | Hard-stop | Every-Commit (on test-setup file changes) | Config-scan: verify a designated test setup file (e.g., `src/test/setup.ts`) exists and is referenced in `vitest.config.ts`; verify it configures a `QueryClientProvider` wrapper utility and imports/configures `msw` | Acknowledged-future-work |
| react-project.11.4 | Test files named `{ComponentName}.test.tsx` and colocated with the component file they test | Testing | Binary | Hard-stop | Every-Commit | Regex/file-path scan: verify each `.tsx` component file has an adjacent `{ComponentName}.test.tsx` file in the same folder — duplicates `GlobalSolutionStructureStandards.md` solution-structure.11.3's colocated-test-suffix rule for this specific naming convention | Acknowledged-future-work |

---

### Section 12 — Compliance Verification (Excluded as an independent detection target)

The unmarked checklist items in Section 12 restate Sections 2–11 as a pre-merge checklist (note: unlike other standards files reviewed in this batch, these checklist bullets do **not** carry individual rule-item `STD-MARKER` comments — this is itself a `file-specification.2.10`/`2.14` compliance gap in this Draft file, since every checklist item is required to carry its own rule-item marker). No independent detection techniques are introduced beyond what is captured above.

### Section 13 — Governance (Excluded)

`react-project.13` (ownership/approval boilerplate) is excluded — not code-checkable, procedural ownership statement only.

---

**Notes:**
1. **This entire file is Acknowledged-future-work** pending the standard's promotion from `Draft` to `Active` status per its own Activation Notes and `GlobalFileSpecificationStandards.md` file-specification.2.8. No enforcement should be implemented against this file until that promotion occurs and the CaptiveExpensesApi (or any other) repository's `react-project.file` exclusion marker is removed for repos that actually adopt React.
2. This matrix surfaces a **structural non-compliance in the standard itself**: Section 12's Compliance Verification checklist items lack individual `<!-- STD-MARKER: react-project.12.N -->` rule-item markers, which `GlobalFileSpecificationStandards.md` file-specification.2.10 and file-specification.2.14 require of every checklist item. Worth flagging to the standards owner (Troy Crowe) when this file is reviewed for Active promotion — the Coverage Matrix effort had to reference section-level rules directly in this file since rule-item markers don't exist yet.
3. Detection here is dominated by **TypeScript/JSX AST analysis** (component export style, hook usage patterns, JSX structure) rather than the file-path/naming scans that dominated Solution Structure, or the ADO-API queries that dominated Repository Standards — a fourth distinct detection-technique category alongside Roslyn (C#), file-tree/regex (structure), and ADO REST API (repository/PR process). A React-specific ESLint plugin (custom rules) is likely the most practical single implementation vehicle for the Binary/Heuristic rules in Sections 4–11.
4. Several rules cross-reference and refine rules already captured in `GlobalSolutionStructureStandards.md` Section 11 (React Application Structure) — e.g., react-project.3 restates and details solution-structure.11.1's folder layout, and react-project.11.4 restates solution-structure.11.3's test-colocation rule. Per file-specification.2.2 (single responsibility, no duplicated rules across files), these overlaps should be reviewed for consolidation once this file is promoted to Active — solution-structure.11.x appears intended as the general layout/naming rule with react-project.md as the domain-specific elaboration, but the current phrasing risks drifting out of sync.

