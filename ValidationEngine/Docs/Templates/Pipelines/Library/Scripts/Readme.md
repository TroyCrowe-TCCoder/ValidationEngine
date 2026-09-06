# Library Local Validation Script Template

## Purpose

This folder contains the reusable local validation entry point for class-library repositories. This script is a template only. It does not become the active validation script until it is copied into a target repository's `Scripts/` (or `scripts/`) folder.

## Required Outcome

You must use this template to give a class-library repository a local validation command that restores, builds, tests (when a test project exists), and optionally packs the solution ΓÇö mirroring the checks the Dev pipeline performs before a pull request is created or updated.

## Direction For The AI Model

- You must copy `Validate.ps1` into the target repository's `Scripts/` folder (matching the repository's existing casing convention if one is already established).
- You must not hardcode a solution or project name ΓÇö the script discovers the first `.sln` and `.csproj` file in the repository root at run time.
- You must not remove the `-Pack` and `-SkipClean` switches; they preserve behavior parity with the reference implementation.
- You must confirm the target repository is a class-library repository before using this template.

## Direction For The Human Developer

- Copy `Validate.ps1` into your repository's `Scripts/` folder.
- Run `pwsh ./Scripts/Validate.ps1` before creating or updating a pull request.
- Use `-Pack` to also produce a NuGet package under `artifacts/`.
- Use `-SkipClean` to skip removing prior `TestResults` folders.

## Rollout Procedure

1. Copy `Validate.ps1` from this folder into the target repository's `Scripts/` folder.
2. Confirm the repository has a `.sln` file (or a single `.csproj`) at its root.
3. Run `pwsh ./Scripts/Validate.ps1` and confirm restore, build, and test (if present) succeed.
4. Reference this script from the repository's README and from the Dev pipeline's local-validation guidance where applicable.
