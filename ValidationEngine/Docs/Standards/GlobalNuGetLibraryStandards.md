# NuGet Library Standards

**Version:** 1.4.0
**Status:** Active
**Applies To:** All repositories under the GlobalStandards governance model that produce or consume internal NuGet packages
**Audience:** AI models and human developers
**Created:** 2026-07-12
**Last Modified:** 2026-08-16

---
<!-- STD-MARKER: nuget-library.file -->


## 1. Purpose
<!-- STD-MARKER: nuget-library.1 -->

This document defines the rules for creating shared libraries, structuring library repositories, packaging internal NuGet libraries, publishing those packages through governed pipelines, and consuming those packages from other repositories. These rules apply to shared libraries such as `HttpClientManager` and `DataImportExportManager`.

---

## 2. Shared Library Scope
<!-- STD-MARKER: nuget-library.2 -->

### 2.1 Shared Library Qualification
<!-- STD-MARKER: nuget-library.2.1 -->

A codebase may be implemented as a NuGet library when the capability benefits from independent packaging, versioning, and consumption through package management rather than committed source-project inclusion.

A shared library used by multiple repositories must be implemented as a NuGet package rather than copied between repositories or committed into consuming solutions.

### 2.2 Repository Ownership
<!-- STD-MARKER: nuget-library.2.2 -->

