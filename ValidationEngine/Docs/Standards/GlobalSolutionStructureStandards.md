# Solution Structure Standard

**Version:** 1.6.0
**Status:** Active
**Applies To:** All repositories under the GlobalStandards governance model
**Audience:** AI models and human developers
**Created:** 2025-01-01
**Last Modified:** 2026-07-18

---
<!-- STD-MARKER: solution-structure.file -->


## 1. Purpose
<!-- STD-MARKER: solution-structure.1 -->

This standard defines the solution and project structure rules that must be applied across all repositories. Both human developers and AI models must apply every rule in this file when creating, modifying, or reviewing repository structure, project layout, naming conventions, and configuration. A work slice that introduces a structural violation must not be marked complete and a PR must not be opened until the violation is resolved. Compliance is verified during pull request review and the pre-merge compliance process.

---

## 2. Visual Studio Solution Folder Structure
<!-- STD-MARKER: solution-structure.2 -->

Both human developers and AI models must verify that all new and existing solution structures conform to this section before closing a work slice. Any deviation must be corrected before the PR is opened.

A solution's folder structure must consist of a solution file at the repository root with a subdirectory for each project.

```
RepositoryRoot/
├── SolutionName.sln
├── SolutionName/
│   └── SolutionName.csproj
├── SolutionName.Tests/
│   └── SolutionName.Tests.csproj
```

No project directory may be placed inside another project directory. Every project gets exactly one top-level subdirectory.

---

## 2.1 Repository Standards Folder
<!-- STD-MARKER: solution-structure.2.1 -->

Both human developers and AI models must verify that every repository contains a root-level `Standards/` folder for repository-specific standards files and approved local deviations.