Every shared library must live in its own dedicated repository with its own solution and test project as required by [`GlobalSolutionStructureStandards.md` Section 7](../standards/GlobalSolutionStructureStandards.md#7-shared-class-libraries).

A consuming repository must not commit a shared library source project into its solution. Shared libraries must be consumed as NuGet packages only.

**Analyzer-package-family exception:** A repository explicitly designated as an analyzer-package family (a repository whose sole purpose is hosting multiple related Roslyn analyzer/code-fix NuGet packages, grouped by standards domain, such as `GlobalStandards.Analyzers`) may host more than one shared library. All domain packages within that repository share a single repository-root solution (`.slnx`); a repository-per-domain solution split is not required because packaging, versioning, and CI triggering are project-scoped, not solution-scoped. Each domain package must still have its own dedicated project and its own test project under that domain's folder. This exception applies only to analyzer-package-family repositories and must not be used to justify combining unrelated shared libraries into one repository in any other case.

---

## 3. Repository and Project Structure
<!-- STD-MARKER: nuget-library.3 -->

### 3.1 Required Solution Layout
<!-- STD-MARKER: nuget-library.3.1 -->

A shared library repository must use the standard repository and solution layout defined in [`GlobalSolutionStructureStandards.md` Section 2](../standards/GlobalSolutionStructureStandards.md#2-visual-studio-solution-folder-structure) and [`Section 7`](../standards/GlobalSolutionStructureStandards.md#7-shared-class-libraries).

A conforming shared library repository must follow this pattern:

```
HttpClientManager/
├── HttpClientManager.sln
├── HttpClientManager/
│   └── HttpClientManager.csproj
└── HttpClientManager.Tests/
    └── HttpClientManager.Tests.csproj
```

### 3.2 Naming Rules
<!-- STD-MARKER: nuget-library.3.2 -->

The repository name, solution name, project name, assembly name, and package ID for a single-library repository must match exactly.

Correct examples:

- `HttpClientManager`
- `DataImportExportManager`

A shared library repository must not use a package ID that differs from the repository and project identity unless an approved repository-specific deviation documents the alternate naming rule.

**Analyzer-package-family exception:** In an analyzer-package-family repository (see Section 2.1), the repository name identifies the family, not a single package or the shared solution. The solution file takes the repository name (for example, `GlobalStandards.Analyzers.slnx`). Each domain package's project name, assembly name, and package ID must match each other exactly and must be prefixed with the repository name, following the pattern `<RepositoryName>.<Domain>` (for example, `GlobalStandards.Analyzers.Caching` inside the `GlobalStandards.Analyzers` repository).

---

## 4. Package Metadata and Project Settings
<!-- STD-MARKER: nuget-library.4 -->

### 4.1 Required Package Metadata
<!-- STD-MARKER: nuget-library.4.1 -->

Every shared library project must declare the following metadata in its `.csproj` file:

- `<PackageId>`
- `<Version>`
- `<Authors>`
- `<Company>`
- `<Description>`
- `<RepositoryUrl>`
- `<RepositoryType>`
- `<GenerateDocumentationFile>`
- `<Nullable>`
- `<ImplicitUsings>`

A conforming example is:

```xml
<PropertyGroup>
  <PackageId>HttpClientManager</PackageId>
  <Version>1.2.0</Version>
  <Authors>Troy Crowe</Authors>
  <Company>Single Source Management</Company>
  <Description>Provides shared outbound HTTP client configuration and request execution helpers.</Description>
  <RepositoryUrl>https://dev.azure.com/tcrowe0170/SingleSourceManagement/_git/HttpClientManager</RepositoryUrl>
  <RepositoryType>git</RepositoryType>
  <GenerateDocumentationFile>true</GenerateDocumentationFile>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
</PropertyGroup>
```

`<Description>` must describe the capability the package provides. It must not repeat the package name only.

### 4.2 Required Build Properties
<!-- STD-MARKER: nuget-library.4.2 -->

Every shared library project must enable deterministic and CI-aware build settings:

```xml
<PropertyGroup>
  <Deterministic>true</Deterministic>
  <ContinuousIntegrationBuild Condition="'$(CI)' == 'true'">true</ContinuousIntegrationBuild>
</PropertyGroup>
```

### 4.3 SourceLink and Symbols
<!-- STD-MARKER: nuget-library.4.3 -->

Every shared library project must enable SourceLink and symbol package generation:

```xml
<PropertyGroup>
  <PublishRepositoryUrl>true</PublishRepositoryUrl>
  <EmbedUntrackedSources>true</EmbedUntrackedSources>
  <IncludeSymbols>true</IncludeSymbols>
  <SymbolPackageFormat>snupkg</SymbolPackageFormat>
</PropertyGroup>
<ItemGroup>
  <PackageReference Include="Microsoft.SourceLink.AzureDevOpsServer.Git" Version="8.0.0" PrivateAssets="All" />
</ItemGroup>
```

---

## 5. Public API and Registration Rules
<!-- STD-MARKER: nuget-library.5 -->

### 5.1 Minimal Public Surface
<!-- STD-MARKER: nuget-library.5.1 -->

A shared library must expose only the public types required by consuming repositories. Implementation types that are not part of the supported library contract must be marked `internal`.

Public types must be marked `sealed` unless inheritance is part of the intended library contract.

### 5.2 Interface Requirement
<!-- STD-MARKER: nuget-library.5.2 -->

Every public service exposed for dependency injection must have a corresponding public interface.

### 5.3 Dependency Injection Registration
<!-- STD-MARKER: nuget-library.5.3 -->

Every shared library that requires service registration must provide a single `ServiceCollectionExtensions` entry point for registration. Consuming applications must call the shared registration method. They must not wire up internal library services manually.

A conforming pattern is:

```csharp
namespace HttpClientManager;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHttpClientManager(this IServiceCollection services)
    {
        return services;
    }
}
```

### 5.4 Options Binding
<!-- STD-MARKER: nuget-library.5.4 -->

A configurable shared library must expose a dedicated options type and must bind that type through the standard options pattern.

### 5.5 XML Documentation
<!-- STD-MARKER: nuget-library.5.5 -->

Every public type, method, property, and constructor in a shared library must include XML documentation comments so that package consumers receive IntelliSense documentation.

### 5.6 Nullability and Exceptions
<!-- STD-MARKER: nuget-library.5.6 -->

Shared library projects must enable nullable reference types. Public APIs must declare nullability explicitly.

Shared libraries must throw `ArgumentNullException` for required null arguments by using `ArgumentNullException.ThrowIfNull`.

Shared libraries must not throw `Exception` directly when a more specific exception type is available.

---

## 6. Dependency Rules
<!-- STD-MARKER: nuget-library.6 -->

### 6.1 Direct Dependency Requirement
<!-- STD-MARKER: nuget-library.6.1 -->

A shared library must reference only packages required by its own implementation. Speculative dependencies are not permitted.

### 6.2 Transitive Dependency Review
<!-- STD-MARKER: nuget-library.6.2 -->

A shared library must not take a dependency that drags in an unnecessary hosting stack or other heavyweight transitive dependency set when the library is intended to remain broadly reusable.

### 6.3 Package Version Declaration
<!-- STD-MARKER: nuget-library.6.3 -->

NuGet dependencies in a shared library `.csproj` file must declare an explicit version.

`Version="*"` and other floating latest-version patterns must not be used in a shared library project file.

---

## 7. Versioning Rules
<!-- STD-MARKER: nuget-library.7 -->

### 7.1 Semantic Versioning
<!-- STD-MARKER: nuget-library.7.1 -->

Every shared library package must follow semantic versioning using `MAJOR.MINOR.PATCH`.

| Change type | Required increment |
|---|---|
| Breaking change that requires consumer code changes | `MAJOR` |
| Backward-compatible capability addition | `MINOR` |
| Backward-compatible fix or internal correction | `PATCH` |

A published package version must never be reused.

### 7.2 Pre-release Versions
<!-- STD-MARKER: nuget-library.7.2 -->

Pre-release versions must use explicit semantic labels such as `-alpha.1`, `-beta.1`, or `-rc.1`.

Pre-release package versions must not be used by production repositories unless an approved repository-specific deviation allows that usage.

### 7.3 Version Source of Truth
<!-- STD-MARKER: nuget-library.7.3 -->

The package version must be declared through the project file `<Version>` property or injected directly into that property during the governed pack step. A repository must not split the authoritative package version across unrelated variables or duplicate version declarations.

---

## 8. Build, Pack, and Publish
<!-- STD-MARKER: nuget-library.8 -->

### 8.1 Governed Pipeline Requirement
<!-- STD-MARKER: nuget-library.8.1 -->

Every shared library repository must implement the class-library pipeline pattern defined in [`GlobalAzureDevOpsPipelineStandards.md` Section Class Library](../standards/GlobalAzureDevOpsPipelineStandards.md#class-library).

Validation on `feature/*` to `dev` must build and test the library and must create the NuGet package artifact.

Shared library repositories must package class libraries as NuGet packages for consumption by other applications.

Shared library repositories must not define or require a production rollout pipeline because a class library is not a live environment-bound deployment artifact.

### 8.2 Pack Command
<!-- STD-MARKER: nuget-library.8.2 -->

Package creation must use `dotnet pack`. `nuget pack` must not be used for SDK-style shared library projects.

A conforming command shape is:

```bash
dotnet pack HttpClientManager/HttpClientManager.csproj --configuration Release --no-build --output ./artifacts
```

### 8.3 Publication Target
<!-- STD-MARKER: nuget-library.8.3 -->

Internal shared library packages must be published to the organization's Azure Artifacts feed. Internal shared libraries must not be published to `nuget.org` unless the repository is explicitly governed as an approved open-source package repository.

Package distribution through an internal feed is package consumption infrastructure, not a production rollout path.

### 8.4 Immutable Publication
<!-- STD-MARKER: nuget-library.8.4 -->

A published package version is immutable. A repository must not overwrite, replace, or delete a published version as the normal correction path. Corrections must be released as a new semantic version.

---

## 9. Consumption Rules
<!-- STD-MARKER: nuget-library.9 -->

### 9.1 Package Reference Only
<!-- STD-MARKER: nuget-library.9.1 -->

A consuming repository must reference a shared library through `<PackageReference>`. It must not reference the shared library by committed project reference or direct DLL reference.

### 9.2 Version Range Rule
<!-- STD-MARKER: nuget-library.9.2 -->

Consuming repositories must reference shared library packages by a fixed-major version range so that non-breaking minor and patch updates can be adopted without allowing automatic major-version upgrades.

A conforming example is:

```xml
<PackageReference Include="HttpClientManager" Version="[1.0, 2.0)" />
```

### 9.3 Upgrade Validation
<!-- STD-MARKER: nuget-library.9.3 -->

When a consuming repository upgrades a shared library package version or version range, it must:

1. Review the package release notes or package change record.
2. Update the package reference in the project file.
3. Run the repository's required validation before merge.
4. Record the package upgrade in the pull request description.

### 9.4 Temporary Source Attachment for Debugging
<!-- STD-MARKER: nuget-library.9.4 -->

A developer may add a shared library source project locally for debugging, but that temporary project reference must not be committed. The committed state must return to package-only consumption before the pull request is opened.

---

## 10. Package Health Verification
<!-- STD-MARKER: nuget-library.10 -->

### 10.1 Vulnerability Checks
<!-- STD-MARKER: nuget-library.10.1 -->

Repositories that produce or consume shared library packages must run `dotnet list package --vulnerable` as part of governed validation.

A validation run that reports a package vulnerability at `High` or `Critical` severity must fail unless an approved repository-specific deviation documents the temporary exception.

### 10.2 Outdated Package Review
<!-- STD-MARKER: nuget-library.10.2 -->

Repositories that produce or consume shared library packages must review outdated package dependencies on a recurring basis and track required upgrades through backlog work when the upgrade is not completed immediately.

Major-version lag must not be allowed to accumulate without an explicitly tracked follow-up.

---

## 11. Compliance Verification
<!-- STD-MARKER: nuget-library.11 -->

- [ ] Every shared library lives in its own repository with its own solution and test project.
- [ ] Consuming repositories use shared libraries through NuGet packages rather than committed project references or direct DLL references.
- [ ] The repository name, solution name, project name, assembly name, and package ID match for each single-library repository unless an approved deviation documents a different rule.
- [ ] Every shared library project declares the required NuGet metadata fields in the project file.
- [ ] Deterministic build and CI build properties are enabled.
- [ ] SourceLink and symbol package generation are enabled.
- [ ] Public service types exposed for DI have corresponding interfaces.
- [ ] Shared libraries expose a single `ServiceCollectionExtensions` registration entry point when service registration is required.
- [ ] Nullable reference types are enabled and public APIs declare nullability explicitly.
- [ ] Shared libraries use explicit dependency versions and do not use floating latest-version package references.
- [ ] Package versioning follows semantic versioning and published versions are not reused.
- [ ] Shared library validation and publication follow the governed class-library pipeline pattern.
- [ ] Internal packages publish to Azure Artifacts rather than `nuget.org`.
- [ ] Consuming repositories use fixed-major version ranges for shared library package references.
- [ ] `dotnet list package --vulnerable` runs as part of governed validation and fails on High or Critical vulnerabilities unless an approved deviation is documented.

---

## 12. Governance
<!-- STD-MARKER: nuget-library.12 -->

This standard is owned by Troy Crowe. No changes to this file may be merged without Troy Crowe's explicit approval. Changes must be submitted as a pull request that includes a rationale comment explaining the reason for the update or deviation. Direct commits to `dev` or `main` are not permitted. Branch, PR, and approval rules are defined in [`GlobalGovernanceStandards.md`](../standards/GlobalGovernanceStandards.md).