Canonical global standards remain in the GlobalStandards repository under `Docs/Standards/`. The root-level `Standards/` folder in each consuming repository is reserved for repository-local deviation files only. The folder name and the addendum file names within it both follow the PascalCase default defined in [Section 3.6](#36-default-folder-and-file-casing), per [`GlobalFileSpecificationStandards.md` Section 2.15](GlobalFileSpecificationStandards.md#215-repository-local-addendum-definition-required).

No repository other than the GlobalStandards repository may contain a copy, duplicate, mirror, or restatement of any global standards file, in whole or in part, under any folder name (for example `docs/`, `docs/standards/`, or any other path). A consuming repository must reference the canonical file in the GlobalStandards repository by relative link rather than copying its content. Any such duplicate found in a consuming repository must be deleted immediately upon discovery.

The repository `copilot-instructions.md` file must list the global baseline first by linking to the designated root governance standard. Any repository-specific deviations or local override documents must appear below that link and must live in the root-level `Standards/` folder.

---

## 2.2 Working Folder
<!-- STD-MARKER: solution-structure.2.2 -->

Both human developers and AI models must verify that a root-level `Working/` folder, when present, is used and retained only as described in this section.

`Working/` exists to hold agent-session and human-session working documents — carry-over notes, deferred-item lists, in-progress plans, and other files that carry work or context forward from one session to the next. It is not a general-purpose scratch or dumping folder and it is not part of the application, the pipeline, or the governed documentation set.

`Working/` must be listed in the repository's root `.gitignore` and nothing inside it is tracked in the repository. A file's presence in `Working/` is never itself a reason to keep it; a file is retained only while it is actively carrying work forward. Once a tracked effort is completed or abandoned, its detail file(s) must be deleted from `Working/` immediately — do not accumulate completed or stale working files. One-off scratch files (for example, single-use fix scripts) must be deleted as soon as their purpose is fulfilled, not left in place after the fact.

Every file placed in `Working/` must declare its own retention state in a `Status:` metadata line near the top of the file (for example `Status: Active`, `Status: In Review`, `Status: Approved`, `Status: Stale — pending deletion`). This status line makes the "actively carrying work forward" determination inspectable rather than relying solely on human judgment: a file whose status has not changed across a defined number of sessions, or that is explicitly marked `Stale`, is a candidate for immediate deletion. Human developers and AI models must update the `Status:` line whenever a file's retention state changes, and must delete any file marked `Stale` rather than leaving it in place.

When a repository maintains multiple concurrent working efforts, a root-level `Working/Backlog.md` file may be used to index them, with each active effort linking to its own detail file in `Working/`. `Backlog.md` itself remains subject to the same retention rule: an entry is removed, along with its detail file, as soon as that work is complete or abandoned.

---

## 2.3 Pipeline Scripts and Local Tooling Folders
<!-- STD-MARKER: solution-structure.2.3 -->

Both human developers and AI models must apply the correct root-level folder for scripts based on where and how the script executes.

A script that is actually invoked by an Azure Pipelines stage must reside in `.azure-pipelines/scripts/`. This folder is reserved exclusively for scripts referenced by pipeline YAML through an external file path (for example, a `File:` reference on a `PowerShell@2` or `AzureCLI@2` task). It must not be used for inline pipeline steps (`targetType: inline` / `scriptLocation: inlineScript`) — inline steps do not require an external file and `.azure-pipelines/scripts/` does not need to exist in a repository until an inline step is extracted into a standalone file.

A root-level `Tools/` folder is reserved for local developer scripts that are not executed by a pipeline — for example, a local pre-PR validation script that mirrors the dev pipeline sequence per [`GlobalAzureDevOpsPipelineStandards.md`](GlobalAzureDevOpsPipelineStandards.md). `Tools/` scripts are run manually or via a developer-invoked command; they are never referenced from pipeline YAML.

`.azure-pipelines/scripts/` (pipeline-executed), `Tools/` (local developer scripts), and `Working/` (temporary session files, see [Section 2.2](#22-working-folder)) are mutually exclusive by purpose. A script must live in exactly one of these locations based on how it is invoked, never based on convenience.

---

## 3. Naming Conventions
<!-- STD-MARKER: solution-structure.3 -->

Both human developers and AI models must apply every naming rule in this section to all new repositories, solutions, projects, and files. A naming violation found during a PR review must be resolved before the PR is approved.

### 3.1 Single-Project Repositories
<!-- STD-MARKER: solution-structure.3.1 -->

When a repository contains only one project, the repository name, solution name, and project name must all match exactly.

```
Repository:  CaptiveMessagingAPI
Solution:    CaptiveMessagingAPI.sln
Project:     CaptiveMessagingAPI/CaptiveMessagingAPI.csproj
```

### 3.2 Internal Project Folder Layout
<!-- STD-MARKER: solution-structure.3.2 -->

Applications follow a domain design pattern. All application layers — controllers, models, interfaces, services, repositories, authorizations, and utilities — are organized as folders within a single project. Layers are not split into separate projects.

The standard internal folder structure for an API project is:

```
SolutionName/
└── ProjectName/
    ├── Authorizations/
    ├── Controllers/
    ├── Extensions/
    ├── Interfaces/
    ├── Models/
    ├── Options/
    ├── Repositories/
    ├── Services/
    └── Utilities/
```

Web projects follow the same structure with the addition of MVC-specific folders. The `Repositories/` folder is not present in Web projects because the data layer is owned by the API layer; web projects consume data exclusively through API calls.

`Utilities/` remains the default home for general-purpose helper code that does not belong to a specific domain concern. A dedicated folder such as `Extensions/` is introduced only when a category of code is large enough, distinct enough, and consistent enough to warrant its own location. Until that bar is met, the code belongs in `Utilities/`.

```
SolutionName/
└── ProjectName/
    ├── Authorizations/
    ├── Controllers/
    ├── Enums/
    ├── Extensions/
    ├── Hubs/
    ├── Interfaces/
    ├── Models/
    ├── Services/
    ├── Utilities/
    ├── ViewComponents/
    ├── Views/
    └── wwwroot/
```

### 3.3 Acronym Suffixes
<!-- STD-MARKER: solution-structure.3.3 -->

Acronym suffixes must be written in full uppercase. Non-acronym descriptive folder and project names use PascalCase.

Correct: `CaptiveMessagingAPI`, `CaptiveExpensesWeb`, `CaptiveExpensesAPI`, `CaptiveAssetsDB`
Incorrect: `CaptiveMessagingApi`, `CaptiveExpensesdb`, `CaptiveAssetsDb`

### 3.4 Namespace Conventions
<!-- STD-MARKER: solution-structure.3.4 -->

[REF-001](../references/StandardsReferences.md#ref-001--using-statement-placement-inside-vs-outside-namespace) for research and decision rationale.

```csharp
namespace CaptiveExpenses.Models
{
    using System;

    public class AccessModel
    {
    }
}
```

### 3.5 Assembly Naming
<!-- STD-MARKER: solution-structure.3.5 -->

Assembly names must match the project name exactly.

### 3.6 Default Folder and File Casing
<!-- STD-MARKER: solution-structure.3.6 -->

Both human developers and AI models must apply PascalCase to every new folder and file name by default, including repository, solution, project, and standards-adjacent folders (for example, `Working/`, `Standards/`).

Lowercase or other non-PascalCase naming is permitted only when it is required by a system, platform, or third-party convention — for example, a `.NET` convention, an Azure Pipelines requirement, a NuGet requirement, or another explicit rule in a standards file. A rule documenting such a requirement must state the required casing explicitly.

---

## 4. Test Projects
<!-- STD-MARKER: solution-structure.4 -->

Both human developers and AI models must verify that a conforming test project exists before marking any work slice complete. Every solution must include at least one test project. Test projects must be placed in the same solution and repository as the code under test. Full test project structure, naming conventions, and coverage rules are defined in [`GlobalTestingStandards.md`](../standards/GlobalTestingStandards.md).

```
CaptiveExpensesAPI/
├── CaptiveExpensesAPI.sln
├── CaptiveExpensesAPI/
│   └── CaptiveExpensesAPI.csproj
└── CaptiveExpensesAPI.Tests/
    └── CaptiveExpensesAPI.Tests.csproj
```

No project beyond the primary application project and the test project may exist within a solution. Any class library consumed by a solution — whether used by one solution or by many — must live in its own dedicated repository and be referenced as a NuGet package under [Section 7 (Shared Class Libraries)](#7-shared-class-libraries). Additional projects must not be created unless a specific architectural need is identified, documented, and approved.

The only exemption is a library that is not consumed by any other solution as a dependency at all — for example a standalone tool such as the `ValidationEngine`, which is run directly rather than referenced by a consuming solution. Such a library still lives in its own repository, but it is not required to be built or published as a NuGet package, since nothing ever consumes it as a package dependency.

---

## 6. Database Projects
<!-- STD-MARKER: solution-structure.6 -->

Both human developers and AI models must apply the structure in this section to all database project repositories. SQL Server Database Projects follow the same folder structure as all other solutions.

```
CaptiveExpensesDB/
├── CaptiveExpensesDB.sln
└── CaptiveExpensesDB/
    ├── CaptiveExpensesDB.sqlproj
    ├── Deployment/
    ├── MigrationScripts/
    ├── Proposals/
    ├── Reports/
    ├── Runbooks/
    ├── Security/
    └── Tests/
```

Some folders may or may not exist depending on the needs of the project.

Internal database structure, migration strategy, and deployment conventions are defined separately in [`GlobalDatabaseStandards.md`](../standards/GlobalDatabaseStandards.md).

---

## 7. Shared Class Libraries
<!-- STD-MARKER: solution-structure.7 -->

Both human developers and AI models must verify that every shared library dependency is referenced as a NuGet package and that no shared library source project is committed as a project reference in a consuming solution before the work slice is closed.

This section applies in two distinct contexts. Sections 7.1, 7.3, and 7.4 govern the shared library's own repository — its ownership, structure, versioning, and local-development handling — and are verified against that library's repository, not against any repository that merely consumes it. Section 7.2's consumption requirement is the only rule in this section that applies to a consuming solution's repository: a consuming solution must reference the shared library as a NuGet package (`PackageReference`), never as a project reference or source-project inclusion. When reviewing a consuming solution for compliance, only the Section 7.2 consumption check is relevant; the library's own repository is where the remaining Section 7 rules are verified.

### 7.1 Ownership and Repository
<!-- STD-MARKER: solution-structure.7.1 -->

A shared class library is any reusable component consumed by a solution as a dependency — whether it is used by exactly one solution or by many, for example `HttpClientManager` or `DataImportExportManager`. Each shared library must live in its own dedicated repository with its own solution and test project. It must not be added as a source project into a consuming solution's `.sln` file and committed in that state.

This requirement does not apply to a library that stands entirely on its own and is not consumed by any other solution as a dependency — for example a standalone tool such as the `ValidationEngine`, which is run directly rather than referenced as a package. That library still lives in its own repository, but it is not required to be built or published as a NuGet package, since nothing ever consumes it as a package dependency.

### 7.2 Distribution via NuGet and Azure Artifacts
<!-- STD-MARKER: solution-structure.7.2 -->

Shared libraries must be built and published as NuGet packages to the organization's Azure Artifacts private feed. Consuming solutions reference the library as a NuGet package dependency, not as a project reference or a direct DLL reference.

Each library repository must have a [pipeline](../standards/GlobalAzureDevOpsPipelineStandards.md) that:
- Builds and tests the library on every PR to `dev`
- Builds, Publishes, Swaps, Validates, and Reverts a new package version to Azure Artifacts on every merge to `main`

### 7.3 Versioning
<!-- STD-MARKER: solution-structure.7.3 -->

Shared libraries must follow semantic versioning (`MAJOR.MINOR.PATCH`):

| Change type | Version segment to increment |
|---|---|
| Breaking change — existing callers must update | `MAJOR` |
| New capability — fully backwards compatible | `MINOR` |
| Bug fix or internal improvement | `PATCH` |

Consuming solutions must reference a version range that floats on minor and patch updates within a fixed major version. This allows non-breaking improvements to propagate automatically while requiring a deliberate update to adopt a breaking change.

```xml
<!-- Automatically receives minor and patch updates within major version 1 -->
<PackageReference Include="HttpClientManager" Version="[1.0, 2.0)" />
```

When a `MAJOR` version is published, consuming solutions must update and test explicitly before adopting the new version.

Application projects do not require deliberate assembly versioning management. Assembly versioning is only meaningful for shared libraries distributed as NuGet packages.

### 7.4 Local Development
<!-- STD-MARKER: solution-structure.7.4 -->

During active development of a shared library, the source project may be temporarily added to a consuming solution for debugging and testing purposes. This temporary reference must never be committed to the solution file. Before committing, the source project reference must be removed and replaced with the NuGet package reference.

Symbol packages (`.snupkg`) must be published alongside the NuGet package so that developers can step through library code in the debugger without needing the source project present in the solution.

---

## 8. Configuration Files
<!-- STD-MARKER: solution-structure.8 -->

Both human developers and AI models must verify that the required configuration files are present and that no secrets are committed to any configuration file before closing a work slice.

Every project must include at minimum:
- `appsettings.json` — the base configuration file present in all environments
- `appsettings.Development.json` — the development environment override

Additional environment-specific configuration files must be added as new environments are provisioned (e.g., `appsettings.Staging.json`, `appsettings.Production.json`).

Secrets must never be committed to any configuration file. Secret management conventions are defined in [`GlobalSecurityStandards.md`](../standards/GlobalSecurityStandards.md).

---

## 9. Repository .gitignore
<!-- STD-MARKER: solution-structure.9 -->

Both human developers and AI models must verify that a compliant `.gitignore` exists at the repository root before opening any PR. Every repository must include a `.gitignore` file at the repository root. The standard Visual Studio `.gitignore` provided by Microsoft must be used as the baseline.

- `bin/`
- `obj/`
- `.vs/`
- `*.user`
- `appsettings.*.json` overrides containing secrets
- `Working/` (see [Section 2.2](#22-working-folder))

Every repository governed by GlobalStandards — with the exception of the GlobalStandards repository itself — must use the same `.gitignore` baseline content described in this section. When this section is updated, the updated baseline must be propagated to every governed repository's `.gitignore` file so that `.gitignore` content stays consistent across repositories. The GlobalStandards repository maintains its own `.gitignore` independently because its folder layout (documentation and templates) differs from an application repository's layout.

---

## 10. Azure Functions
<!-- STD-MARKER: solution-structure.10 -->

No Azure Functions projects exist at the time of this writing. When the first Azure Functions project is created, best practices must be researched, agreed upon, and documented as an addendum to this standard before development begins.

---

## 11. React Application Structure
<!-- STD-MARKER: solution-structure.11 -->

Both human developers and AI models must apply the structure rules in this section to all React applications. React applications are not Visual Studio projects and do not use a solution file.

React applications must use TypeScript. Technology stack decisions beyond folder structure and naming conventions are defined in [`GlobalReactProjectStandards.md`](../standards/GlobalReactProjectStandards.md).

### 11.1 Folder Layout
<!-- STD-MARKER: solution-structure.11.1 -->

All source code must be organized under `src/` using a feature-first grouping. Each feature owns its components, hooks, services, and types. Code that is shared across features belongs in `src/shared/`. Root-level application setup — routing, global providers, and state — belongs in `src/app/`.

```
src/
├── features/
│   └── messageCenter/
│       ├── components/
│       ├── hooks/
│       ├── services/
│       ├── types/
│       └── index.ts
├── shared/
│   ├── components/
│   ├── hooks/
│   └── utils/
├── app/
└── index.tsx
```

### 11.2 Naming
<!-- STD-MARKER: solution-structure.11.2 -->

Component files use PascalCase. Hook files and utility files use camelCase. Feature folder names use camelCase. Type and interface files use PascalCase. Each feature must expose a barrel file named `index.ts`.

| Artifact | Convention | Example |
|---|---|---|
| Component file | PascalCase | `MessageList.tsx` |
| Hook file | camelCase | `useMessageList.ts` |
| Utility file | camelCase | `formatDate.ts` |
| Type file | PascalCase | `MessagePayload.ts` |
| Feature folder | camelCase | `messageCenter/` |
| Barrel file | `index.ts` | `features/messageCenter/index.ts` |

### 11.3 Depth and Colocation
<!-- STD-MARKER: solution-structure.11.3 -->

The maximum folder depth inside any `features/[featureName]/` directory is three levels. Tests must be colocated with the file they test, using the `.test.tsx` or `.test.ts` suffix. Imports must use absolute paths configured via `tsconfig.json`; relative paths that traverse more than one directory level are not permitted.

---

## 12. Global Error Handling
<!-- STD-MARKER: solution-structure.12 -->

Both human developers and AI models must verify that a global error handler is registered as the first middleware in every API and Web project before the PR is opened. Every application must include a global error handler.

For API projects the global error handler must be registered as middleware in `Program.cs`. For Web projects it must be configured using the built-in ASP.NET Core exception handling middleware. In both cases the handler must be registered before any other middleware that processes requests.

The global error handler class or middleware must be placed in the `Utilities/` folder unless a dedicated error handling folder is introduced. Error handling rules governing exception types, swallowed exceptions, and response formatting are defined in [`GlobalCodingStandards.md`](../standards/GlobalCodingStandards.md#7-error-handling).

---

## 13. Remediation
<!-- STD-MARKER: solution-structure.13 -->

When an existing repository does not conform to this standard, both human developers and AI models must complete the following steps before any new development work begins in that repository.

Two remediation paths are available:

### 13.1 In-Place Rename
<!-- STD-MARKER: solution-structure.13.1 -->

Use this path when only naming is incorrect and the structure is otherwise compliant.

1. Rename the solution file, project directories, and project files to match the naming conventions defined in [Section 3](#3-naming-conventions).
2. Update all namespace declarations throughout the codebase to mirror the corrected folder structure per [Section 3.4](#34-namespace-conventions).
3. Move all `using` statements inside the namespace block in every `.cs` file across the project, including controllers.
4. Update all assembly names to match the corrected project names per [Section 3.5](#35-assembly-naming).
5. Update Azure DevOps pipeline references to reflect renamed files and directories.
6. Update any NuGet package references if the project is a [shared library](#7-shared-class-libraries).
7. Verify the repository name in Azure DevOps matches the solution name and rename if necessary.

### 13.2 Recreate and Migrate
<!-- STD-MARKER: solution-structure.13.2 -->

Use this path when both naming and structure are non-compliant, or when the overhead of in-place renaming is too high.

1. Create a new solution and project structure conforming to this standard.
2. Migrate all source files into the new structure.
3. Update all namespaces to mirror the new folder structure per [Section 3.4](#34-namespace-conventions).
4. Move all `using` statements inside the namespace block in every `.cs` file across the project, including controllers.
5. Update all [pipeline](../standards/GlobalAzureDevOpsPipelineStandards.md) references, NuGet references, and repository settings.
6. Delete the old non-compliant solution structure.
7. Verify compliance against [Section 14](#14-compliance-verification) before merging.

Remediation progress for each repository is tracked within that repository's own governance or onboarding notes, not in this document.

---

## 14. Compliance Verification
<!-- STD-MARKER: solution-structure.14 -->

- [ ] The solution file is at the repository root. <!-- STD-MARKER: solution-structure.14.1 -->
- [ ] A root-level `Standards/` folder exists for repository-specific standards files and approved local deviations. <!-- STD-MARKER: solution-structure.14.2 -->
- [ ] The repository `copilot-instructions.md` file points first to the designated root governance standard. <!-- STD-MARKER: solution-structure.14.3 -->
- [ ] A `.gitignore` file exists at the repository root using the Microsoft Visual Studio baseline. <!-- STD-MARKER: solution-structure.14.4 -->
- [ ] No project directory is nested inside another project directory. <!-- STD-MARKER: solution-structure.14.5 -->
- [ ] All acronym suffixes are fully uppercase. <!-- STD-MARKER: solution-structure.14.6 -->
- [ ] Assembly names match their project names. <!-- STD-MARKER: solution-structure.14.7 -->
- [ ] Namespaces mirror the folder structure; `using` statements are inside the namespace block. <!-- STD-MARKER: solution-structure.14.8 -->
- [ ] Application layers are implemented as folders within a single project, not as separate projects. <!-- STD-MARKER: solution-structure.14.9 -->
- [ ] No additional projects exist beyond the primary application project and the test project (any consumed class library lives in its own repository, not as an in-solution project). <!-- STD-MARKER: solution-structure.14.10 -->
- [ ] Any class library consumed by this solution as a dependency is referenced as a NuGet package from its own dedicated repository, not as an in-solution project. <!-- STD-MARKER: solution-structure.14.11 -->
- [ ] A test project named `[SolutionName].Tests` exists in the solution. <!-- STD-MARKER: solution-structure.14.12 -->
- [ ] A minimum of `appsettings.json` and `appsettings.Development.json` exist in every project. <!-- STD-MARKER: solution-structure.14.13 -->
- [ ] No secrets are committed to any configuration file. <!-- STD-MARKER: solution-structure.14.14 -->
- [ ] Database solutions follow the same `SolutionName/ProjectName/` folder structure as all other solutions. <!-- STD-MARKER: solution-structure.14.15 -->
- [ ] No shared class library source project is committed as a project reference in a consuming solution's `.sln` file. <!-- STD-MARKER: solution-structure.14.16 -->
- [ ] All shared library dependencies are referenced as NuGet packages using a `[MAJOR.0, MAJOR+1.0)` version range. <!-- STD-MARKER: solution-structure.14.17 -->
- [ ] React repositories may or may not include a solution file. <!-- STD-MARKER: solution-structure.14.18 -->
- [ ] React source code uses feature-first layout under `src/features/`. <!-- STD-MARKER: solution-structure.14.19 -->
- [ ] No feature folder exceeds three levels of depth. <!-- STD-MARKER: solution-structure.14.20 -->
- [ ] React component files must be PascalCase. <!-- STD-MARKER: solution-structure.14.21 -->
- [ ] React hook and utility files must be camelCase. <!-- STD-MARKER: solution-structure.14.22 -->
- [ ] A global error handler is registered as the first middleware in `Program.cs` for API and Web projects. <!-- STD-MARKER: solution-structure.14.23 -->
- [ ] The global error handler does not expose raw stack traces or internal error details to the caller. <!-- STD-MARKER: solution-structure.14.24 -->
- [ ] No repository other than GlobalStandards contains a copy, duplicate, mirror, or restatement of any global standards file. <!-- STD-MARKER: solution-structure.14.25 -->
- [ ] All folder and file names use PascalCase with no hyphenation, except where a system, platform, or third-party convention requires otherwise. <!-- STD-MARKER: solution-structure.14.26 -->

---

## 15. Governance
<!-- STD-MARKER: solution-structure.15 -->

This standard is owned by Troy Crowe. No changes to this file may be merged without Troy Crowe's explicit approval. Changes must be submitted as a pull request that includes a rationale comment explaining the reason for the update or deviation. Direct commits to `dev` or `main` are not permitted.
